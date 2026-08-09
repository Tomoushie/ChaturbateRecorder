using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SentinelGuard;

namespace ChaturbateRecorderApp.Services
{
    public enum DownloadState { Idle, Running, Completed, Failed, Stopped }

    /// <summary>
    /// Pilote yt-dlp : construit sa ligne de commande, interprète sa sortie et
    /// tient le journal brut de l'enregistrement.
    ///
    /// **Depuis 36.0, la supervision du processus ne vit plus ici** : lancement,
    /// capture de sortie, watchdog d'inactivité et arrêt de l'arbre de processus
    /// sont passés dans <see cref="GuardedProcessRunner"/> (SentinelGuard). Ce
    /// qui reste est ce qui ne concerne QUE yt-dlp — ses arguments, sa regex de
    /// progression, son fichier de log — et n'avait donc rien à faire dans une
    /// bibliothèque destinée à des tiers.
    ///
    /// L'API publique n'a pas bougé (<see cref="Start"/>, <see cref="Stop"/>,
    /// <see cref="State"/> et les trois évènements) : MainForm est inchangé.
    ///
    /// Tous les évènements sont levés depuis un thread de pool, PAS depuis le
    /// thread d'interface : l'abonné doit marshaler via Control.Invoke avant de
    /// toucher un contrôle WinForms.
    /// </summary>
    public class DownloadEngine
    {
        private readonly GuardedProcessRunner _runner = new();
        private StreamWriter? _logWriter;
        private string _logFilePath = "";
        private long _logMaxSizeBytes;

        public DownloadState State { get; private set; } = DownloadState.Idle;

        public event Action<string>? OnLogLine;
        public event Action<double>? OnProgress;
        public event Action<DownloadState>? OnStateChanged;

        private static readonly Regex ProgressRegex =
            new(@"\[download\]\s+([\d\.]+)%", RegexOptions.Compiled);

        public DownloadEngine()
        {
            _runner.OutputLineReceived += HandleLine;
            _runner.StateChanged += HandleRunnerState;
            // Le runner ne journalise rien lui-même (c'est la règle du package) :
            // ses diagnostics — watchdog déclenché, arrêt impossible — arrivent
            // ici et repartent dans le log de l'application.
            _runner.Diagnostic += message => Logger.Log(message, LogLevel.ERROR);
        }

        public void Start(string ytDlpPath, string ffmpegPath, string targetUrl, string outputTemplate, string logFilePath, string? formatSelector = null, string outputContainer = "mp4", string? cookiesFilePath = null, string? proxyUrl = null, int watchdogTimeoutSeconds = 120, long logMaxSizeBytes = 0)
        {
            if (State == DownloadState.Running)
                throw new InvalidOperationException("Un téléchargement est déjà en cours.");

            var arguments = BuildArguments(ffmpegPath, targetUrl, outputTemplate, formatSelector,
                outputContainer, cookiesFilePath, proxyUrl);

            _logFilePath = logFilePath;
            _logMaxSizeBytes = logMaxSizeBytes;

            try
            {
                _logWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Logger.Log($"Impossible d'ouvrir le fichier log '{logFilePath}' : {ex.Message}", LogLevel.ERROR);
                SetState(DownloadState.Failed);
                return;
            }

            if (!_runner.Start(ytDlpPath, arguments, TimeSpan.FromSeconds(watchdogTimeoutSeconds), out var reason))
            {
                Logger.Log($"Impossible de démarrer yt-dlp : {reason}", LogLevel.ERROR);
                SafeCloseLogWriter();
                SetState(DownloadState.Failed);
            }
        }

        /// <summary>
        /// Ligne de commande yt-dlp. Isolée pour être lisible d'un bloc — c'est
        /// la seule partie de cette classe qui décide de ce qui est réellement
        /// enregistré, et chaque option y répond à un incident précis.
        /// </summary>
        private static List<string> BuildArguments(string ffmpegPath, string targetUrl, string outputTemplate,
            string? formatSelector, string outputContainer, string? cookiesFilePath, string? proxyUrl)
        {
            var arguments = new List<string>
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
                targetUrl,
            };

            if (!string.IsNullOrWhiteSpace(formatSelector))
            {
                arguments.Add("-f");
                arguments.Add(formatSelector);
            }

            if (!string.IsNullOrWhiteSpace(cookiesFilePath))
            {
                arguments.Add("--cookies");
                arguments.Add(cookiesFilePath);
            }

            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                arguments.Add("--proxy");
                arguments.Add(proxyUrl);
            }

            return arguments;
        }

        private void HandleLine(string line)
        {
            try { _logWriter?.WriteLine(line); }
            catch { /* fichier log verrouillé/inaccessible : le log UI reste utilisable */ }

            RotateJobLogIfTooLarge();

            OnLogLine?.Invoke(line);

            if (TryParseProgress(line, out var pct))
                OnProgress?.Invoke(pct);
        }

        /// <summary>
        /// Traduit l'état du superviseur en état métier. La correspondance est
        /// volontairement explicite plutôt qu'un cast entre deux enums de même
        /// forme : rien ne garantit que SentinelGuard gardera cet ordre, et un
        /// décalage silencieux ferait passer un échec pour une réussite.
        /// </summary>
        private void HandleRunnerState(SupervisedProcessState state)
        {
            if (state == SupervisedProcessState.Running)
            {
                SetState(DownloadState.Running);
                return;
            }

            if (state == SupervisedProcessState.Idle) return;

            SafeCloseLogWriter();
            SetState(state switch
            {
                SupervisedProcessState.Completed => DownloadState.Completed,
                SupervisedProcessState.Stopped => DownloadState.Stopped,
                _ => DownloadState.Failed,
            });
        }

        private void SetState(DownloadState state)
        {
            State = state;
            OnStateChanged?.Invoke(state);
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

        private void SafeCloseLogWriter()
        {
            try { _logWriter?.Flush(); _logWriter?.Dispose(); }
            catch { /* ignoré */ }
            _logWriter = null;
        }

        /// <summary>
        /// Rotation (2.4) du log brut de ce job si sa taille dépasse le seuil
        /// configuré : ferme l'écriture en cours, laisse LogFileRotator renommer
        /// le fichier plein, puis rouvre un fichier vide sous le nom d'origine
        /// pour la suite de l'enregistrement.
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

                LogFileRotator.RotateIfTooLarge(_logFilePath, _logMaxSizeBytes);

                _logWriter = new StreamWriter(_logFilePath, append: true, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la rotation du log de job : {ex.Message}", LogLevel.WARN);
            }
        }

        /// <summary>
        /// Arrête l'enregistrement en tuant l'arbre de processus complet
        /// (yt-dlp ET le ffmpeg qu'il a éventuellement lancé en enfant), et
        /// marque l'arrêt comme MANUEL : l'état final sera Stopped, ce qui
        /// exclut la reconnexion automatique côté MainForm.
        /// </summary>
        public void Stop() => _runner.Stop();
    }
}
