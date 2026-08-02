using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Security;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp
{
    public class MainForm : Form
    {
        // --- Contrôles ---
        private Panel contentPanel = null!;
        private ComboBox themeCombo = null!;
        private Label themeLabel = null!;
        private Button checkUpdateButton = null!;
        private Button tutorialButton = null!;
        private Button modeToggleButton = null!;
        private TextBox urlTextBox = null!;
        private Button startButton = null!;
        private Button stopAllButton = null!;
        private Button addFavoriteButton = null!;
        private FlowLayoutPanel jobsListPanel = null!;
        private Panel advancedOptionsPanel = null!;
        private GroupBox grpRecord = null!;
        private GroupBox grpProgress = null!;
        private GroupBox grpHistory = null!;
        private ListView historyListView = null!;
        private Button refreshHistoryButton = null!;
        private Button openHistoryFolderButton = null!;
        private GroupBox grpFavorites = null!;
        private GroupBox grpDonate = null!;
        private GroupBox grpLogs = null!;
        private ComboBox qualityCombo = null!;
        private ComboBox codecCombo = null!;
        private ComboBox formatCombo = null!;
        private TextBox saveDirTextBox = null!;
        private Button browseDirButton = null!;
        private TextBox cookiesTextBox = null!;
        private Button browseCookiesButton = null!;
        private TextBox proxyTextBox = null!;
        private CheckBox autoReconnectCheckbox = null!;
        private ListBox favoritesListBox = null!;
        private Button loadFavoriteButton = null!;
        private Button removeFavoriteButton = null!;
        private Button donateButton = null!;
        private Button websiteButton = null!;
        private PictureBox qrPictureBox = null!;
        private Label donateLabel = null!;
        private ListBox logListBox = null!;

        /// <summary>
        /// Un enregistrement affiché dans la liste "Enregistrements en cours" :
        /// lie un RecordingJob (moteur + métadonnées) à sa ligne d'UI dédiée.
        /// Plusieurs peuvent tourner en parallèle, chacun avec son propre
        /// process yt-dlp — c'est ce qui permet d'enregistrer plusieurs lives
        /// à la fois sans ouvrir plusieurs instances de l'application.
        /// </summary>
        private sealed class JobRow
        {
            public RecordingJob Job = null!;
            public Panel Container = null!;
            public Label NameLabel = null!;
            public ProgressBar ProgressBar = null!;
            public Label StatusLabel = null!;
            public Button StopButton = null!;
            public Button OpenButton = null!;
            public Action RestartEngine = null!;
            public System.Windows.Forms.Timer? PendingReconnectTimer;
        }

        // --- État ---
        private readonly FavoritesManager _favorites = new();
        private readonly List<JobRow> _jobRows = new();
        private bool _advancedMode = true;
        private NotifyIcon _notifyIcon = null!;

        private readonly UserSettings _settings;

        public MainForm()
        {
            _settings = SettingsManager.Load();
            if (!string.IsNullOrWhiteSpace(_settings.CaptureDir) && PathValidator.IsValidPath(_settings.CaptureDir))
                AppConfig.CaptureDir = _settings.CaptureDir;
            if (!string.IsNullOrWhiteSpace(_settings.CookiesFilePath) && File.Exists(_settings.CookiesFilePath))
                AppConfig.CookiesFilePath = _settings.CookiesFilePath;
            if (!string.IsNullOrWhiteSpace(_settings.ProxyUrl))
                AppConfig.ProxyUrl = _settings.ProxyUrl;

            InitializeComponent();

            Logger.Log("Application démarrée.");

            if (!PathValidator.IsValidPath(AppConfig.CaptureDir) || !PathValidator.IsValidPath(AppConfig.LogDir))
            {
                MessageBox.Show(
                    "Dossier de capture ou de logs invalide (sandbox de chemins).",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            Directory.CreateDirectory(AppConfig.CaptureDir);
            Directory.CreateDirectory(AppConfig.LogDir);

            LogRotationManager.PurgeOldLogs(AppConfig.LogDir, AppConfig.LogRetentionDays);

            WarnIfBroadWriteAccess(AppConfig.AppDir);
            WarnIfBroadWriteAccess(AppConfig.CaptureDir);
            WarnIfBroadWriteAccess(AppConfig.LogDir);

            _favorites.Load();
            foreach (var fav in _favorites.Favorites)
                favoritesListBox.Items.Add(fav);

            LoadQrImage();
            ThemeManager.Apply(this, AppTheme.Light);
            ApplyIcons();
            ApplyUiMode(_settings.AdvancedMode ?? true);
            RefreshHistoryAsync();
            ShowFirstRunDialogs();
        }

        private static string CurrentVersion =>
            typeof(MainForm).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        /// <summary>
        /// Sur un tout premier lancement (aucune version jamais vue), affiche le
        /// tutoriel plutôt que le changelog complet — moins redondant pour un
        /// nouvel utilisateur qui ne connaît encore aucune fonctionnalité.
        /// Pour une mise à jour (version déjà vue mais différente), affiche le
        /// changelog de la nouvelle version.
        /// </summary>
        private void ShowFirstRunDialogs()
        {
            var version = CurrentVersion;

            if (_settings.LastSeenVersion == null)
            {
                ShowTutorial();
                _settings.LastSeenVersion = version;
                _settings.HasSeenTutorial = true;
                SettingsManager.Save(_settings);
                return;
            }

            if (_settings.LastSeenVersion != version)
            {
                ShowChangelog(version);
                _settings.LastSeenVersion = version;
                SettingsManager.Save(_settings);
            }
        }

        private void ShowChangelog(string version)
        {
            var entry = Changelog.Entries.FirstOrDefault(e => e.Version == version);
            var body = entry.Changes is { Length: > 0 }
                ? string.Join(Environment.NewLine, entry.Changes.Select(c => "• " + c))
                : "Aucun détail disponible pour cette version.";

            MessageBox.Show(this, body, $"Nouveautés — v{version}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowTutorial()
        {
            using var tutorial = new TutorialForm();
            tutorial.ShowDialog(this);
        }

        /// <summary>
        /// Marshale l'exécution sur le thread UI si nécessaire — indispensable
        /// puisque les événements de DownloadEngine sont levés depuis les
        /// threads du Process (OutputDataReceived / Exited tournent sur des
        /// threads de pool, jamais sur le thread UI).
        /// </summary>
        private void SafeInvoke(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        /// <summary>
        /// Vérification ACL non bloquante (voir AclValidator) : informe l'utilisateur
        /// sans empêcher le démarrage, "Utilisateurs authentifiés" ayant par défaut
        /// des droits Modify sur la plupart des dossiers Windows non durcis.
        /// </summary>
        private void WarnIfBroadWriteAccess(string directoryPath)
        {
            if (AclValidator.TryFindBroadWriteAccess(directoryPath, out var details))
            {
                Logger.Log($"ACL permissive détectée : {details}", LogLevel.WARN);
            }
        }

        private void LoadQrImage()
        {
            if (File.Exists(AppConfig.DonateQrPath))
            {
                if (!BinaryVerifier.VerifyFileHash(AppConfig.DonateQrPath, AppConfig.DonateQrExpectedSha256))
                {
                    Logger.Log($"Hash du QR code de don invalide ({AppConfig.DonateQrPath}) : affichage refusé (possible substitution).", LogLevel.ERROR);
                    return;
                }

                try
                {
                    qrPictureBox.Image = Image.FromFile(AppConfig.DonateQrPath);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Impossible de charger le QR code de don ({AppConfig.DonateQrPath}) : {ex.Message}", LogLevel.WARN);
                }
            }
            else
            {
                Logger.Log($"QR code de don introuvable : {AppConfig.DonateQrPath}", LogLevel.WARN);
            }
        }

        private void AppendLog(string line)
        {
            logListBox.Items.Add(line);
            logListBox.TopIndex = logListBox.Items.Count - 1;
        }

        private void AppendJobLog(RecordingJob job, string message)
        {
            AppendLog($"[{DateTime.Now:HH:mm:ss}] [{job.RoomName}] {message}");
        }

        /// <summary>
        /// Construit la ligne d'UI (nom, barre marquee, statut, bouton) pour un
        /// nouveau job et câble ses événements DownloadEngine. Le bouton sert de
        /// Stop tant que l'enregistrement tourne, puis de "Retirer" une fois
        /// terminé (évite d'accumuler indéfiniment des lignes mortes).
        /// </summary>
        private JobRow BuildJobRow(RecordingJob job)
        {
            var container = new Panel { Size = new Size(605, 46), Margin = new Padding(2) };
            var nameLabel = new Label { Text = job.RoomName, Location = new Point(2, 2), AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            var openButton = new Button { Text = "Ouvrir", Location = new Point(505, 0), Size = new Size(95, 20) };
            var progressBar = new ProgressBar { Location = new Point(2, 22), Size = new Size(350, 18), Minimum = 0, Maximum = 100 };
            var statusLabel = new Label { Text = "Préparation...", Location = new Point(358, 22), Size = new Size(140, 18), AutoSize = false };
            var stopButton = new Button { Text = "Stop", Location = new Point(505, 22), Size = new Size(95, 22) };

            openButton.Image = IconManager.Render("open", 14, IconColor);
            openButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            stopButton.Image = IconManager.Render("stop", 14, IconColor);
            stopButton.TextImageRelation = TextImageRelation.ImageBeforeText;

            openButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(job.SourceUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Impossible d'ouvrir la page : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            container.Controls.AddRange(new Control[] { nameLabel, openButton, progressBar, statusLabel, stopButton });

            var row = new JobRow
            {
                Job = job,
                Container = container,
                NameLabel = nameLabel,
                ProgressBar = progressBar,
                StatusLabel = statusLabel,
                StopButton = stopButton,
                OpenButton = openButton,
            };

            stopButton.Click += (s, e) =>
            {
                if (job.Engine.State == DownloadState.Running)
                {
                    job.Engine.Stop();
                }
                else if (row.PendingReconnectTimer != null)
                {
                    row.PendingReconnectTimer.Stop();
                    row.PendingReconnectTimer.Dispose();
                    row.PendingReconnectTimer = null;
                    job.AutoReconnectEnabled = false;
                    AppendJobLog(job, "Reconnexion automatique annulée.");
                    row.StopButton.Text = "Retirer";
                    row.StatusLabel.Text = "Annulé";
                }
                else
                {
                    jobsListPanel.Controls.Remove(row.Container);
                    _jobRows.Remove(row);
                }
            };

            job.Engine.OnLogLine      += line => SafeInvoke(() => AppendJobLog(job, line));
            job.Engine.OnProgress     += pct  => SafeInvoke(() => UpdateJobProgress(row, pct));
            job.Engine.OnStateChanged += state => SafeInvoke(() => HandleJobStateChanged(row, state));

            return row;
        }

        private void UpdateJobProgress(JobRow row, double pct)
        {
            var clamped = Math.Min(100, Math.Max(0, (int)Math.Round(pct)));
            row.ProgressBar.Value = clamped;
            row.StatusLabel.Text = $"{pct.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}%";
        }

        private void HandleJobStateChanged(JobRow row, DownloadState state)
        {
            switch (state)
            {
                case DownloadState.Running:
                    row.Job.ReconnectAttempt = 0;
                    row.StopButton.Text = "Stop";
                    row.StopButton.Enabled = true;
                    row.ProgressBar.Style = ProgressBarStyle.Marquee;
                    row.ProgressBar.MarqueeAnimationSpeed = 30;
                    row.StatusLabel.Text = "En cours...";
                    PulseProgressBar(row.ProgressBar, RunningColor);
                    break;

                case DownloadState.Completed:
                case DownloadState.Failed:
                case DownloadState.Stopped:
                    row.StopButton.Text = "Retirer";
                    row.ProgressBar.Style = ProgressBarStyle.Blocks;
                    row.ProgressBar.Value = state == DownloadState.Completed ? 100 : 0;
                    row.ProgressBar.SetBarColor(state switch
                    {
                        DownloadState.Completed => CompletedColor,
                        DownloadState.Failed => FailedColor,
                        _ => StoppedColor,
                    });
                    row.StatusLabel.Text = $"{state}";
                    AppendJobLog(row.Job, $"Job terminé (état : {state}).");
                    RefreshHistoryAsync();

                    if (state == DownloadState.Stopped)
                    {
                        AppendJobLog(row.Job, "Téléchargement interrompu.");
                    }
                    else
                    {
                        GenerateThumbnail(row.Job);
                        if (state == DownloadState.Completed)
                            ShowNotification("Enregistrement terminé", row.Job.RoomName);
                        else
                            ShowNotification("Erreur d'enregistrement", $"{row.Job.RoomName} : flux inaccessible ou interrompu de façon inattendue.", ToolTipIcon.Error);

                        // Reconnexion automatique (4.2) : uniquement si le job ne s'est
                        // PAS arrêté manuellement (cas déjà exclu ci-dessus) et que
                        // l'utilisateur a coché l'option pour cet enregistrement.
                        if (row.Job.AutoReconnectEnabled && row.Job.ReconnectAttempt < AppConfig.AutoReconnectMaxAttempts)
                            ScheduleReconnect(row);
                    }

                    // Le réencodage se fait toujours en post-traitement sur le fichier
                    // final, y compris après un arrêt manuel : yt-dlp ne réencode qu'à
                    // la fin normale d'un téléchargement, or un live s'arrête toujours
                    // par un Kill du process (STOP ou fermeture du formulaire), qui ne
                    // laisse jamais ce post-traitement interne s'exécuter.
                    if (row.Job.CodecChoice != "copy" && state != DownloadState.Failed)
                        ReencodeCaptureAsync(row.Job);
                    break;
            }
        }

        /// <summary>
        /// Reconnexion automatique (4.2) : programme une nouvelle tentative
        /// après le délai configuré. Le bouton Stop de la ligne devient
        /// "Annuler" tant que la reconnexion est en attente.
        /// </summary>
        private void ScheduleReconnect(JobRow row)
        {
            row.Job.ReconnectAttempt++;
            var attempt = row.Job.ReconnectAttempt;
            var delaySeconds = AppConfig.AutoReconnectDelaySeconds;

            AppendJobLog(row.Job, $"Reconnexion automatique dans {delaySeconds}s (tentative {attempt}/{AppConfig.AutoReconnectMaxAttempts})...");
            row.StatusLabel.Text = $"Reco. dans {delaySeconds}s...";
            row.StopButton.Text = "Annuler";

            var timer = new System.Windows.Forms.Timer { Interval = delaySeconds * 1000 };
            row.PendingReconnectTimer = timer;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                row.PendingReconnectTimer = null;
                if (!_jobRows.Contains(row)) return;

                AppendJobLog(row.Job, $"Nouvelle tentative de connexion ({attempt}/{AppConfig.AutoReconnectMaxAttempts})...");
                row.RestartEngine();
            };
            timer.Start();
        }

        // --- Couleurs dynamiques de la barre de progression (3.4) ---
        private static readonly Color RunningColor   = Color.FromArgb(0, 120, 215);
        private static readonly Color CompletedColor = Color.FromArgb(16, 137, 62);
        private static readonly Color FailedColor    = Color.FromArgb(196, 43, 28);
        private static readonly Color StoppedColor   = Color.FromArgb(120, 120, 120);

        /// <summary>
        /// Effet "pulse" (3.4) au démarrage d'un enregistrement : alterne
        /// brièvement entre une teinte éclaircie et la couleur définitive
        /// avant de s'y stabiliser, pour signaler visuellement le démarrage.
        /// </summary>
        private void PulseProgressBar(ProgressBar bar, Color settleColor)
        {
            var brighter = Color.FromArgb(255,
                Math.Min(255, settleColor.R + 70),
                Math.Min(255, settleColor.G + 70),
                Math.Min(255, settleColor.B + 70));

            var ticks = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 150 };
            timer.Tick += (s, e) =>
            {
                if (bar.IsDisposed) { timer.Stop(); timer.Dispose(); return; }

                bar.SetBarColor(ticks % 2 == 0 ? brighter : settleColor);
                ticks++;
                if (ticks >= 6)
                {
                    timer.Stop();
                    timer.Dispose();
                    bar.SetBarColor(settleColor);
                }
            };
            timer.Start();
        }

        private FileInfo? FindLatestCaptureFile(RecordingJob job)
        {
            if (!Directory.Exists(job.CaptureDir)) return null;

            return new DirectoryInfo(job.CaptureDir)
                .GetFiles($"{job.RoomName}-*.{job.ContainerExt}")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();
        }

        private static readonly string[] VideoExtensions = { ".mp4", ".mkv", ".mov" };

        /// <summary>
        /// Historique des enregistrements (4.4) : scan du dossier de capture en
        /// arrière-plan (Task.Run — énumération de fichiers + éventuels appels
        /// ffprobe ne doivent jamais geler l'UI), résultat appliqué au ListView
        /// via SafeInvoke. Limité aux 50 fichiers les plus récents pour éviter
        /// un scan trop lourd sur un dossier avec un historique important.
        /// </summary>
        private void RefreshHistoryAsync()
        {
            var captureDir = AppConfig.CaptureDir;
            var ffprobePath = AppConfig.FFprobePath;
            var hasFFprobe = File.Exists(ffprobePath);

            Task.Run(() =>
            {
                var entries = new List<(string Name, long Size, string Duration, DateTime Date, string FullPath)>();
                try
                {
                    if (Directory.Exists(captureDir))
                    {
                        entries.AddRange(new DirectoryInfo(captureDir)
                            .GetFiles()
                            .Where(f => VideoExtensions.Contains(f.Extension.ToLowerInvariant()))
                            .OrderByDescending(f => f.LastWriteTime)
                            .Take(50)
                            .Select(f => (
                                f.Name,
                                f.Length,
                                hasFFprobe ? ProbeDuration(ffprobePath, f.FullName) : "N/A",
                                f.LastWriteTime,
                                f.FullName
                            )));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors du scan de l'historique : {ex.Message}", LogLevel.WARN);
                }

                SafeInvoke(() =>
                {
                    historyListView.Items.Clear();
                    foreach (var entry in entries)
                    {
                        var item = new ListViewItem(entry.Name);
                        item.SubItems.Add(FormatSize(entry.Size));
                        item.SubItems.Add(entry.Duration);
                        item.SubItems.Add(entry.Date.ToString("dd/MM/yyyy HH:mm"));
                        item.Tag = entry.FullPath;
                        historyListView.Items.Add(item);
                    }
                });
            });
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "o", "Ko", "Mo", "Go" };
            double size = bytes;
            var unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            return $"{size:0.#} {units[unitIndex]}";
        }

        private static string ProbeDuration(string ffprobePath, string filePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                };
                foreach (var a in new[] { "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", filePath })
                    psi.ArgumentList.Add(a);

                using var p = Process.Start(psi);
                if (p == null) return "N/A";

                var output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(5000);

                if (double.TryParse(output, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
                {
                    var ts = TimeSpan.FromSeconds(seconds);
                    return ts.Hours > 0 ? $"{(int)ts.TotalHours}h{ts.Minutes:00}" : $"{ts.Minutes}m{ts.Seconds:00}";
                }
            }
            catch
            {
                // Non bloquant : "N/A" en cas d'échec (ffprobe manquant, fichier corrompu...).
            }
            return "N/A";
        }

        private void OnOpenHistoryFolderClick(object? sender, EventArgs e)
        {
            try
            {
                if (historyListView.SelectedItems.Count > 0 &&
                    historyListView.SelectedItems[0].Tag is string path && File.Exists(path))
                {
                    var psi = new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = false };
                    psi.ArgumentList.Add($"/select,{path}");
                    Process.Start(psi);
                }
                else
                {
                    Process.Start(new ProcessStartInfo(AppConfig.CaptureDir) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le dossier : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Génère la miniature en arrière-plan (Task.Run) : l'appel ffmpeg
        /// tournait auparavant en synchrone sur le thread UI (jusqu'à 15s de
        /// gel possible), ce qui contredit le principe "ffmpeg jamais sur le
        /// thread principal" (2.2). Même traitement que ReencodeCaptureAsync.
        /// </summary>
        private void GenerateThumbnail(RecordingJob job)
        {
            var videoFile = FindLatestCaptureFile(job);
            if (videoFile == null)
            {
                AppendJobLog(job, "Aucune vidéo trouvée.");
                return;
            }

            var thumbnail = Path.Combine(videoFile.DirectoryName!, Path.GetFileNameWithoutExtension(videoFile.Name) + ".jpg");

            Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = AppConfig.FFmpegPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    foreach (var a in new[]
                    {
                        "-ss", AppConfig.ThumbnailOffsetSeconds.ToString(),
                        "-i", videoFile.FullName,
                        "-frames:v", "1",
                        "-q:v", "2",
                        thumbnail,
                        "-y",
                        "-loglevel", "error"
                    })
                    {
                        psi.ArgumentList.Add(a);
                    }

                    using var p = Process.Start(psi);
                    p?.WaitForExit(15000);

                    SafeInvoke(() => AppendJobLog(job, File.Exists(thumbnail)
                        ? $"Miniature créée : {thumbnail}"
                        : "Erreur création miniature."));
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de la génération de la miniature : {ex.Message}", LogLevel.WARN);
                }
            });
        }

        /// <summary>
        /// Réencode le fichier capturé en post-traitement (voir commentaire dans
        /// HandleJobStateChanged pour la raison : yt-dlp ne peut pas le faire
        /// lui-même après un Kill de process). Tourne en arrière-plan (Task.Run)
        /// pour ne pas geler l'UI le temps de l'encodage, qui peut durer plusieurs
        /// minutes. Produit un fichier séparé (suffixe -h264/-h265) : la capture
        /// d'origine n'est jamais modifiée ni supprimée, même si l'encodage échoue.
        /// </summary>
        private void ReencodeCaptureAsync(RecordingJob job)
        {
            var videoFile = FindLatestCaptureFile(job);
            if (videoFile == null)
            {
                AppendJobLog(job, "Réencodage annulé : aucune vidéo trouvée.");
                return;
            }

            var codec = job.CodecChoice;
            var codecArgs = codec == "h265"
                ? new[] { "-c:v", "libx265", "-crf", "28", "-preset", "medium" }
                : new[] { "-c:v", "libx264", "-crf", "23", "-preset", "medium" };

            // Utilise l'extension du fichier source (pas job.ContainerExt, au cas où
            // un nouveau job pour la même room aurait déjà démarré entre-temps).
            var outputFile = Path.Combine(videoFile.DirectoryName!,
                $"{Path.GetFileNameWithoutExtension(videoFile.Name)}-{codec}{videoFile.Extension}");

            AppendJobLog(job, $"Réencodage ({codec}) démarré en arrière-plan : {outputFile}");

            Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = AppConfig.FFmpegPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    foreach (var a in new[] { "-i", videoFile.FullName }
                        .Concat(codecArgs)
                        .Concat(new[] { "-c:a", "aac", outputFile, "-y", "-loglevel", "error" }))
                    {
                        psi.ArgumentList.Add(a);
                    }

                    using var p = Process.Start(psi);
                    p?.WaitForExit();

                    var ok = p != null && p.ExitCode == 0 && File.Exists(outputFile);
                    SafeInvoke(() => AppendJobLog(job, ok
                        ? $"Réencodage ({codec}) terminé : {outputFile}"
                        : $"Échec du réencodage ({codec})."));
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors du réencodage ({codec}) : {ex.Message}", LogLevel.WARN);
                    SafeInvoke(() => AppendJobLog(job, $"Erreur réencodage : {ex.Message}"));
                }
            });
        }

        // ------------------------------------------------------------------
        // Événements
        // ------------------------------------------------------------------

        private void OnStartClick(object? sender, EventArgs e)
        {
            var urlInput = urlTextBox.Text.Trim();

            if (!UrlValidator.IsSafeUrl(urlInput, AppConfig.Whitelist, AppConfig.Blacklist))
            {
                MessageBox.Show("URL refusée par la validation de sécurité (voir log de session).", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!BinaryVerifier.VerifyTrustedBinary(AppConfig.YtDlpPath, AppConfig.YtDlpExpectedSha256,
                    AppConfig.YtDlpRequireAuthenticode, AppConfig.YtDlpExpectedSignerThumbprint, AppConfig.YtDlpExpectedSignerSubject))
            {
                MessageBox.Show("Échec vérification yt-dlp.exe. Renseigne AppConfig.YtDlpExpectedSha256.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!BinaryVerifier.VerifyTrustedBinary(AppConfig.FFmpegPath, AppConfig.FfmpegExpectedSha256,
                    AppConfig.FfmpegRequireAuthenticode, AppConfig.FfmpegExpectedSignerThumbprint, AppConfig.FfmpegExpectedSignerSubject))
            {
                MessageBox.Show("Échec vérification ffmpeg.exe. Renseigne AppConfig.FfmpegExpectedSha256.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (AppConfig.EnableCaPinning)
            {
                var ytOk = BinaryVerifier.VerifyCaPinning(AppConfig.YtDlpPath, AppConfig.TrustedCaThumbprint, AppConfig.TrustedCaIssuer);
                var ffOk = ytOk && BinaryVerifier.VerifyCaPinning(AppConfig.FFmpegPath, AppConfig.TrustedCaThumbprint, AppConfig.TrustedCaIssuer);
                if (!ffOk)
                {
                    MessageBox.Show("Échec du pinning CA pour les binaires.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Logger.Log("CA pinning activé et validé pour yt-dlp.exe et ffmpeg.exe.");
            }
            else
            {
                Logger.Log("CA pinning désactivé (binaires non signés).");
            }

            // Sandbox dossier : re-validation défensive à chaque lancement (protège aussi
            // contre un remplacement du dossier par un lien symbolique entre-temps).
            if (!PathValidator.IsValidPath(AppConfig.CaptureDir))
            {
                MessageBox.Show($"Dossier de sortie invalide ou interdit par la sandbox : {AppConfig.CaptureDir}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var uri = new Uri(urlInput);

            if (AppConfig.EnableTlsServerPinning)
            {
                if (!CertificateValidator.VerifyRemoteCertificate(uri.Host, 443, AppConfig.ServerExpectedThumbprint, AppConfig.ServerExpectedIssuer))
                {
                    MessageBox.Show($"Échec de la vérification TLS du serveur distant ({uri.Host}).", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Logger.Log($"TLS server pinning activé et validé pour {uri.Host}.");
            }
            else
            {
                Logger.Log("TLS server pinning désactivé (validation TLS native uniquement).");
            }

            var roomName = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(roomName)) roomName = "Chaturbate";

            if (_jobRows.Any(r => r.Job.RoomName == roomName && r.Job.Engine.State == DownloadState.Running))
            {
                MessageBox.Show($"Un enregistrement pour '{roomName}' est déjà en cours.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Chemin de log seulement validé une fois ici : seul l'horodatage
            // change entre tentatives, ce qui ne peut pas rendre un chemin déjà
            // valide invalide.
            if (!PathValidator.IsValidPath(Path.Combine(AppConfig.LogDir, $"{roomName}-test.log")))
            {
                MessageBox.Show("Chemin de log invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var codecChoice = codecCombo.SelectedIndex switch
            {
                1 => "h264",
                2 => "h265",
                _ => "copy"
            };
            var formatSelector = qualityCombo.SelectedIndex switch
            {
                1 => "bestvideo[height<=720]+bestaudio/best[height<=720]/best",
                2 => "worst",
                _ => null
            };
            var containerExt = formatCombo.SelectedIndex switch
            {
                1 => "mkv",
                2 => "mov",
                _ => "mp4"
            };

            var job = new RecordingJob
            {
                RoomName = roomName,
                SourceUrl = urlInput,
                CaptureDir = AppConfig.CaptureDir,
                CodecChoice = codecChoice,
                ContainerExt = containerExt,
                AutoReconnectEnabled = autoReconnectCheckbox.Checked,
            };

            var row = BuildJobRow(job);

            // Capturée ici et réutilisée telle quelle pour les reconnexions
            // automatiques (4.2) : régénère un horodatage frais à chaque appel,
            // donc un nouveau fichier de sortie/log à chaque tentative.
            void StartEngine()
            {
                var time = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var logFilePath = Path.Combine(AppConfig.LogDir, $"{roomName}-{time}.log");
                var outputPath  = Path.Combine(job.CaptureDir, $"{roomName}-{time}.%(ext)s");

                job.Engine.Start(AppConfig.YtDlpPath, AppConfig.FFmpegPath, urlInput, outputPath, logFilePath, formatSelector, containerExt,
                    AppConfig.CookiesFilePath, AppConfig.ProxyUrl, AppConfig.YtDlpWatchdogTimeoutSeconds, AppConfig.LogMaxFileSizeBytes);
            }
            row.RestartEngine = StartEngine;

            _jobRows.Add(row);
            jobsListPanel.Controls.Add(row.Container);

            AppendJobLog(job, "Démarrage de l'enregistrement...");

            try
            {
                StartEngine();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible de démarrer le téléchargement : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                jobsListPanel.Controls.Remove(row.Container);
                _jobRows.Remove(row);
            }
        }

        private void OnStopAllClick(object? sender, EventArgs e)
        {
            foreach (var row in _jobRows.ToList())
            {
                if (row.Job.Engine.State == DownloadState.Running)
                    row.Job.Engine.Stop();
            }
        }

        private void OnBrowseCaptureDirClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choisis le dossier de sauvegarde des enregistrements",
                UseDescriptionForTitle = true,
                SelectedPath = Directory.Exists(AppConfig.CaptureDir) ? AppConfig.CaptureDir : AppConfig.AppDir,
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var chosen = dialog.SelectedPath;
            if (!PathValidator.IsValidPath(chosen))
            {
                MessageBox.Show("Dossier invalide ou interdit par la sandbox de chemins.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AppConfig.CaptureDir = chosen;
            Directory.CreateDirectory(AppConfig.CaptureDir);
            saveDirTextBox.Text = AppConfig.CaptureDir;

            _settings.CaptureDir = AppConfig.CaptureDir;
            SettingsManager.Save(_settings);

            AppendLog($"[{DateTime.Now:HH:mm:ss}] Dossier de sauvegarde changé : {AppConfig.CaptureDir}");
        }

        private void OnBrowseCookiesClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choisis un fichier cookies.txt (format Netscape, exporté depuis ton navigateur)",
                Filter = "Fichiers cookies (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            if (!PathValidator.IsValidPath(dialog.FileName, mustExist: true))
            {
                MessageBox.Show("Fichier invalide ou interdit par la sandbox de chemins.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AppConfig.CookiesFilePath = dialog.FileName;
            cookiesTextBox.Text = AppConfig.CookiesFilePath;

            _settings.CookiesFilePath = AppConfig.CookiesFilePath;
            SettingsManager.Save(_settings);

            AppendLog($"[{DateTime.Now:HH:mm:ss}] Fichier cookies changé : {AppConfig.CookiesFilePath}");
        }

        private void OnProxyTextChanged(object? sender, EventArgs e)
        {
            AppConfig.ProxyUrl = proxyTextBox.Text.Trim();
            _settings.ProxyUrl = AppConfig.ProxyUrl;
            SettingsManager.Save(_settings);
        }

        private void OnAddFavoriteClick(object? sender, EventArgs e)
        {
            var url = urlTextBox.Text.Trim();
            if (!_favorites.AddFavorite(url))
            {
                MessageBox.Show("URL invalide ou déjà présente dans les favoris.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            favoritesListBox.Items.Add(url);
            ShowNotification("Favori ajouté", url);
        }

        private void OnRemoveFavoriteClick(object? sender, EventArgs e)
        {
            if (favoritesListBox.SelectedItem is not string selected) return;
            _favorites.RemoveFavorite(selected);
            favoritesListBox.Items.Remove(selected);
        }

        private void OnLoadFavoriteClick(object? sender, EventArgs e)
        {
            if (favoritesListBox.SelectedItem is string selected)
                urlTextBox.Text = selected;
        }

        private void OnDonateClick(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(AppConfig.DonateUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le lien de don : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnWebsiteClick(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(AppConfig.WebsiteUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le site web : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            var theme = themeCombo.SelectedItem?.ToString() == "Sombre" ? AppTheme.Dark : AppTheme.Light;
            ThemeManager.Apply(this, theme);
            ApplyIcons();
        }

        /// <summary>
        /// Couleur des icônes (3.1) alignée sur le thème actif — les boutons
        /// reçoivent déjà ce même ForeColor via ThemeManager, donc l'icône
        /// reste lisible sur le fond du bouton dans les deux thèmes.
        /// </summary>
        private Color IconColor =>
            themeCombo.SelectedItem?.ToString() == "Sombre" ? Color.WhiteSmoke : Color.Black;

        /// <summary>
        /// (Ré)assigne les icônes vectorielles de tous les boutons fixes, ainsi
        /// que des lignes d'enregistrement déjà créées (les nouvelles lignes se
        /// colorent elles-mêmes à la création dans BuildJobRow).
        /// </summary>
        private void ApplyIcons()
        {
            var color = IconColor;

            void SetIcon(Button button, string icon, int size = 16)
            {
                button.Image = IconManager.Render(icon, size, color);
                button.TextImageRelation = TextImageRelation.ImageBeforeText;
                button.ImageAlign = ContentAlignment.MiddleLeft;
                button.TextAlign = ContentAlignment.MiddleRight;
            }

            SetIcon(startButton, "play");
            SetIcon(stopAllButton, "stop");
            SetIcon(browseDirButton, "folder");
            SetIcon(checkUpdateButton, "update");
            SetIcon(tutorialButton, "book");
            SetIcon(websiteButton, "globe");

            foreach (var row in _jobRows)
            {
                row.StopButton.Image = IconManager.Render("stop", 14, color);
                row.OpenButton.Image = IconManager.Render("open", 14, color);
                row.StopButton.TextImageRelation = TextImageRelation.ImageBeforeText;
                row.OpenButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            }
        }

        /// <summary>
        /// Mode simple / avancé (3.2). En mode simple : uniquement URL,
        /// Démarrer/Tout arrêter et la liste des enregistrements en cours.
        /// En mode avancé : tout (qualité/codec/format, dossier, cookies/proxy,
        /// thème, guide, mises à jour, favoris, don, logs). Le choix est
        /// mémorisé entre les lancements.
        /// </summary>
        private void ApplyUiMode(bool advanced)
        {
            _advancedMode = advanced;

            const int grpRecordY = 40;
            const int grpRecordHeightAdvanced = 310;
            const int grpRecordHeightSimple = 110;

            advancedOptionsPanel.Visible = advanced;
            themeLabel.Visible = advanced;
            themeCombo.Visible = advanced;
            tutorialButton.Visible = advanced;
            checkUpdateButton.Visible = advanced;
            grpHistory.Visible = advanced;
            grpFavorites.Visible = advanced;
            grpDonate.Visible = advanced;
            grpLogs.Visible = advanced;

            grpRecord.Height = advanced ? grpRecordHeightAdvanced : grpRecordHeightSimple;

            var progressY = grpRecordY + grpRecord.Height + 8;
            grpProgress.Location = new Point(12, progressY);

            int naturalHeight;
            if (advanced)
            {
                grpHistory.Location = new Point(12, progressY + grpProgress.Height + 8);
                grpFavorites.Location = new Point(12, grpHistory.Bottom + 8);
                grpDonate.Location = new Point(12, grpFavorites.Bottom + 8);
                grpLogs.Location = new Point(12, grpDonate.Bottom + 8);
                naturalHeight = grpLogs.Bottom + 20;
            }
            else
            {
                naturalHeight = progressY + grpProgress.Height + 20;
            }

            // Taille "naturelle" du contenu : appliquée à la fenêtre pour un
            // confort immédiat au changement de mode, et comme plancher de
            // défilement (AutoScrollMinSize) si l'utilisateur réduit ensuite
            // la fenêtre manuellement en dessous de cette taille.
            ClientSize = new Size(700, naturalHeight);
            contentPanel.AutoScrollMinSize = new Size(700, naturalHeight);

            modeToggleButton.Text = advanced ? "Mode simple" : "Mode avancé";

            _settings.AdvancedMode = advanced;
            SettingsManager.Save(_settings);
        }

        private async void OnCheckUpdateClick(object? sender, EventArgs e)
        {
            checkUpdateButton.Enabled = false;
            try
            {
                var update = await UpdateChecker.CheckForUpdateAsync(CurrentVersion);
                if (update == null)
                {
                    MessageBox.Show(this, $"Tu utilises déjà la dernière version (v{CurrentVersion}).", "Mises à jour", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var runningJobs = _jobRows.Count(r => r.Job.Engine.State == DownloadState.Running);
                var warning = runningJobs > 0
                    ? $"\n\n⚠ {runningJobs} enregistrement(s) en cours seront interrompus par le redémarrage."
                    : "";

                var result = MessageBox.Show(this,
                    $"Version v{update.Version} disponible (actuelle : v{CurrentVersion}).\n\nTélécharger et installer maintenant ? L'application redémarrera automatiquement.{warning}",
                    "Mise à jour disponible", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes) return;

                AppendLog($"[{DateTime.Now:HH:mm:ss}] Téléchargement de la mise à jour v{update.Version}...");
                await UpdateInstaller.DownloadAndInstallAsync(update.DownloadUrl, AppConfig.AppDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Échec de la vérification des mises à jour : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                checkUpdateButton.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var row in _jobRows)
                row.Job.Engine.Stop();

            // Retire l'icône de la zone de notification avant fermeture : sans
            // ça, Windows laisse une icône "fantôme" jusqu'au survol suivant.
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Notification "toast" (3.3) via la zone de notification — le style
        /// visuel moderne est appliqué automatiquement par Windows 10/11 sans
        /// dépendance supplémentaire. Volontairement non bloquant : l'absence
        /// de notification ne doit jamais interrompre l'appli.
        /// </summary>
        private void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try { _notifyIcon.ShowBalloonTip(4000, title, message, icon); }
            catch (Exception ex) { Logger.Log($"Impossible d'afficher la notification : {ex.Message}", LogLevel.WARN); }
        }

        // ------------------------------------------------------------------
        // Construction de l'UI — équivalent du bloc de création de contrôles
        // WinForms du script PowerShell. Écrit à la main (pas de fichier
        // .Designer.cs séparé) ; libre à toi de le scinder si tu ouvres le
        // projet dans Visual Studio avec le concepteur de formulaires.
        // ------------------------------------------------------------------
        private void InitializeComponent()
        {
            SuspendLayout();

            Text = $"Chaturbate Recorder v{CurrentVersion}";
            ClientSize = new Size(700, 960);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimumSize = new Size(500, 300);

            // Panneau conteneur avec défilement automatique : la fenêtre est
            // redimensionnable librement (agrandir/réduire/maximiser), et si
            // elle devient plus petite que la taille naturelle du contenu
            // (calculée dans ApplyUiMode via AutoScrollMinSize), des barres de
            // défilement apparaissent au lieu de couper le contenu.
            contentPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // Réutilise l'icône déjà embarquée dans l'exe (via <ApplicationIcon> dans
            // le .csproj) pour la fenêtre/barre des tâches — pas de fichier séparé à
            // copier, l'icône EXE et l'icône de fenêtre restent toujours cohérentes.
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception ex) { Logger.Log($"Impossible de charger l'icône : {ex.Message}", LogLevel.WARN); }

            // Icône de la zone de notification (3.3) : uniquement pour porter les
            // notifications de type "toast" (ShowBalloonTip) — enregistrement
            // terminé/en erreur, favori ajouté.
            _notifyIcon = new NotifyIcon
            {
                Icon = Icon ?? SystemIcons.Application,
                Text = "Chaturbate Recorder",
                Visible = true,
            };

            themeLabel = new Label { Text = "Thème :", Location = new Point(12, 12), AutoSize = true };
            themeCombo = new ComboBox
            {
                Location = new Point(70, 9),
                Size = new Size(120, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            themeCombo.Items.AddRange(new object[] { "Clair", "Sombre" });
            themeCombo.SelectedItem = "Clair";
            themeCombo.SelectedIndexChanged += OnThemeChanged;

            checkUpdateButton = new Button { Text = "Rechercher une mise à jour", Location = new Point(500, 9), Size = new Size(172, 24) };
            checkUpdateButton.Click += OnCheckUpdateClick;

            tutorialButton = new Button { Text = "Guide de démarrage", Location = new Point(210, 9), Size = new Size(160, 24) };
            tutorialButton.Click += (s, e) => ShowTutorial();

            modeToggleButton = new Button { Location = new Point(380, 9), Size = new Size(110, 24) };
            modeToggleButton.Click += (s, e) => ApplyUiMode(!_advancedMode);

            // --- GroupBox : Enregistrement ---
            grpRecord = new GroupBox { Text = "Enregistrement", Location = new Point(12, 40), Size = new Size(660, 272) };
            var urlLabel = new Label { Text = "URL Chaturbate :", Location = new Point(12, 25), AutoSize = true };
            urlTextBox = new TextBox { Location = new Point(12, 48), Size = new Size(420, 24) };
            startButton = new Button { Text = "Démarrer", Location = new Point(445, 46), Size = new Size(95, 28) };
            stopAllButton = new Button { Text = "Tout arrêter", Location = new Point(548, 46), Size = new Size(95, 28) };
            addFavoriteButton = new Button { Text = "+ Favori", Location = new Point(445, 78), Size = new Size(198, 24) };

            // Options avancées (qualité/codec/format, dossier, cookies/proxy) :
            // regroupées dans un panneau dédié pour pouvoir les masquer en bloc
            // en Mode simple (3.2), coordonnées relatives au panneau lui-même.
            advancedOptionsPanel = new Panel { Location = new Point(0, 100), Size = new Size(660, 200) };

            var qualityLabel = new Label { Text = "Qualité source :", Location = new Point(12, 12), AutoSize = true };
            qualityCombo = new ComboBox
            {
                Location = new Point(12, 30),
                Size = new Size(190, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            qualityCombo.Items.AddRange(new object[]
            {
                "Meilleure qualité (recommandé)",
                "Qualité moyenne (720p max)",
                "Qualité minimale (économie)"
            });
            qualityCombo.SelectedIndex = 0;

            var codecLabel = new Label { Text = "Codec de sortie :", Location = new Point(214, 12), AutoSize = true };
            codecCombo = new ComboBox
            {
                Location = new Point(214, 30),
                Size = new Size(260, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            codecCombo.Items.AddRange(new object[]
            {
                "Copie sans réencodage (recommandé, rapide)",
                "H.264 (libx264 — compatibilité universelle)",
                "H.265 (libx265 — fichier plus léger)"
            });
            codecCombo.SelectedIndex = 0;

            var formatLabel = new Label { Text = "Format de sortie :", Location = new Point(486, 12), AutoSize = true };
            formatCombo = new ComboBox
            {
                Location = new Point(486, 30),
                Size = new Size(150, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            formatCombo.Items.AddRange(new object[]
            {
                "MP4 (recommandé)",
                "MKV (plus robuste)",
                "MOV"
            });
            formatCombo.SelectedIndex = 0;

            var saveDirLabel = new Label { Text = "Dossier de sauvegarde :", Location = new Point(12, 66), AutoSize = true };
            saveDirTextBox = new TextBox
            {
                Location = new Point(12, 84),
                Size = new Size(520, 24),
                ReadOnly = true,
                Text = AppConfig.CaptureDir
            };
            browseDirButton = new Button { Text = "Parcourir...", Location = new Point(542, 84), Size = new Size(106, 24) };

            var cookiesLabel = new Label { Text = "Cookies (optionnel) :", Location = new Point(12, 120), AutoSize = true };
            cookiesTextBox = new TextBox
            {
                Location = new Point(12, 138),
                Size = new Size(260, 24),
                ReadOnly = true,
                Text = AppConfig.CookiesFilePath
            };
            browseCookiesButton = new Button { Text = "...", Location = new Point(278, 138), Size = new Size(40, 24) };

            var proxyLabel = new Label { Text = "Proxy SOCKS5/HTTP (optionnel) :", Location = new Point(330, 120), AutoSize = true };
            proxyTextBox = new TextBox
            {
                Location = new Point(330, 138),
                Size = new Size(306, 24),
                Text = AppConfig.ProxyUrl,
                PlaceholderText = "ex: socks5://127.0.0.1:9050"
            };

            autoReconnectCheckbox = new CheckBox
            {
                Text = "Reconnexion automatique si le live se termine de façon inattendue",
                Location = new Point(12, 172),
                AutoSize = true,
            };

            startButton.Click += OnStartClick;
            stopAllButton.Click += OnStopAllClick;
            addFavoriteButton.Click += OnAddFavoriteClick;
            browseDirButton.Click += OnBrowseCaptureDirClick;
            browseCookiesButton.Click += OnBrowseCookiesClick;
            proxyTextBox.Leave += OnProxyTextChanged;

            advancedOptionsPanel.Controls.AddRange(new Control[]
            {
                qualityLabel, qualityCombo, codecLabel, codecCombo, formatLabel, formatCombo,
                saveDirLabel, saveDirTextBox, browseDirButton,
                cookiesLabel, cookiesTextBox, browseCookiesButton, proxyLabel, proxyTextBox,
                autoReconnectCheckbox
            });

            grpRecord.Controls.AddRange(new Control[]
            {
                urlLabel, urlTextBox, startButton, stopAllButton, addFavoriteButton,
                advancedOptionsPanel
            });

            // --- GroupBox : Enregistrements en cours (plusieurs jobs possibles) ---
            grpProgress = new GroupBox { Text = "Enregistrements en cours", Location = new Point(12, 320), Size = new Size(660, 140) };
            jobsListPanel = new FlowLayoutPanel
            {
                Location = new Point(12, 22),
                Size = new Size(636, 108),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
            };
            grpProgress.Controls.Add(jobsListPanel);

            // --- GroupBox : Historique des enregistrements (4.4) ---
            grpHistory = new GroupBox { Text = "Historique des enregistrements", Location = new Point(12, 468), Size = new Size(660, 130) };
            historyListView = new ListView
            {
                Location = new Point(12, 22),
                Size = new Size(500, 98),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
            };
            historyListView.Columns.Add("Fichier", 220);
            historyListView.Columns.Add("Taille", 80);
            historyListView.Columns.Add("Durée", 70);
            historyListView.Columns.Add("Date", 120);

            refreshHistoryButton = new Button { Text = "Actualiser", Location = new Point(522, 22), Size = new Size(120, 26) };
            openHistoryFolderButton = new Button { Text = "Ouvrir dossier", Location = new Point(522, 54), Size = new Size(120, 26) };

            refreshHistoryButton.Click += (s, e) => RefreshHistoryAsync();
            openHistoryFolderButton.Click += OnOpenHistoryFolderClick;

            grpHistory.Controls.AddRange(new Control[] { historyListView, refreshHistoryButton, openHistoryFolderButton });

            // --- GroupBox : Favoris ---
            grpFavorites = new GroupBox { Text = "Favoris", Location = new Point(12, 468), Size = new Size(660, 130) };
            favoritesListBox = new ListBox { Location = new Point(12, 22), Size = new Size(500, 98) };
            loadFavoriteButton = new Button { Text = "Charger", Location = new Point(522, 22), Size = new Size(120, 26) };
            removeFavoriteButton = new Button { Text = "Supprimer favori", Location = new Point(522, 54), Size = new Size(120, 26) };

            loadFavoriteButton.Click += OnLoadFavoriteClick;
            removeFavoriteButton.Click += OnRemoveFavoriteClick;
            favoritesListBox.DoubleClick += OnLoadFavoriteClick;

            grpFavorites.Controls.AddRange(new Control[] { favoritesListBox, loadFavoriteButton, removeFavoriteButton });

            // --- GroupBox : Soutenir le projet ---
            grpDonate = new GroupBox { Text = "Soutenir le projet", Location = new Point(12, 606), Size = new Size(660, 100) };
            donateButton = new Button { Text = "Faire un don (PayPal)", Location = new Point(12, 34), Size = new Size(220, 32) };
            websiteButton = new Button { Text = "Site web", Location = new Point(12, 70), Size = new Size(220, 22) };
            qrPictureBox = new PictureBox
            {
                Location = new Point(250, 12),
                Size = new Size(76, 76),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            donateLabel = new Label
            {
                Text = "Scanne le QR code avec ton téléphone, ou clique sur le bouton.",
                Location = new Point(340, 40),
                Size = new Size(300, 40)
            };

            donateButton.Click += OnDonateClick;
            websiteButton.Click += OnWebsiteClick;

            grpDonate.Controls.AddRange(new Control[] { donateButton, websiteButton, qrPictureBox, donateLabel });

            // --- GroupBox : Logs ---
            grpLogs = new GroupBox { Text = "Logs", Location = new Point(12, 714), Size = new Size(660, 220) };
            logListBox = new ListBox
            {
                Location = new Point(12, 22),
                Size = new Size(636, 186),
                HorizontalScrollbar = true
            };
            grpLogs.Controls.Add(logListBox);

            contentPanel.Controls.AddRange(new Control[] { themeLabel, themeCombo, tutorialButton, checkUpdateButton, modeToggleButton, grpRecord, grpProgress, grpHistory, grpFavorites, grpDonate, grpLogs });
            Controls.Add(contentPanel);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
