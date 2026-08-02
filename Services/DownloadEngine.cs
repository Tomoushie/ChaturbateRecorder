using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChaturbateRecorderApp.Services
{
    public enum DownloadState { Idle, Running, Completed, Failed, Stopped }

    /// <summary>
    /// Moteur de téléchargement basé sur un vrai System.Diagnostics.Process
    /// (remplace Start-Job de la version PowerShell, qui tourne dans un
    /// processus PowerShell séparé et ne tue pas forcément les processus
    /// enfants natifs). La sortie de yt-dlp est lue ligne par ligne de façon
    /// asynchrone (OutputDataReceived), écrite dans le fichier log, et le
    /// pourcentage est extrait en direct via regex.
    ///
    /// Tous les événements (OnLogLine, OnProgress, OnStateChanged) sont levés
    /// depuis un thread de pool (celui du Process), PAS depuis le thread UI :
    /// l'abonné (MainForm) doit impérativement marshaler via Control.Invoke
    /// avant de toucher un contrôle WinForms.
    /// </summary>
    public class DownloadEngine
    {
        private Process? _process;
        private StreamWriter? _logWriter;
        private string _logFilePath = "";
        private long _logMaxSizeBytes;
        private bool _wasStoppedManually;
        private bool _watchdogTriggered;
        private DateTime _lastOutputUtc;
        private CancellationTokenSource? _watchdogCts;
        private readonly object _sync = new();

        public DownloadState State { get; private set; } = DownloadState.Idle;

        public event Action<string>? OnLogLine;
        public event Action<double>? OnProgress;
        public event Action<DownloadState>? OnStateChanged;

        private static readonly Regex ProgressRegex =
            new(@"\[download\]\s+([\d\.]+)%", RegexOptions.Compiled);

        public void Start(string ytDlpPath, string ffmpegPath, string targetUrl, string outputTemplate, string logFilePath, string? formatSelector = null, string outputContainer = "mp4", string? cookiesFilePath = null, string? proxyUrl = null, int watchdogTimeoutSeconds = 120, long logMaxSizeBytes = 0)
        {
            lock (_sync)
            {
                if (State == DownloadState.Running)
                    throw new InvalidOperationException("Un téléchargement est déjà en cours.");

                _wasStoppedManually = false;
                _watchdogTriggered = false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            foreach (var a in new[]
            {
                "--newline",
                "--retries", "infinite",
                "--fragment-retries", "infinite",
                "--retry-sleep", "5",
                "--socket-timeout", "30",
                "--wait-for-video", "30",
                "--hls-use-mpegts",
                "--remux-video", outputContainer,
                "--ffmpeg-location", ffmpegPath,
                "-o", outputTemplate,
                "--progress",
                targetUrl
            })
            {
                psi.ArgumentList.Add(a);
            }

            if (!string.IsNullOrWhiteSpace(formatSelector))
            {
                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add(formatSelector);
            }

            if (!string.IsNullOrWhiteSpace(cookiesFilePath))
            {
                psi.ArgumentList.Add("--cookies");
                psi.ArgumentList.Add(cookiesFilePath);
            }

            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                psi.ArgumentList.Add("--proxy");
                psi.ArgumentList.Add(proxyUrl);
            }

            _logFilePath = logFilePath;
            _logMaxSizeBytes = logMaxSizeBytes;

            try
            {
                _logWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Logger.Log($"Impossible d'ouvrir le fichier log '{logFilePath}' : {ex.Message}", LogLevel.ERROR);
                State = DownloadState.Failed;
                OnStateChanged?.Invoke(State);
                return;
            }

            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.OutputDataReceived += (s, e) => HandleLine(e.Data);
            _process.ErrorDataReceived  += (s, e) => HandleLine(e.Data);
            _process.Exited += (s, e) => HandleExited();

            try
            {
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                State = DownloadState.Running;
                OnStateChanged?.Invoke(State);

                _lastOutputUtc = DateTime.UtcNow;
                _watchdogCts = new CancellationTokenSource();
                _ = RunWatchdogAsync(watchdogTimeoutSeconds, _watchdogCts.Token);
            }
            catch (Exception ex)
            {
                Logger.Log($"Impossible de démarrer yt-dlp : {ex.Message}", LogLevel.ERROR);
                SafeCloseLogWriter();
                State = DownloadState.Failed;
                OnStateChanged?.Invoke(State);
            }
        }

        /// <summary>
        /// Watchdog anti-freeze : si yt-dlp/ffmpeg ne produisent plus aucune
        /// ligne de sortie pendant `timeoutSeconds`, on considère le process
        /// figé et on le tue proprement (arbre complet), avec un log explicite.
        /// Distinct d'un arrêt manuel (_wasStoppedManually) : le job final
        /// passe à Failed, pas à Stopped.
        /// </summary>
        private async Task RunWatchdogAsync(int timeoutSeconds, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                    if (State != DownloadState.Running) return;

                    var idleSeconds = (DateTime.UtcNow - _lastOutputUtc).TotalSeconds;
                    if (idleSeconds >= timeoutSeconds)
                    {
                        Logger.Log($"Watchdog : aucune sortie depuis {idleSeconds:F0}s (seuil {timeoutSeconds}s) — process considéré figé, arrêt forcé.", LogLevel.ERROR);
                        lock (_sync) { _watchdogTriggered = true; }
                        try { _process?.Kill(entireProcessTree: true); }
                        catch (Exception ex) { Logger.Log($"Erreur lors de l'arrêt forcé par le watchdog : {ex.Message}", LogLevel.ERROR); }
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt normal (process terminé ou Stop() appelé) : rien à faire.
            }
        }

        private void HandleLine(string? line)
        {
            if (string.IsNullOrEmpty(line)) return;

            _lastOutputUtc = DateTime.UtcNow;

            try { _logWriter?.WriteLine(line); }
            catch { /* fichier log verrouillé/inaccessible : on continue quand même, le log UI reste utilisable */ }

            RotateJobLogIfTooLarge();

            OnLogLine?.Invoke(line);

            if (TryParseProgress(line, out var pct))
                OnProgress?.Invoke(pct);
        }

        /// <summary>
        /// Extrait le pourcentage d'une ligne de sortie yt-dlp du type
        /// "[download]  42.0% of ...". Exposée en public/statique pour être
        /// testable directement (voir Tests/ProgressParsingTests.cs) sans avoir
        /// à démarrer un vrai process yt-dlp.
        /// </summary>
        public static bool TryParseProgress(string line, out double percent)
        {
            var match = ProgressRegex.Match(line);
            if (match.Success &&
                double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out percent))
            {
                return true;
            }
            percent = 0;
            return false;
        }

        private void HandleExited()
        {
            _watchdogCts?.Cancel();

            DownloadState finalState;
            lock (_sync)
            {
                finalState = _wasStoppedManually
                    ? DownloadState.Stopped
                    : _watchdogTriggered
                        ? DownloadState.Failed
                        : (_process!.ExitCode == 0 ? DownloadState.Completed : DownloadState.Failed);
                State = finalState;
            }

            SafeCloseLogWriter();
            OnStateChanged?.Invoke(finalState);
        }

        private void SafeCloseLogWriter()
        {
            try { _logWriter?.Flush(); _logWriter?.Dispose(); }
            catch { /* ignoré */ }
            _logWriter = null;
        }

        /// <summary>
        /// Rotation (2.4) du log brut de ce job si sa taille dépasse le seuil
        /// configuré : ferme l'écriture en cours, renomme le fichier plein
        /// avec un suffixe horodaté, puis rouvre un nouveau fichier vide sous
        /// le nom d'origine pour la suite de l'enregistrement.
        /// </summary>
        private void RotateJobLogIfTooLarge()
        {
            if (_logWriter == null || _logMaxSizeBytes <= 0) return;

            try
            {
                if (_logWriter.BaseStream.Length < _logMaxSizeBytes) return;

                _logWriter.Flush();
                _logWriter.Dispose();
                _logWriter = null;

                LogRotationManager.RotateIfTooLarge(_logFilePath, _logMaxSizeBytes);

                _logWriter = new StreamWriter(_logFilePath, append: true, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la rotation du log de job : {ex.Message}", LogLevel.WARN);
            }
        }

        /// <summary>
        /// Arrête le téléchargement en tuant l'arbre de processus complet
        /// (yt-dlp ET le ffmpeg qu'il a éventuellement lancé en enfant).
        /// Equivalent de Stop-Job + Remove-Job, en plus fiable : un Stop-Job
        /// PowerShell arrête le job mais ne garantit pas la terminaison des
        /// processus natifs enfants, ce que Process.Kill(true) fait réellement.
        /// </summary>
        public void Stop()
        {
            lock (_sync)
            {
                if (_process == null || _process.HasExited) return;
                _wasStoppedManually = true;
            }

            try
            {
                _process!.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de l'arrêt du processus : {ex.Message}", LogLevel.ERROR);
            }
        }
    }
}
