using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SentinelGuard;
using Xunit;

namespace SentinelGuard.Tests
{
    /// <summary>
    /// Ces tests lancent de VRAIS processus (cmd.exe). C'est délibéré : ce qui
    /// peut casser dans un superviseur de processus — une sortie perdue à la
    /// fermeture, un watchdog qui ne se déclenche jamais, un état final erroné —
    /// ne se reproduit pas avec un objet simulé, qui ne ferait que rejouer ce
    /// qu'on croit déjà savoir du comportement de Windows.
    /// </summary>
    public class GuardedProcessRunnerTests
    {
        private static string Cmd => Path.Combine(Environment.SystemDirectory, "cmd.exe");

        /// <summary>
        /// Attend un état final. Les évènements viennent d'un thread de pool :
        /// un simple Assert après Start passerait avant même que le processus
        /// n'ait démarré.
        /// </summary>
        private static async Task<SupervisedProcessState> WaitForExitAsync(
            GuardedProcessRunner runner, int timeoutSeconds = 30)
        {
            var completion = new TaskCompletionSource<SupervisedProcessState>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            runner.StateChanged += state =>
            {
                if (state != SupervisedProcessState.Running)
                    completion.TrySetResult(state);
            };

            var finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
            Assert.True(finished == completion.Task, "Le processus ne s'est jamais terminé.");
            return await completion.Task;
        }

        [Fact]
        public async Task CapturesOutputLines()
        {
            using var runner = new GuardedProcessRunner();
            var lines = new List<string>();
            runner.OutputLineReceived += line => { lock (lines) lines.Add(line); };

            var exit = WaitForExitAsync(runner);
            Assert.True(runner.Start(Cmd, new[] { "/c", "echo", "SentinelGuard" }, TimeSpan.Zero, out var reason), reason);
            var state = await exit;

            Assert.Equal(SupervisedProcessState.Completed, state);
            Assert.Contains(lines, l => l.Contains("SentinelGuard", StringComparison.Ordinal));
        }

        [Fact]
        public async Task NonZeroExitCode_IsAFailure()
        {
            using var runner = new GuardedProcessRunner();

            var exit = WaitForExitAsync(runner);
            Assert.True(runner.Start(Cmd, new[] { "/c", "exit", "3" }, TimeSpan.Zero));
            var state = await exit;

            // Un processus qui se termine n'est pas un processus qui a réussi :
            // c'est exactement la confusion que ce garde-fou empêche.
            Assert.Equal(SupervisedProcessState.Failed, state);
            Assert.Equal(3, runner.ExitCode);
        }

        [Fact]
        public async Task Stop_ReportsStoppedAndNotFailed()
        {
            using var runner = new GuardedProcessRunner();

            var exit = WaitForExitAsync(runner);
            // ping en boucle : vivant plusieurs secondes, tué avant la fin.
            Assert.True(runner.Start(Cmd, new[] { "/c", "ping", "-n", "30", "127.0.0.1" }, TimeSpan.Zero));
            await Task.Delay(300);
            runner.Stop();
            var state = await exit;

            // La distinction compte pour l'appelant : un arrêt demandé ne doit
            // pas déclencher la reconnexion automatique qu'un échec déclenche.
            Assert.Equal(SupervisedProcessState.Stopped, state);
        }

        [Fact]
        public async Task Watchdog_KillsASilentProcess()
        {
            using var runner = new GuardedProcessRunner();
            var diagnostics = new List<string>();
            runner.Diagnostic += message => { lock (diagnostics) diagnostics.Add(message); };

            var exit = WaitForExitAsync(runner);
            // Sortie redirigée vers nul : le processus vit ~5 s sans jamais
            // écrire une ligne. C'est le cas que le watchdog existe pour couvrir.
            Assert.True(runner.Start(Cmd,
                new[] { "/c", "ping -n 6 127.0.0.1 > nul" }, TimeSpan.FromSeconds(1)));
            var state = await exit;

            Assert.Equal(SupervisedProcessState.Failed, state);
            Assert.Contains(diagnostics, d => d.Contains("hung", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Watchdog_LeavesATalkativeProcessAlone()
        {
            using var runner = new GuardedProcessRunner();

            var exit = WaitForExitAsync(runner);
            // ping écrit une ligne par seconde : le seuil de 3 s n'est jamais
            // atteint. Sans ce test, un watchdog trop zélé tuerait un travail
            // parfaitement sain sans que rien ne le signale.
            Assert.True(runner.Start(Cmd,
                new[] { "/c", "ping", "-n", "4", "127.0.0.1" }, TimeSpan.FromSeconds(3)));
            var state = await exit;

            Assert.Equal(SupervisedProcessState.Completed, state);
        }

        [Fact]
        public void Start_RefusesAMissingExecutable()
        {
            using var runner = new GuardedProcessRunner();

            var started = runner.Start(
                Path.Combine(Path.GetTempPath(), "sentinelguard-does-not-exist.exe"),
                Array.Empty<string>(), TimeSpan.Zero, out var reason);

            Assert.False(started);
            Assert.False(string.IsNullOrEmpty(reason));
            Assert.Equal(SupervisedProcessState.Idle, runner.State);
        }

        [Fact]
        public void Start_RefusesAnEmptyPath()
        {
            using var runner = new GuardedProcessRunner();

            Assert.False(runner.Start("", Array.Empty<string>(), TimeSpan.Zero, out var reason));
            Assert.False(string.IsNullOrEmpty(reason));
        }

        [Fact]
        public async Task Start_RefusesASecondProcessInTheSameRunner()
        {
            using var runner = new GuardedProcessRunner();

            var exit = WaitForExitAsync(runner);
            Assert.True(runner.Start(Cmd, new[] { "/c", "ping", "-n", "10", "127.0.0.1" }, TimeSpan.Zero));

            // Sans ce refus, le second démarrage remplacerait la référence au
            // premier processus, qui continuerait de tourner sans que personne
            // ne puisse plus l'arrêter.
            Assert.False(runner.Start(Cmd, new[] { "/c", "echo", "second" }, TimeSpan.Zero, out var reason));
            Assert.False(string.IsNullOrEmpty(reason));

            runner.Stop();
            await exit;
        }

        [Fact]
        public async Task ArgumentsAreNotSplitOnSpaces()
        {
            using var runner = new GuardedProcessRunner();
            var lines = new List<string>();
            runner.OutputLineReceived += line => { lock (lines) lines.Add(line); };

            var exit = WaitForExitAsync(runner);
            // Un argument contenant des espaces passe en UN élément : c'est tout
            // l'intérêt d'ArgumentList face à une ligne de commande concaténée,
            // où un chemin non échappé se scinde silencieusement en deux.
            Assert.True(runner.Start(Cmd, new[] { "/c", "echo", "deux mots" }, TimeSpan.Zero));
            await exit;

            Assert.Contains(lines, l => l.Contains("deux mots", StringComparison.Ordinal));
        }

        [Fact]
        public async Task LastOutputLineIsNotLostAtExit()
        {
            using var runner = new GuardedProcessRunner();
            var lines = new List<string>();
            runner.OutputLineReceived += line => { lock (lines) lines.Add(line); };

            var exit = WaitForExitAsync(runner);
            // La ligne écrite juste avant la sortie du processus est celle qui
            // disparaissait par intermittence : Exited peut se lever avant que
            // les gestionnaires asynchrones aient été servis.
            Assert.True(runner.Start(Cmd, new[] { "/c", "echo derniere-ligne" }, TimeSpan.Zero));
            await exit;

            Assert.Contains(lines, l => l.Contains("derniere-ligne", StringComparison.Ordinal));
        }

        [Fact]
        public void Dispose_WithoutStart_DoesNothing()
        {
            var runner = new GuardedProcessRunner();
            runner.Dispose();
            runner.Dispose(); // idempotent

            Assert.Throws<ObjectDisposedException>(() =>
                runner.Start(Cmd, Array.Empty<string>(), TimeSpan.Zero, out _));
        }
    }
}
