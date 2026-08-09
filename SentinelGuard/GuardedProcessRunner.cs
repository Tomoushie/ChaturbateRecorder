using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SentinelGuard
{
    /// <summary>
    /// Lifecycle of a process supervised by <see cref="GuardedProcessRunner"/>.
    /// </summary>
    public enum SupervisedProcessState
    {
        /// <summary>Nothing has been started yet.</summary>
        Idle,
        /// <summary>The process was started and has not exited.</summary>
        Running,
        /// <summary>The process exited on its own with exit code 0.</summary>
        Completed,
        /// <summary>The process exited with a non-zero code, or was killed by the inactivity watchdog.</summary>
        Failed,
        /// <summary>The process was killed by an explicit <see cref="GuardedProcessRunner.Stop"/> call.</summary>
        Stopped,
    }

    /// <summary>
    /// Runs an external executable under supervision: output is captured line by
    /// line, an inactivity watchdog kills a process that stops producing output,
    /// and stopping kills the whole process tree rather than just the child you
    /// started.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the runtime counterpart of <see cref="BinaryVerifier"/>: verifying
    /// that a binary is the one you expect only covers the moment before you
    /// launch it. Once it runs, two failure modes remain, and neither raises an
    /// exception you can catch:
    /// </para>
    /// <list type="bullet">
    /// <item><description>the process hangs — still alive, doing nothing, holding
    /// its output handles open. Waiting on it waits forever;</description></item>
    /// <item><description>the process spawns children of its own. Killing the
    /// process you started leaves them running, still writing to your files.
    /// <see cref="Stop"/> kills the entire tree.</description></item>
    /// </list>
    /// <para>
    /// <b>Unlike the validators in this package, this class is not a pure
    /// function</b> — it starts a process and raises events. It still follows the
    /// same rule about diagnostics: nothing is written to a log or to the
    /// console. Failures that have no exception to carry them are reported
    /// through <see cref="Diagnostic"/>, and the caller decides what to do.
    /// </para>
    /// <para>
    /// <b>Threading:</b> <see cref="OutputLineReceived"/>,
    /// <see cref="StateChanged"/> and <see cref="Diagnostic"/> are raised on
    /// thread-pool threads, never on the thread that called
    /// <see cref="Start(string, IReadOnlyList{string}, TimeSpan)"/>. A UI
    /// application must marshal to its own thread before touching any control.
    /// </para>
    /// </remarks>
    public sealed class GuardedProcessRunner : IDisposable
    {
        // Le watchdog échantillonne l'inactivité plutôt que d'armer un minuteur
        // remis à zéro à chaque ligne : une sortie bavarde relancerait alors des
        // milliers de minuteurs par seconde. La cadence suit le délai demandé —
        // sans ça, un seuil de 2 s ne serait constaté qu'au bout de 10.
        private static readonly TimeSpan MinPollInterval = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan MaxPollInterval = TimeSpan.FromSeconds(10);

        private readonly object _sync = new();
        private Process? _process;
        private CancellationTokenSource? _watchdogCts;
        private bool _stoppedManually;
        private bool _watchdogTriggered;
        private long _lastOutputTicks;
        private bool _disposed;

        /// <summary>Current state of the supervised process.</summary>
        public SupervisedProcessState State { get; private set; } = SupervisedProcessState.Idle;

        /// <summary>
        /// Exit code of the process, or <see langword="null"/> while it is
        /// running or if it never started.
        /// </summary>
        public int? ExitCode { get; private set; }

        /// <summary>
        /// Raised for every line the process writes to standard output or
        /// standard error, in arrival order. Empty lines are skipped.
        /// </summary>
        public event Action<string>? OutputLineReceived;

        /// <summary>Raised whenever <see cref="State"/> changes.</summary>
        public event Action<SupervisedProcessState>? StateChanged;

        /// <summary>
        /// Raised for failures that happen after a successful start and
        /// therefore have no caller to return to — a watchdog kill, or a kill
        /// that itself failed. Purely informational: the state change that goes
        /// with it is reported through <see cref="StateChanged"/>.
        /// </summary>
        public event Action<string>? Diagnostic;

        /// <summary>
        /// Starts <paramref name="executablePath"/> with the given arguments.
        /// </summary>
        /// <param name="executablePath">Full path of the executable to run.</param>
        /// <param name="arguments">
        /// Arguments, one element per argument — never a single pre-quoted
        /// string. Each element is passed verbatim, so a path containing spaces
        /// or quotes needs no escaping and cannot be split in two.
        /// </param>
        /// <param name="inactivityTimeout">
        /// How long the process may produce no output at all before it is
        /// considered hung and killed. <see cref="TimeSpan.Zero"/> disables the
        /// watchdog — appropriate for a process that is legitimately silent.
        /// </param>
        /// <returns><see langword="true"/> if the process started.</returns>
        public bool Start(string executablePath, IReadOnlyList<string> arguments, TimeSpan inactivityTimeout) =>
            Start(executablePath, arguments, inactivityTimeout, out _);

        /// <summary>
        /// Starts <paramref name="executablePath"/>, reporting why it could not
        /// start through <paramref name="reason"/>.
        /// </summary>
        /// <param name="executablePath">Full path of the executable to run.</param>
        /// <param name="arguments">Arguments, one element per argument.</param>
        /// <param name="inactivityTimeout">
        /// Idle time after which the process is considered hung and killed;
        /// <see cref="TimeSpan.Zero"/> disables the watchdog.
        /// </param>
        /// <param name="reason">
        /// Exact cause of the refusal, or <see langword="null"/> on success.
        /// </param>
        /// <returns><see langword="true"/> if the process started.</returns>
        public bool Start(string executablePath, IReadOnlyList<string> arguments, TimeSpan inactivityTimeout,
            out string? reason)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                reason = "Executable path is empty.";
                return false;
            }

            if (!File.Exists(executablePath))
            {
                reason = $"Executable not found: '{executablePath}'.";
                return false;
            }

            lock (_sync)
            {
                if (State == SupervisedProcessState.Running)
                {
                    reason = "A process is already running in this runner.";
                    return false;
                }

                _stoppedManually = false;
                _watchdogTriggered = false;
                ExitCode = null;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            foreach (var argument in arguments ?? Array.Empty<string>())
                startInfo.ArgumentList.Add(argument);

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => HandleLine(e.Data);
            process.ErrorDataReceived += (_, e) => HandleLine(e.Data);
            process.Exited += (_, _) => HandleExited();

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                process.Dispose();
                reason = $"Could not start '{executablePath}': {ex.Message}";
                SetState(SupervisedProcessState.Failed);
                return false;
            }

            lock (_sync)
            {
                _process = process;
                Volatile.Write(ref _lastOutputTicks, DateTime.UtcNow.Ticks);
            }

            SetState(SupervisedProcessState.Running);

            if (inactivityTimeout > TimeSpan.Zero)
            {
                _watchdogCts = new CancellationTokenSource();
                _ = WatchForInactivityAsync(inactivityTimeout, _watchdogCts.Token);
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// Kills the supervised process <b>and every process it spawned</b>,
        /// then reports <see cref="SupervisedProcessState.Stopped"/>. Does
        /// nothing if no process is running.
        /// </summary>
        public void Stop()
        {
            Process? process;
            lock (_sync)
            {
                process = _process;
                if (process == null || process.HasExited) return;
                _stoppedManually = true;
            }

            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                // Une course perdue contre un process qui vient de se terminer
                // n'est pas une erreur : l'évènement Exited fera le travail.
                Diagnostic?.Invoke($"Could not kill the process tree: {ex.Message}");
            }
        }

        private async Task WatchForInactivityAsync(TimeSpan timeout, CancellationToken token)
        {
            var poll = TimeSpan.FromMilliseconds(Math.Clamp(
                timeout.TotalMilliseconds / 4, MinPollInterval.TotalMilliseconds, MaxPollInterval.TotalMilliseconds));

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(poll, token).ConfigureAwait(false);
                    if (State != SupervisedProcessState.Running) return;

                    var idle = DateTime.UtcNow - new DateTime(Volatile.Read(ref _lastOutputTicks), DateTimeKind.Utc);
                    if (idle < timeout) continue;

                    Process? process;
                    lock (_sync)
                    {
                        if (State != SupervisedProcessState.Running) return;
                        _watchdogTriggered = true;
                        process = _process;
                    }

                    Diagnostic?.Invoke(
                        $"No output for {idle.TotalSeconds:F0}s (threshold {timeout.TotalSeconds:F0}s) — process considered hung, killing it.");

                    try
                    {
                        process?.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        Diagnostic?.Invoke($"Could not kill the hung process: {ex.Message}");
                    }

                    return;
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt normal : le process s'est terminé, ou Dispose a été appelé.
            }
        }

        private void HandleLine(string? line)
        {
            if (string.IsNullOrEmpty(line)) return;

            Volatile.Write(ref _lastOutputTicks, DateTime.UtcNow.Ticks);
            OutputLineReceived?.Invoke(line);
        }

        private void HandleExited()
        {
            _watchdogCts?.Cancel();

            Process? process;
            bool stoppedManually, watchdogTriggered;
            lock (_sync)
            {
                process = _process;
                stoppedManually = _stoppedManually;
                watchdogTriggered = _watchdogTriggered;
            }

            if (process == null) return;

            try
            {
                // Exited peut se lever AVANT que les dernières lignes aient été
                // remises aux gestionnaires asynchrones. WaitForExit() sans
                // délai est la façon documentée d'attendre ce vidage : sans lui,
                // la fin de la sortie — souvent le message d'erreur qui explique
                // l'échec — se perd par intermittence.
                process.WaitForExit();
                ExitCode = process.ExitCode;
            }
            catch (Exception ex)
            {
                Diagnostic?.Invoke($"Could not read the exit code: {ex.Message}");
            }

            var finalState = stoppedManually ? SupervisedProcessState.Stopped
                : watchdogTriggered ? SupervisedProcessState.Failed
                : ExitCode == 0 ? SupervisedProcessState.Completed
                : SupervisedProcessState.Failed;

            SetState(finalState);
        }

        private void SetState(SupervisedProcessState state)
        {
            lock (_sync) { State = state; }
            StateChanged?.Invoke(state);
        }

        /// <summary>
        /// Cancels the watchdog and releases the underlying process handle. Does
        /// <b>not</b> kill a running process: disposing a supervisor is not a
        /// reason to interrupt what it supervises. Call <see cref="Stop"/> first
        /// if that is what you want.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _watchdogCts?.Cancel();
            _watchdogCts?.Dispose();
            _watchdogCts = null;

            lock (_sync)
            {
                _process?.Dispose();
                _process = null;
            }
        }
    }
}
