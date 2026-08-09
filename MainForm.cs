using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using SentinelGuard;

namespace ChaturbateRecorderApp
{
    public class MainForm : Form
    {
        // --- Contrôles ---
        private Panel contentPanel = null!;
        private ThemedButton paramsButton = null!;
        private ThemedButton checkUpdateButton = null!;
        private ThemedButton tutorialButton = null!;
        private ThemedButton reportBugButton = null!;
        private ThemedButton diagnosticButton = null!;
        private ThemedButton modeToggleButton = null!;
        private Label urlLabel = null!;
        private TextBox urlTextBox = null!;
        private ThemedButton startButton = null!;
        private ThemedButton stopAllButton = null!;
        private ThemedButton addFavoriteButton = null!;
        private FlowLayoutPanel jobsListPanel = null!;
        private Panel advancedOptionsPanel = null!;
        private RoundedGroupPanel grpRecord = null!;
        private RoundedGroupPanel grpProgress = null!;
        private RoundedGroupPanel grpHistory = null!;
        private ListView historyListView = null!;
        private ThemedButton refreshHistoryButton = null!;
        private ThemedButton openHistoryFolderButton = null!;
        // 4.1 : ouverture directe de la vidéo, et miniatures dans la liste.
        private ThemedButton openHistoryFileButton = null!;
        private ImageList historyThumbnails = null!;
        private RoundedGroupPanel grpFavorites = null!;
        private RoundedGroupPanel grpDonate = null!;
        private RoundedGroupPanel grpLogs = null!;
        private Label qualityLabel = null!;
        private ThemedComboBox qualityCombo = null!;
        private Label codecLabel = null!;
        private ThemedComboBox codecCombo = null!;
        private Label formatLabel = null!;
        private ThemedComboBox formatCombo = null!;
        private Label durationLabel = null!;
        private ThemedComboBox durationCombo = null!;
        private ListBox favoritesListBox = null!;
        // 88.0 : surveillance automatique.
        private RoundedGroupPanel grpWatch = null!;
        private ListView watchListView = null!;
        private ThemedButton addWatchButton = null!;
        private ThemedButton removeWatchButton = null!;
        private ThemedButton loadFavoriteButton = null!;
        private ThemedButton removeFavoriteButton = null!;
        private ThemedButton sponsorButton = null!;
        private ThemedButton donateButton = null!;
        private ThemedButton websiteButton = null!;
        // 98.0 : note de legalite, rangee 1 donc visible dans les deux modes.
        private ThemedButton legalButton = null!;
        // 50.0 : réseaux sociaux (partage X/Reddit + dépôt GitHub).
        private ThemedButton shareXButton = null!;
        private ThemedButton shareRedditButton = null!;
        private ThemedButton githubButton = null!;
        private PictureBox qrPictureBox = null!;
        private Label donateLabel = null!;
        private ListBox logListBox = null!;

        /// <summary>
        /// Ce que doit afficher une ligne de job indépendamment de la langue
        /// courante — voir RefreshJobRowLabels, seul endroit qui traduit ces
        /// états en texte réel.
        /// </summary>
        private enum JobRowStatus { Preparing, Running, ReconnectPending, Cancelled, Finished }

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
            public ThemedProgressBar ProgressBar = null!;
            public Label StatusLabel = null!;
            public ThemedButton StopButton = null!;
            public ThemedButton OpenButton = null!;
            public Action RestartEngine = null!;
            public System.Windows.Forms.Timer? PendingReconnectTimer;

            public JobRowStatus Status = JobRowStatus.Preparing;
            public DownloadState? FinishedState;
            public int ReconnectDelaySeconds;
            public bool HasProgressPct;
            public double LastProgressPct;

            // Minuteur (87.0) : libellé du temps restant, à droite du nom, et
            // le timer d'affichage qui le rafraîchit chaque seconde. Ce même
            // timer déclenche l'arrêt à l'échéance — un seul objet à arrêter.
            public Label TimerLabel = null!;
            public System.Windows.Forms.Timer? CountdownTimer;
        }

        // --- État ---
        private readonly FavoritesManager _favorites = new();
        private readonly WatchListManager _watchList = new();
        private System.Windows.Forms.Timer? _watchTimer;
        // Empêche deux passages de se chevaucher : un contrôle prend quelques
        // secondes par salon, une liste fournie peut dépasser l'intervalle.
        private bool _watchTickRunning;
        private readonly List<JobRow> _jobRows = new();
        private bool _advancedMode = true;
        private AppTheme _currentTheme = AppTheme.Light;
        private AppLanguage _currentLanguage = AppLanguage.French;
        private NotifyIcon _notifyIcon = null!;
        private ToolStripMenuItem _trayOpenItem = null!;
        private ToolStripMenuItem _traySettingsItem = null!;
        private ToolStripMenuItem _trayCloseItem = null!;
        // Réduction dans la zone de notification (19.0) : le X masque la
        // fenêtre au lieu de fermer l'app, pour ne pas interrompre les
        // enregistrements en cours. Seul "Fermer" du menu de la zone de
        // notification déclenche la fermeture réelle (ce booléen le distingue
        // d'un clic sur le X, qui lève le même événement OnFormClosing).
        private bool _isReallyClosing;

        // Recherche automatique de mise à jour (79.0). Le timer tourne toujours
        // et c'est le tick qui relit _settings.AutoUpdateCheck : activer ou
        // désactiver le réglage dans la fenêtre Paramètres prend donc effet
        // immédiatement, sans redémarrage ni plomberie de rappel.
        private System.Windows.Forms.Timer? _autoUpdateTimer;
        private UpdateInfo? _pendingUpdate;
        private bool _autoUpdateCheckRunning;
        // Action associée à la dernière notification affichée, exécutée si
        // l'utilisateur clique dessus. Remise à null à chaque nouvelle
        // notification pour qu'un toast de fin d'enregistrement n'hérite pas
        // de l'action d'un toast de mise à jour affiché avant lui.
        private Action? _balloonClickAction;
        // 93.0 : évènement nommé signalé par une seconde instance.
        private EventWaitHandle? _secondInstanceEvent;

        private readonly UserSettings _settings;

        public MainForm()
        {
            _settings = SettingsManager.Load();
            _currentLanguage = _settings.Language == "en" ? AppLanguage.English : AppLanguage.French;
            // Réaligne sur le réglage persisté la valeur provisoire posée par
            // Program.Main depuis la langue de l'OS (24.0).
            Localization.Current = _currentLanguage;
            if (!string.IsNullOrWhiteSpace(_settings.CaptureDir))
            {
                if (PathValidator.IsValidPath(_settings.CaptureDir, mustExist: false, out var savedDirReason))
                    AppConfig.CaptureDir = _settings.CaptureDir;
                else
                    Logger.Log($"Dossier de capture enregistré ignoré ({_settings.CaptureDir}) : {savedDirReason}", LogLevel.WARN);
            }
            if (!string.IsNullOrWhiteSpace(_settings.CookiesFilePath) && File.Exists(_settings.CookiesFilePath))
                AppConfig.CookiesFilePath = _settings.CookiesFilePath;
            if (!string.IsNullOrWhiteSpace(_settings.ProxyUrl))
                AppConfig.ProxyUrl = _settings.ProxyUrl;

            InitializeComponent();

            Logger.Log("Application démarrée.");

            // Le motif est journalisé au point d'appel depuis la fusion avec
            // SentinelGuard : ses validateurs ne journalisent rien eux-mêmes.
            // Ça compte particulièrement ici — l'application s'arrête, et sans
            // la raison exacte l'utilisateur n'a qu'une boîte de dialogue
            // générique pour comprendre lequel des deux dossiers pose problème.
            if (!PathValidator.IsValidPath(AppConfig.CaptureDir, mustExist: false, out var captureReason) ||
                !PathValidator.IsValidPath(AppConfig.LogDir, mustExist: false, out captureReason))
            {
                Logger.Log($"Dossier de capture ou de logs refusé : {captureReason}", LogLevel.ERROR);
                MessageBox.Show(
                    Localization.Get("error.invalidCaptureOrLogDir"),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            // Ces deux appels tuaient l'application au demarrage quand le chemin
            // designait un disque absent — cas d'un reglage enregistre sur une
            // cle USB retiree, ou d'une valeur par defaut heritee d'une version
            // anterieure a 1.27.0. Un dossier de capture injoignable est un
            // probleme reel, mais il ne justifie pas de refuser de demarrer :
            // on retombe sur le defaut et on le dit.
            AppConfig.CaptureDir = DirectoryResolver.EnsureOrFallback(
                AppConfig.CaptureDir, AppConfig.DefaultCaptureDir(), AppConfig.AppDir, out var captureFellBack);
            AppConfig.LogDir = DirectoryResolver.EnsureOrFallback(
                AppConfig.LogDir, AppConfig.DefaultLogDir(), AppConfig.AppDir, out _);

            if (captureFellBack)
            {
                // Le reglage persiste est corrige, sinon l'avertissement
                // reviendrait a chaque lancement sans que rien ne change.
                _settings.CaptureDir = AppConfig.CaptureDir;
                SettingsManager.Save(_settings);
                MessageBox.Show(
                    Localization.Format("warn.captureDirFellBack", AppConfig.CaptureDir),
                    Localization.Get("dialog.info"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            SentinelGuard.LogFileRotator.PurgeOlderThan(AppConfig.LogDir, AppConfig.LogRetentionDays);

            WarnIfBroadWriteAccess(AppConfig.AppDir);
            WarnIfBroadWriteAccess(AppConfig.CaptureDir);
            WarnIfBroadWriteAccess(AppConfig.LogDir);

            ApplySafeMode();

            _favorites.Load();
            foreach (var fav in _favorites.Favorites)
                favoritesListBox.Items.Add(fav);

            _watchList.Load();
            foreach (var room in _watchList.Rooms)
                AddWatchRow(room);

            LoadQrImage();
            ThemeManager.Apply(this, _currentTheme);
            ApplyIcons();
            ApplyLanguage(_currentLanguage);
            ApplyUiMode(_settings.AdvancedMode ?? true, animate: false);
            RefreshHistoryAsync();
            ShowFirstRunDialogs();

            // Fondu d'ouverture (9.2) : la fenêtre démarre invisible (Opacity=0)
            // et remonte à pleine opacité une fois affichée — Shown ne se
            // déclenche qu'après Application.Run(new MainForm()), donc après les
            // dialogues de premier lancement éventuels ci-dessus.
            Opacity = 0;
            Shown += (s, e) => AnimateOpacity(1.0, 250);

            StartAutoUpdateChecks();
            ListenForSecondInstance();
            StartWatchLoop();
        }

        /// <summary>
        /// 93.0 — surveille l'évènement nommé que signale une seconde instance
        /// pour révéler cette fenêtre-ci. Thread d'arrière-plan (IsBackground)
        /// plutôt qu'un Timer : il attend sans consommer, et n'empêche pas
        /// l'application de se terminer. ShowMainWindow touchant à l'interface,
        /// il repasse par SafeInvoke.
        /// </summary>
        private void ListenForSecondInstance()
        {
            EventWaitHandle showEvent;
            try
            {
                showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ShowWindowEventName);
            }
            catch (Exception ex)
            {
                // Sans cet évènement l'application reste parfaitement utilisable :
                // seul le réveil par second lancement est perdu, le mutex ayant
                // déjà empêché la seconde instance de s'ouvrir.
                Logger.Log($"Surveillance du second lancement indisponible : {ex.Message}", LogLevel.WARN);
                return;
            }

            _secondInstanceEvent = showEvent;
            var thread = new Thread(() =>
            {
                while (showEvent.WaitOne())
                {
                    if (_isReallyClosing) return;
                    SafeInvoke(ShowMainWindow);
                }
            })
            { IsBackground = true, Name = "SecondInstanceWatcher" };
            thread.Start();
        }

        /// <summary>
        /// Rejoue les interrupteurs manuels, puis controle ce qui peut l'etre
        /// et desactive automatiquement ce qui est defaillant (29.0 / 2.2).
        ///
        /// **Regle** : aucune de ces defaillances n'empeche de demarrer. Un
        /// ffmpeg absent interdit le reencodage et les miniatures, pas la
        /// capture ; un cookies.txt illisible interdit le contenu reserve, pas
        /// le flux public. Ce qui etait auparavant un echec obscur au moment
        /// d'enregistrer — voire un echec TOTALEMENT silencieux pour le
        /// cookies.txt — devient un message clair au demarrage.
        /// </summary>
        private void ApplySafeMode()
        {
            SafeMode.LoadManual(_settings.DisabledComponents);
            SafeMode.ClearAutomatic();

            if (!File.Exists(AppConfig.FFmpegPath))
                SafeMode.DisableAutomatically(SafeComponent.Ffmpeg,
                    Localization.Format("safe.reason.ffmpegMissing", AppConfig.FFmpegPath));

            // Le controle du cookies.txt existe depuis la v1.26.1 mais ne
            // servait qu'au moment de le choisir : un fichier devenu invalide
            // apres coup (reexport rate, edition manuelle) repassait inapercu.
            if (!string.IsNullOrWhiteSpace(AppConfig.CookiesFilePath))
            {
                if (!File.Exists(AppConfig.CookiesFilePath))
                {
                    SafeMode.DisableAutomatically(SafeComponent.Cookies,
                        Localization.Format("safe.reason.cookiesMissing", AppConfig.CookiesFilePath));
                }
                else
                {
                    try
                    {
                        var check = CookieFileValidator.Validate(File.ReadAllLines(AppConfig.CookiesFilePath));
                        if (!check.IsValid)
                            SafeMode.DisableAutomatically(SafeComponent.Cookies,
                                Localization.Format("safe.reason.cookiesInvalid", check.Problem, check.Line));
                    }
                    catch (Exception ex)
                    {
                        SafeMode.DisableAutomatically(SafeComponent.Cookies, ex.Message);
                    }
                }
            }

            ReportAutomaticSafeMode();
        }

        /// <summary>
        /// Un seul message, listant tout ce qui a ete desactive et pourquoi.
        /// Rien ne s'affiche si tout va bien — c'est le cas courant, et une
        /// boite « tout va bien » a chaque demarrage serait vite fermee sans
        /// etre lue, ce qui la rendrait inutile le jour ou elle compte.
        /// </summary>
        private void ReportAutomaticSafeMode()
        {
            var disabled = SafeMode.AutomaticallyDisabled;
            if (disabled.Count == 0) return;

            var details = string.Join("\n\n", disabled.Select(c =>
                $"- {Localization.Get("safe.component." + c)} : {SafeMode.AutomaticReason(c)}"));

            AppendLog($"[{DateTime.Now:HH:mm:ss}] Safe Mode : {disabled.Count} composant(s) desactive(s).");
            MessageBox.Show(this,
                Localization.Get("safe.intro") + "\n\n" + details + "\n\n" + Localization.Get("safe.outro"),
                Localization.Get("safe.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string CurrentVersion =>
            typeof(MainForm).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        /// <summary>
        /// Sur un tout premier lancement (aucune version jamais vue), affiche le
        /// tutoriel plutôt que le changelog complet — moins redondant pour un
        /// nouvel utilisateur qui ne connaît encore aucune fonctionnalité.
        /// Pour une mise à jour (version déjà vue mais différente), affiche les
        /// nouveautés de TOUTES les versions franchies depuis, pas seulement
        /// celle qui vient d'être installée (voir Changelog.GetChangesSince).
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
                ShowChangelog(_settings.LastSeenVersion, version);
                _settings.LastSeenVersion = version;
                SettingsManager.Save(_settings);
            }
        }

        private void ShowChangelog(string? lastSeenVersion, string version)
        {
            var announced = Changelog.GetChangesSince(
                lastSeenVersion, version, Localization.Current == AppLanguage.English);

            using var dialog = new ChangelogForm(announced, version, _currentTheme);
            dialog.ShowDialog(this);
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
            // Hauteur 26 (et non 20/22) : en dessous de 24 le bas des lettres est
            // rogné, jambages compris ("p" de Stop/Open, "y" éventuel d'un futur
            // libellé) — l'icône de 14 px posée en ImageBeforeText ne laissait pas
            // assez de place à la police. 24 est le premier palier propre, 26 est
            // retenu pour la marge et par cohérence avec les autres boutons de
            // l'app (Actualiser / Ouvrir dossier sont déjà en 26).
            // Largeur 105 (et non 95) : une fois le thème appliqué aux lignes, le
            // Padding(8,0,8,0) des boutons thématisés ne laissait plus la place à
            // "Remove", tronqué en "Remov" (l'anglais est le cas le plus long).
            const int buttonWidth = 105;
            const int buttonHeight = 26;
            const int buttonX = 495; // 495 + 105 = 600, même bord droit qu'avant.
            const int secondRowY = 30;

            var container = new Panel { Size = new Size(605, 56), Margin = new Padding(2) };
            // Largeur bornée (et non AutoSize) depuis l'ajout du minuteur (87.0) :
            // le libellé du temps restant occupe la droite de cette rangée, un nom
            // de salon inhabituellement long viendrait sinon se superposer à lui.
            // AutoEllipsis coupe proprement avec "..." plutôt que de déborder.
            var nameLabel = new Label
            {
                Text = job.RoomName,
                Location = new Point(2, 5),
                Size = new Size(335, 18),
                AutoSize = false,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold)
            };
            // Minuteur (87.0) : entre le nom et le bouton Ouvrir, sur la rangée du
            // haut restée libre. Se termine à x=490, juste avant buttonX (495).
            // Masqué tant qu'aucun minuteur n'est actif, pour ne rien changer à
            // l'apparence des enregistrements illimités.
            var timerLabel = new Label
            {
                Location = new Point(345, 5),
                Size = new Size(145, 18),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false
            };
            var openButton = new ThemedButton { Location = new Point(buttonX, 0), Size = new Size(buttonWidth, buttonHeight) };
            var progressBar = new ThemedProgressBar { Location = new Point(2, secondRowY + 4), Size = new Size(350, 18), Minimum = 0, Maximum = 100, BarColor = RunningColor };
            var statusLabel = new Label { Location = new Point(358, secondRowY + 4), Size = new Size(130, 18), AutoSize = false };
            var stopButton = new ThemedButton { Location = new Point(buttonX, secondRowY), Size = new Size(buttonWidth, buttonHeight) };

            // Minuteur et état sont des informations d'accompagnement : le nom
            // du salon reste le seul texte plein de la ligne.
            ThemeManager.SetTextRole(timerLabel, TextRole.Caption);
            ThemeManager.SetTextRole(statusLabel, TextRole.Caption);

            openButton.IconName = "open";
            openButton.IconSize = 14;
            // Danger, et non Primary : ce bouton interrompt une capture en
            // cours. Il reste discret (texte et bordure rouges sur fond neutre)
            // parce qu'il est visible en permanence, une ligne par
            // enregistrement — autant d'aplats rouges crieraient à l'écran.
            stopButton.Role = ButtonRole.Danger;
            stopButton.IconName = "stop";
            stopButton.IconSize = 14;

            openButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(job.SourceUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Localization.Format("error.cannotOpenPage", ex.Message),
                        Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            container.Controls.AddRange(new Control[] { nameLabel, timerLabel, openButton, progressBar, statusLabel, stopButton });

            // Parentée ICI, avant l'application du thème (39.0) : depuis que
            // les cartes ont leur propre couleur de surface, ThemeManager
            // remonte la chaîne des parents pour savoir sur quoi il peint. Une
            // ligne encore orpheline recevait donc le fond de la FENÊTRE, et
            // apparaissait comme un rectangle plus sombre au milieu de la carte.
            jobsListPanel.Controls.Add(container);

            var row = new JobRow
            {
                Job = job,
                Container = container,
                NameLabel = nameLabel,
                ProgressBar = progressBar,
                StatusLabel = statusLabel,
                StopButton = stopButton,
                OpenButton = openButton,
                TimerLabel = timerLabel,
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
                    row.Status = JobRowStatus.Cancelled;
                    RefreshJobRowLabels(row);
                }
                else
                {
                    RemoveJobRow(row);
                }
            };

            job.Engine.OnLogLine      += line => SafeInvoke(() => AppendJobLog(job, line));
            job.Engine.OnProgress     += pct  => SafeInvoke(() => UpdateJobProgress(row, pct));
            job.Engine.OnStateChanged += state => SafeInvoke(() => HandleJobStateChanged(row, state));

            RefreshJobRowLabels(row);

            // ThemeManager.Apply n'est appelé qu'une fois dans le constructeur,
            // donc avant qu'aucune ligne n'existe : sans cet appel les contrôles
            // créés ici gardent leur rendu système (boutons gris clair à bordure,
            // texte noir) au lieu du bleu d'accent, et une ligne créée en thème
            // sombre reste claire jusqu'au prochain changement de thème (seul
            // AnimateThemeTransition repasse récursivement sur tout le
            // formulaire). N'écrase aucune couleur d'état : le cas
            // ThemedProgressBar de ThemeManager ne touche que la piste et la
            // bordure, jamais BarColor (posée juste au-dessus à RunningColor,
            // puis pilotée par HandleJobStateChanged/PulseProgressBar), et les
            // icônes sont déjà rendues en IconColor, fixe dans les deux thèmes.
            ThemeManager.Apply(container, _currentTheme);
            return row;
        }

        /// <summary>
        /// Retire une ligne de la liste et libère ses contrôles. Le Dispose
        /// compte depuis que la barre de progression est dessinée par
        /// l'application : en mode indéterminé, ThemedProgressBar fait tourner
        /// un Timer, que seul son Dispose arrête. Un simple Controls.Remove
        /// laisserait la ligne (et le RecordingJob accroché à ses gestionnaires)
        /// vivante indéfiniment, sur une application faite pour tourner longtemps.
        ///
        /// Le Dispose est différé : l'un des appelants est le gestionnaire Click
        /// du bouton "Retirer", qui appartient justement à la ligne détruite —
        /// il doit avoir fini de s'exécuter avant que son bouton disparaisse.
        /// </summary>
        private void RemoveJobRow(JobRow row)
        {
            // Même raison pour le minuteur d'arrêt (87.0) : c'est aussi un Timer,
            // que rien n'arrêterait si la ligne disparaissait sans passer ici.
            StopRecordingTimer(row);

            // 95.0 — même raison, pour les deux choses que le retrait laissait
            // vivantes : le minuteur de reconnexion en attente, et le moteur
            // lui-même. Sans ça, retirer une ligne pendant une tentative de
            // reconnexion laissait yt-dlp tourner et relancer le cycle
            // Running/Failed dans le vide. AutoReconnectEnabled est coupé avant
            // Stop() pour que l'état final ne reprogramme rien.
            if (row.PendingReconnectTimer != null)
            {
                row.PendingReconnectTimer.Stop();
                row.PendingReconnectTimer.Dispose();
                row.PendingReconnectTimer = null;
            }
            row.Job.AutoReconnectEnabled = false;
            row.Job.Engine.Stop();

            jobsListPanel.Controls.Remove(row.Container);
            _jobRows.Remove(row);
            BeginInvoke(() => row.Container.Dispose());
        }

        /// <summary>
        /// Seul endroit qui traduit l'état logique d'une ligne de job
        /// (JobRowStatus) en texte affiché — appelé à chaque changement d'état
        /// ET depuis ApplyLanguage pour retraduire les lignes déjà affichées
        /// sans perdre leur état courant (ex : ne pas remplacer un pourcentage
        /// en cours par "En cours...").
        /// </summary>
        private void RefreshJobRowLabels(JobRow row)
        {
            string L(string key) => Localization.Get(key, _currentLanguage);

            row.OpenButton.Text = L("job.open");

            switch (row.Status)
            {
                case JobRowStatus.Preparing:
                case JobRowStatus.Running:
                    row.StopButton.Text = L("job.stop");
                    row.StatusLabel.Text = row.HasProgressPct
                        ? FormatProgressPct(row.LastProgressPct)
                        : L(row.Status == JobRowStatus.Running ? "job.running" : "job.preparing");
                    break;

                case JobRowStatus.ReconnectPending:
                    row.StopButton.Text = L("job.cancel");
                    row.StatusLabel.Text = string.Format(L("job.reconnectIn"), row.ReconnectDelaySeconds);
                    break;

                case JobRowStatus.Cancelled:
                    row.StopButton.Text = L("job.remove");
                    row.StatusLabel.Text = L("job.cancelled");
                    break;

                case JobRowStatus.Finished:
                    row.StopButton.Text = L("job.remove");
                    row.StatusLabel.Text = row.FinishedState switch
                    {
                        DownloadState.Completed => L("job.state.completed"),
                        DownloadState.Failed => L("job.state.failed"),
                        _ => L("job.state.stopped"),
                    };
                    break;
            }
        }

        /// <summary>
        /// Démarre le minuteur d'une ligne (87.0), si l'utilisateur en a demandé
        /// un. Appelé au passage en Running.
        ///
        /// L'échéance n'est calculée qu'au PREMIER démarrage : une reconnexion
        /// automatique repasse par Running, mais ne doit pas repousser l'arrêt,
        /// sinon une room instable enregistrerait indéfiniment.
        /// </summary>
        private void StartRecordingTimer(JobRow row)
        {
            if (row.Job.TimerMinutes <= 0) return;

            row.Job.StopAtUtc ??= DateTime.UtcNow.AddMinutes(row.Job.TimerMinutes);
            row.TimerLabel.Visible = true;
            UpdateCountdown(row);

            if (row.CountdownTimer != null) return;

            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            row.CountdownTimer = timer;
            timer.Tick += (s, e) => UpdateCountdown(row);
            timer.Start();
        }

        /// <summary>
        /// Rafraîchit le temps restant et déclenche l'arrêt à l'échéance.
        /// </summary>
        private void UpdateCountdown(JobRow row)
        {
            if (row.Job.StopAtUtc is not { } echeance) return;

            var restant = echeance - DateTime.UtcNow;
            if (restant > TimeSpan.Zero)
            {
                row.TimerLabel.Text = "⏱ " + RecordingTimer.FormatRemaining(restant);
                return;
            }

            // Échéance atteinte : on coupe le timer AVANT d'arrêter le moteur,
            // pour ne pas re-déclencher l'arrêt à chaque tick suivant.
            StopRecordingTimer(row);
            row.TimerLabel.Visible = false;

            AppendJobLog(row.Job, $"Durée maximale atteinte ({row.Job.TimerMinutes} min) : arrêt de l'enregistrement.");

            // Engine.Stop() marque l'arrêt comme manuel, donc l'état final sera
            // Stopped — ce qui exclut la reconnexion automatique dans
            // HandleJobStateChanged. Un minuteur qui relancerait aussitôt
            // l'enregistrement n'aurait aucun sens.
            if (row.Job.Engine.State == DownloadState.Running)
                row.Job.Engine.Stop();
        }

        /// <summary>
        /// Arrête et libère le minuteur d'une ligne. Sans effet s'il n'y en a
        /// pas — appelable depuis tous les chemins de sortie sans condition.
        /// </summary>
        private static void StopRecordingTimer(JobRow row)
        {
            if (row.CountdownTimer == null) return;
            row.CountdownTimer.Stop();
            row.CountdownTimer.Dispose();
            row.CountdownTimer = null;
        }

        /// <summary>
        /// Pourcentage affiché à côté de la barre. Entier depuis 39.0 : la
        /// décimale changeait plusieurs fois par seconde sans rien apprendre à
        /// personne, ce qui faisait « bouger » en permanence une zone de texte
        /// que l'œil ne peut pas suivre. L'espace avant le signe est une règle
        /// typographique française, que l'anglais ne partage pas.
        /// </summary>
        private static string FormatProgressPct(double pct)
        {
            var rounded = Math.Round(pct).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            return Localization.Current == AppLanguage.English ? $"{rounded}%" : $"{rounded} %";
        }

        private void UpdateJobProgress(JobRow row, double pct)
        {
            var clamped = Math.Min(100, Math.Max(0, (int)Math.Round(pct)));
            row.ProgressBar.Value = clamped;
            row.HasProgressPct = true;
            row.LastProgressPct = pct;
            row.StatusLabel.Text = FormatProgressPct(pct);
            // 95.0 : c'est ICI que le compteur de reconnexions se remet à zéro.
            // Recevoir une progression est la seule preuve que le flux existe
            // vraiment ; l'état Running ne prouve que le démarrage du processus.
            row.Job.ReconnectAttempt = 0;
        }

        private void HandleJobStateChanged(JobRow row, DownloadState state)
        {
            // 95.0 : une ligne retirée n'a plus rien à afficher ni à notifier.
            // Le moteur peut encore lever un changement d'état après le retrait
            // (processus yt-dlp en cours de fin), ce qui produisait une
            // notification d'erreur pour un enregistrement disparu de l'écran.
            if (!_jobRows.Contains(row)) return;

            switch (state)
            {
                case DownloadState.Running:
                    // 95.0 — le compteur de tentatives n'est PLUS remis à zéro ici.
                    // DownloadEngine lève Running dès que le PROCESSUS yt-dlp a
                    // démarré, pas quand le flux coule : une room hors ligne
                    // enchaîne donc Running -> Failed en boucle, et remettre le
                    // compteur à zéro à chaque passage rendait le plafond
                    // AutoReconnectMaxAttempts inatteignable — d'où une
                    // notification d'erreur toutes les 30 s, indéfiniment.
                    // La remise à zéro vit désormais dans UpdateJobProgress :
                    // seule une progression réelle prouve que le flux existe.
                    row.Status = JobRowStatus.Running;
                    row.HasProgressPct = false;
                    RefreshJobRowLabels(row);
                    row.StopButton.Enabled = true;
                    row.ProgressBar.Style = ProgressBarStyle.Marquee;
                    row.ProgressBar.MarqueeAnimationSpeed = 30;
                    PulseProgressBar(row.ProgressBar, RunningColor);
                    StartRecordingTimer(row);
                    break;

                case DownloadState.Completed:
                case DownloadState.Failed:
                case DownloadState.Stopped:
                    row.Status = JobRowStatus.Finished;
                    row.FinishedState = state;
                    RefreshJobRowLabels(row);
                    // Le minuteur n'a plus lieu d'etre, quelle que soit la raison de
                    // la fin. Si l'enregistrement reprend (reconnexion automatique),
                    // StartRecordingTimer le relancera sur l'echeance initiale.
                    StopRecordingTimer(row);
                    row.TimerLabel.Visible = false;
                    row.ProgressBar.Style = ProgressBarStyle.Blocks;
                    AnimateProgressBarFill(row.ProgressBar, state == DownloadState.Completed ? 100 : 0);
                    row.ProgressBar.BarColor = state switch
                    {
                        DownloadState.Completed => CompletedColor,
                        DownloadState.Failed => FailedColor,
                        _ => StoppedColor,
                    };
                    AppendJobLog(row.Job, $"Job terminé (état : {state}).");
                    RefreshHistoryAsync();

                    if (state == DownloadState.Stopped)
                    {
                        AppendJobLog(row.Job, "Téléchargement interrompu.");
                    }
                    else
                    {
                        // Sans ffmpeg, pas de miniature — mais la capture, elle,
                        // a bien eu lieu : il n'y a aucune raison de la perdre.
                        if (SafeMode.IsEnabled(SafeComponent.Ffmpeg)) GenerateThumbnail(row.Job);
                        if (state == DownloadState.Completed)
                            ShowNotification(Localization.Get("notify.recordingDone.title"), row.Job.RoomName);
                        else
                            ShowNotification(Localization.Get("notify.recordingError.title"),
                                Localization.Format("notify.recordingError.body", row.Job.RoomName), ToolTipIcon.Error);

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
                    if (row.Job.CodecChoice != "copy" && state != DownloadState.Failed
                        && SafeMode.IsEnabled(SafeComponent.Ffmpeg))
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
            row.Status = JobRowStatus.ReconnectPending;
            row.ReconnectDelaySeconds = delaySeconds;
            RefreshJobRowLabels(row);

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
        private void PulseProgressBar(ThemedProgressBar bar, Color settleColor)
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

                bar.BarColor = ticks % 2 == 0 ? brighter : settleColor;
                ticks++;
                if (ticks >= 6)
                {
                    timer.Stop();
                    timer.Dispose();
                    bar.BarColor = settleColor;
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Remplissage animé (8.3) : anime la Value de la barre vers sa cible
        /// au lieu d'un saut instantané, visible à la fin d'un job (la barre
        /// reste en Marquee pendant l'enregistrement, donc c'est ici que le
        /// passage en mode "Blocks" devient visible pour la première fois).
        /// </summary>
        private void AnimateProgressBarFill(ThemedProgressBar bar, int target)
        {
            var timer = new System.Windows.Forms.Timer { Interval = 12 };
            timer.Tick += (s, e) =>
            {
                if (bar.IsDisposed) { timer.Stop(); timer.Dispose(); return; }

                var current = bar.Value;
                if (current == target)
                {
                    timer.Stop();
                    timer.Dispose();
                    return;
                }

                var step = Math.Max(1, Math.Abs(target - current) / 6);
                bar.Value = current < target
                    ? Math.Min(target, current + step)
                    : Math.Max(target, current - step);
            };
            timer.Start();
        }

        /// <summary>
        /// Fichier de capture de CE job précisément — chemin déterministe basé
        /// sur OutputBaseName (horodatage figé au (re)démarrage), plus fiable
        /// qu'une recherche par RoomName + "fichier le plus récent" : un salon
        /// enregistré plusieurs fois (jours différents, reconnexions) produit
        /// plusieurs fichiers correspondant au même motif, et le réencodage
        /// (ReencodeCaptureAsync) écrit un fichier supplémentaire qui matchait
        /// aussi ce motif — l'ancienne heuristique par date de modification
        /// pouvait donc attribuer la miniature/le réencodage d'un job au
        /// fichier d'un AUTRE enregistrement du même salon.
        /// </summary>
        private FileInfo? FindOwnCaptureFile(RecordingJob job)
        {
            if (job.OutputBaseName == null) return null;

            var expectedPath = Path.Combine(job.CaptureDir, $"{job.OutputBaseName}.{job.ContainerExt}");
            return File.Exists(expectedPath) ? new FileInfo(expectedPath) : null;
        }

        private static readonly string[] VideoExtensions = { ".mp4", ".mkv", ".mov" };

        /// <summary>
        /// Historique des enregistrements (4.4) : scan du dossier de capture en
        /// arrière-plan (Task.Run — énumération de fichiers + éventuels appels
        /// ffprobe ne doivent jamais geler l'UI), résultat appliqué au ListView
        /// via SafeInvoke. Limité aux 50 fichiers les plus récents pour éviter
        /// un scan trop lourd sur un dossier avec un historique important.
        /// </summary>
        /// <summary>
        /// Charge la miniature d'une vidéo si elle existe. Elles sont générées
        /// par ffmpeg à la fin de chaque enregistrement depuis la v1.3.0, posées
        /// en .jpg à côté du fichier — et n'avaient jamais été affichées nulle
        /// part. Ce travail était donc payé à chaque capture pour rien.
        /// </summary>
        private static Bitmap? LoadThumbnail(string videoPath)
        {
            try
            {
                var jpg = Path.ChangeExtension(videoPath, ".jpg");
                if (!File.Exists(jpg)) return null;

                // Passer par un FileStream, et NON Image.FromFile : celui-ci
                // garde le fichier ouvert tant que l'image vit, ce qui
                // empêcherait de supprimer ou déplacer la vidéo depuis
                // l'explorateur tant que l'application tourne.
                using var stream = File.OpenRead(jpg);
                using var source = Image.FromStream(stream);

                var bitmap = new Bitmap(48, 27);
                using var g = Graphics.FromImage(bitmap);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, 48, 27);
                return bitmap;
            }
            catch (Exception ex)
            {
                // Miniature illisible ou tronquée : la ligne s'affiche sans
                // image, ce n'est pas une raison de perdre tout l'historique.
                Logger.Log($"Miniature illisible pour '{videoPath}' : {ex.Message}", LogLevel.WARN);
                return null;
            }
        }

        /// <summary>
        /// Ouvre la vidéo sélectionnée avec le lecteur par défaut (4.1).
        /// </summary>
        private void OnOpenHistoryFileClick(object? sender, EventArgs e)
        {
            if (historyListView.SelectedItems.Count == 0) return;
            if (historyListView.SelectedItems[0].Tag is not string path) return;

            if (!File.Exists(path))
            {
                // Le fichier a pu être supprimé depuis le dernier rafraîchissement.
                MessageBox.Show(this, Localization.Format("error.fileGone", path),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshHistoryAsync();
                return;
            }

            OpenExternal(path);
        }

        private void RefreshHistoryAsync()
        {
            var captureDir = AppConfig.CaptureDir;
            var ffprobePath = AppConfig.FFprobePath;
            var hasFFprobe = File.Exists(ffprobePath);

            Task.Run(() =>
            {
                var entries = new List<(string Name, long Size, string Duration, DateTime Date, string FullPath, Bitmap? Thumb)>();
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
                                f.FullName,
                                // Décodée ici, sur le thread de fond : ouvrir et
                                // redimensionner 50 JPEG sur le thread UI figerait
                                // la fenêtre à chaque rafraîchissement.
                                LoadThumbnail(f.FullName)
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
                    historyThumbnails.Images.Clear();

                    foreach (var entry in entries)
                    {
                        var item = new ListViewItem(entry.Name);
                        item.SubItems.Add(FormatSize(entry.Size));
                        item.SubItems.Add(entry.Duration);
                        item.SubItems.Add(entry.Date.ToString("dd/MM/yy HH:mm"));
                        item.Tag = entry.FullPath;

                        if (entry.Thumb != null)
                        {
                            // ImageList recopie l'image dans son propre handle :
                            // on libère la nôtre aussitôt, sinon 50 bitmaps
                            // fuiraient à chaque rafraîchissement.
                            historyThumbnails.Images.Add(entry.Thumb);
                            item.ImageIndex = historyThumbnails.Images.Count - 1;
                            entry.Thumb.Dispose();
                        }

                        historyListView.Items.Add(item);
                    }

                    ThemedListView.Refresh(historyListView);
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
                MessageBox.Show(Localization.Format("error.cannotOpenFolder", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            var videoFile = FindOwnCaptureFile(job);
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
            var videoFile = FindOwnCaptureFile(job);
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

        /// <summary>
        /// Vérifie le hash d'un binaire externe contre la valeur figée dans
        /// AppConfig (celle testée par le mainteneur à la release) OU un hash
        /// déjà approuvé localement (TrustedBinaryStore). Si aucun des deux ne
        /// correspond, propose une confiance à la première utilisation plutôt
        /// que de bloquer silencieusement : yt-dlp/ffmpeg sont mis à jour très
        /// souvent, un hash figé dans le binaire de l'appli devient obsolète en
        /// quelques jours pour quiconque télécharge "la dernière version" comme
        /// recommandé par le README/wiki (voir issue #16).
        /// </summary>
        private bool VerifyOrTrustBinary(string binaryKey, string displayName, string path,
            string expectedSha256, bool requireAuthenticode, string expectedSignerThumbprint, string expectedSignerSubject)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(this, Localization.Format("error.binaryNotFound", displayName, path),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var actualHash = BinaryVerifier.ComputeSha256(path);
            if (actualHash == null)
            {
                MessageBox.Show(this, Localization.Format("error.cannotComputeHash", displayName),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var trustedHash = TrustedBinaryStore.GetTrustedHash(binaryKey);
            var hashKnown =
                string.Equals(actualHash, expectedSha256?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actualHash, trustedHash, StringComparison.OrdinalIgnoreCase);

            if (!hashKnown)
            {
                var message = Localization.Format("verify.hashMismatch", displayName, actualHash);

                var result = MessageBox.Show(this, message, Localization.Format("verify.title", displayName),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) return false;

                TrustedBinaryStore.Trust(binaryKey, actualHash);
                Logger.Log($"{displayName} approuvé manuellement par l'utilisateur (hash {actualHash}).", LogLevel.WARN);
            }

            if (requireAuthenticode &&
                !BinaryVerifier.VerifyTrustedBinary(path, actualHash, true, expectedSignerThumbprint, expectedSignerSubject))
            {
                MessageBox.Show(this, Localization.Format("error.invalidAuthenticode", displayName),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        // ==================================================================
        // Surveillance automatique (88.0 / 4.3)
        // ==================================================================

        /// <summary>
        /// Ajoute une ligne a la liste surveillee. Le nom du salon est extrait
        /// de l'URL pour l'affichage ; l'URL complete reste dans le Tag, c'est
        /// elle qui sert a interroger et a enregistrer.
        /// </summary>
        private void AddWatchRow(string url)
        {
            var item = new ListViewItem(RoomNameFromUrl(url)) { Tag = url, Name = "watch.state.pending" };
            item.SubItems.Add(Localization.Get("watch.state.pending"));
            watchListView.Items.Add(item);
            // L'ascenseur vertical apparaît sans redimensionner le contrôle :
            // la dernière colonne doit se réajuster à la largeur utile.
            ThemedListView.Refresh(watchListView);
        }

        private static string RoomNameFromUrl(string url)
        {
            try
            {
                var name = new Uri(url).AbsolutePath.Trim('/')
                    .Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return string.IsNullOrWhiteSpace(name) ? url : name;
            }
            catch { return url; }
        }

        private void OnAddWatchClick(object? sender, EventArgs e)
        {
            var url = urlTextBox.Text.Trim();

            // Meme controle que pour un enregistrement : une URL refusee par le
            // sandbox ne doit pas entrer dans une liste qui la rappellera toutes
            // les deux minutes.
            if (!UrlValidator.IsSafeUrl(url, AppConfig.Whitelist, AppConfig.Blacklist, out var watchUrlReason))
            {
                Logger.Log($"URL refusée pour la surveillance ({url}) : {watchUrlReason}", LogLevel.ERROR);
                MessageBox.Show(this, Localization.Get("error.urlRejected"),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!_watchList.Add(url))
            {
                MessageBox.Show(this, Localization.Format("watch.alreadyWatched", RoomNameFromUrl(url)),
                    Localization.Get("dialog.info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _watchList.Save();
            AddWatchRow(url);
            AppendLog($"[{DateTime.Now:HH:mm:ss}] Surveillance activee pour {RoomNameFromUrl(url)}.");
        }

        private void OnRemoveWatchClick(object? sender, EventArgs e)
        {
            if (watchListView.SelectedItems.Count == 0) return;

            var item = watchListView.SelectedItems[0];
            var url = item.Tag as string ?? "";
            _watchList.Remove(url);
            _watchList.Save();
            watchListView.Items.Remove(item);
            AppendLog($"[{DateTime.Now:HH:mm:ss}] Surveillance desactivee pour {RoomNameFromUrl(url)}.");
        }

        /// <summary>
        /// Retraduit la colonne d'etat sans relancer de controle. La cle de
        /// l'etat courant vit dans ListViewItem.Name, pas dans le texte affiche
        /// : sinon changer de langue en cours de session perdrait l'etat.
        /// </summary>
        private void RefreshWatchStates()
        {
            foreach (ListViewItem item in watchListView.Items)
            {
                var key = string.IsNullOrEmpty(item.Name) ? "watch.state.pending" : item.Name;
                item.SubItems[1].Text = Localization.Get(key);
            }
        }

        private bool IsRecording(string url)
        {
            var room = RoomNameFromUrl(url);
            return _jobRows.Any(r => r.Job.RoomName == room && r.Job.Engine.State == DownloadState.Running);
        }

        private void SetWatchState(ListViewItem item, string key)
        {
            item.Name = key;
            item.SubItems[1].Text = Localization.Get(key);
        }

        private void StartWatchLoop()
        {
            _watchTimer = new System.Windows.Forms.Timer
            {
                Interval = Math.Max(60, _settings.WatchIntervalSeconds) * 1000,
            };
            _watchTimer.Tick += async (s, e) => await RunWatchPassAsync();
            _watchTimer.Start();

            // Premier passage rapproche : sans lui, un salon deja en ligne au
            // demarrage attendrait l'intervalle complet avant d'etre vu.
            var first = new System.Windows.Forms.Timer { Interval = 10_000 };
            first.Tick += async (s, e) =>
            {
                first.Stop();
                first.Dispose();
                await RunWatchPassAsync();
            };
            first.Start();
        }

        /// <summary>
        /// Un passage sur toute la liste. Les salons sont controles l'un APRES
        /// l'autre, jamais en parallele : dix processus yt-dlp simultanes vers
        /// le meme site est exactement ce qui se fait remarquer, et rien
        /// n'exige que le passage soit rapide.
        /// </summary>
        private async Task RunWatchPassAsync()
        {
            if (!SafeMode.IsEnabled(SafeComponent.Watch)) return;
            if (_watchTickRunning || watchListView.Items.Count == 0) return;
            _watchTickRunning = true;
            try
            {
                foreach (var item in watchListView.Items.Cast<ListViewItem>().ToList())
                {
                    if (IsDisposed || _watchTimer == null) return;
                    if (item.Tag is not string url) continue;

                    // Deja en cours d'enregistrement : rien a controler, et
                    // surtout rien a redemarrer.
                    if (IsRecording(url))
                    {
                        SetWatchState(item, "watch.state.recording");
                        continue;
                    }

                    var status = await RoomStatusChecker.CheckAsync(
                        AppConfig.YtDlpPath, url,
                        SafeMode.IsEnabled(SafeComponent.Cookies) ? AppConfig.CookiesFilePath : "",
                        SafeMode.IsEnabled(SafeComponent.Proxy) ? AppConfig.ProxyUrl : "");

                    // L'appel dure plusieurs secondes : la fenetre a pu etre
                    // fermee entre-temps.
                    if (IsDisposed || _watchTimer == null) return;

                    var key = status switch
                    {
                        RoomStatus.Online => "watch.state.online",
                        RoomStatus.Offline => "watch.state.offline",
                        _ => "watch.state.unknown",
                    };
                    SetWatchState(item, key);

                    // SEUL Online declenche. Unknown (reseau coupe, salon banni)
                    // ne doit jamais lancer un enregistrement dans le vide.
                    if (status != RoomStatus.Online) continue;

                    AppendLog($"[{DateTime.Now:HH:mm:ss}] Surveillance : {RoomNameFromUrl(url)} est en ligne, demarrage.");
                    ShowNotification(Localization.Get("watch.started.title"),
                        Localization.Format("watch.started.body", RoomNameFromUrl(url)));
                    StartRecording(url, interactive: false);
                    SetWatchState(item, "watch.state.recording");
                }
            }
            finally
            {
                _watchTickRunning = false;
            }
        }

        /// <summary>
        /// Refus de démarrage : dialogue quand l'utilisateur attend une réponse,
        /// ligne de log quand c'est la surveillance qui a demandé (88.0).
        /// </summary>
        private void RefuseStart(bool interactive, string message, string title)
        {
            if (interactive)
                MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Surveillance : démarrage refusé — {message}");
        }

        private void OnStartClick(object? sender, EventArgs e)
            => StartRecording(urlTextBox.Text.Trim(), interactive: true);

        /// <summary>
        /// Démarre un enregistrement. `interactive` distingue le clic sur
        /// « Démarrer » de la surveillance automatique (88.0) : en mode non
        /// interactif, un refus s'écrit dans les logs au lieu d'ouvrir une
        /// boîte de dialogue — personne n'est devant l'écran pour la fermer, et
        /// une modale bloquerait la boucle de surveillance.
        ///
        /// **Exception assumée** : la vérification des binaires
        /// (VerifyOrTrustBinary) garde son dialogue dans les deux modes. Elle ne
        /// se déclenche que si le hash de yt-dlp/ffmpeg est inconnu ou a changé
        /// — un cas qui DOIT interrompre l'utilisateur, surveillance ou pas.
        /// </summary>
        private void StartRecording(string urlInput, bool interactive)
        {

            if (!UrlValidator.IsSafeUrl(urlInput, AppConfig.Whitelist, AppConfig.Blacklist, out var urlReason))
            {
                Logger.Log($"URL refusée ({urlInput}) : {urlReason}", LogLevel.ERROR);
                RefuseStart(interactive, Localization.Get("error.urlRejected"), Localization.Get("dialog.error"));
                return;
            }

            if (!VerifyOrTrustBinary("yt-dlp", "yt-dlp.exe", AppConfig.YtDlpPath, AppConfig.YtDlpExpectedSha256,
                    AppConfig.YtDlpRequireAuthenticode, AppConfig.YtDlpExpectedSignerThumbprint, AppConfig.YtDlpExpectedSignerSubject))
            {
                return;
            }
            if (!VerifyOrTrustBinary("ffmpeg", "ffmpeg.exe", AppConfig.FFmpegPath, AppConfig.FfmpegExpectedSha256,
                    AppConfig.FfmpegRequireAuthenticode, AppConfig.FfmpegExpectedSignerThumbprint, AppConfig.FfmpegExpectedSignerSubject))
            {
                return;
            }

            if (AppConfig.EnableCaPinning)
            {
                var ytOk = BinaryVerifier.VerifyCaPinning(AppConfig.YtDlpPath, AppConfig.TrustedCaThumbprint, AppConfig.TrustedCaIssuer);
                var ffOk = ytOk && BinaryVerifier.VerifyCaPinning(AppConfig.FFmpegPath, AppConfig.TrustedCaThumbprint, AppConfig.TrustedCaIssuer);
                if (!ffOk)
                {
                    RefuseStart(interactive, Localization.Get("error.caPinningFailed"), Localization.Get("dialog.error"));
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
            if (!PathValidator.IsValidPath(AppConfig.CaptureDir, mustExist: false, out var outputDirReason))
            {
                Logger.Log($"Dossier de capture refusé au lancement : {outputDirReason}", LogLevel.ERROR);
                RefuseStart(interactive, Localization.Format("error.invalidOutputDir", AppConfig.CaptureDir), Localization.Get("dialog.error"));
                return;
            }

            var uri = new Uri(urlInput);

            if (AppConfig.EnableTlsServerPinning)
            {
                if (!CertificateValidator.VerifyRemoteCertificate(uri.Host, 443, AppConfig.ServerExpectedThumbprint,
                        AppConfig.ServerExpectedIssuer, out var tlsReason))
                {
                    // Motif indispensable ici : « vérification TLS échouée »
                    // recouvre aussi bien une interception qu'une empreinte
                    // devenue obsolète après un renouvellement de certificat.
                    Logger.Log($"Vérification TLS refusée pour {uri.Host} : {tlsReason}", LogLevel.ERROR);
                    RefuseStart(interactive, Localization.Format("error.tlsVerificationFailed", uri.Host), Localization.Get("dialog.error"));
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

            // Safe Mode : un seul enregistrement a la fois quand le
            // multi-stream est desactive. Refus explicite plutot que silencieux.
            if (!SafeMode.IsEnabled(SafeComponent.MultiStream)
                && _jobRows.Any(r => r.Job.Engine.State == DownloadState.Running))
            {
                RefuseStart(interactive, Localization.Get("safe.multiStreamOff"), Localization.Get("dialog.info"));
                return;
            }

            if (_jobRows.Any(r => r.Job.RoomName == roomName && r.Job.Engine.State == DownloadState.Running))
            {
                // En surveillance, un enregistrement déjà en cours est le cas
                // NORMAL à chaque passage : silencieux, pas même une ligne de log.
                if (interactive)
                    MessageBox.Show(Localization.Format("info.alreadyRecording", roomName),
                        Localization.Get("dialog.info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Chemin de log seulement validé une fois ici : seul l'horodatage
            // change entre tentatives, ce qui ne peut pas rendre un chemin déjà
            // valide invalide.
            if (!PathValidator.IsValidPath(Path.Combine(AppConfig.LogDir, $"{roomName}-test.log"),
                    mustExist: false, out var logPathReason))
            {
                Logger.Log($"Chemin de log refusé : {logPathReason}", LogLevel.ERROR);
                RefuseStart(interactive, Localization.Get("error.invalidLogPath"), Localization.Get("dialog.error"));
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
                AutoReconnectEnabled = _settings.AutoReconnectDefault,
                TimerMinutes = RecordingTimer.MinutesForIndex(durationCombo.SelectedIndex),
            };

            var row = BuildJobRow(job);

            // Capturée ici et réutilisée telle quelle pour les reconnexions
            // automatiques (4.2) : régénère un horodatage frais à chaque appel,
            // donc un nouveau fichier de sortie/log à chaque tentative.
            void StartEngine()
            {
                var time = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                job.OutputBaseName = $"{roomName}-{time}";
                var logFilePath = Path.Combine(AppConfig.LogDir, $"{job.OutputBaseName}.log");
                var outputPath  = Path.Combine(job.CaptureDir, $"{job.OutputBaseName}.%(ext)s");

                // Safe Mode : un composant desactive n'est pas transmis a yt-dlp.
                // Passer un cookies.txt invalide faisait echouer TOUTES les
                // captures (constate le 2026-08-08) ; mieux vaut enregistrer
                // sans authentification que ne rien enregistrer du tout.
                job.Engine.Start(AppConfig.YtDlpPath, AppConfig.FFmpegPath, urlInput, outputPath, logFilePath, formatSelector, containerExt,
                    SafeMode.IsEnabled(SafeComponent.Cookies) ? AppConfig.CookiesFilePath : "",
                    SafeMode.IsEnabled(SafeComponent.Proxy) ? AppConfig.ProxyUrl : "",
                    AppConfig.YtDlpWatchdogTimeoutSeconds, AppConfig.LogMaxFileSizeBytes);
            }
            row.RestartEngine = StartEngine;

            // La ligne est déjà posée dans le panneau par BuildJobRow (elle doit
            // connaître son parent avant que le thème lui soit appliqué).
            _jobRows.Add(row);

            AppendJobLog(job, "Démarrage de l'enregistrement...");

            try
            {
                StartEngine();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.Format("error.cannotStartDownload", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                RemoveJobRow(row);
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

        private void OnAddFavoriteClick(object? sender, EventArgs e)
        {
            var url = urlTextBox.Text.Trim();

            // Deux causes, deux messages. « URL invalide ou déjà présente »
            // obligeait l'utilisateur à deviner laquelle des deux : signalé en
            // vrai après un clic sur un favori déjà enregistré, où le message
            // laissait croire à une URL malformée.
            if (_favorites.Favorites.Any(f => string.Equals(f, url, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, Localization.Format("info.favoriteAlreadyPresent", url),
                    Localization.Get("dialog.info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_favorites.AddFavorite(url))
            {
                MessageBox.Show(this, Localization.Get("info.favoriteInvalidUrl"),
                    Localization.Get("dialog.info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            favoritesListBox.Items.Add(url);
            ShowNotification(Localization.Get("notify.favoriteAdded.title"), url);
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
                MessageBox.Show(Localization.Format("error.cannotOpenDonateLink", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSponsorClick(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(AppConfig.SponsorUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.Format("error.cannotOpenSponsor", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Ouvre une URL dans le navigateur par défaut (50.0). Même repli que
        /// OnWebsiteClick : un navigateur absent ou une association de
        /// protocole cassée ne doit pas remonter en exception non gérée.
        /// </summary>
        private void OpenExternal(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.Format("error.cannotOpenWebsite", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(Localization.Format("error.cannotOpenWebsite", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Rappelé par SettingsForm (19.0) quand le thème est changé depuis la
        /// fenêtre Paramètres — la logique d'animation/transition reste ici
        /// puisqu'elle porte sur MainForm, pas sur la fenêtre de paramètres.
        /// </summary>
        private void HandleThemeChangedFromSettings(AppTheme theme)
        {
            AnimateThemeTransition(_currentTheme, theme);
            _currentTheme = theme;
        }

        /// <summary>Rappelé par SettingsForm (19.0) quand la langue est changée.</summary>
        private void HandleLanguageChangedFromSettings(AppLanguage language)
        {
            _currentLanguage = language;
            Localization.Current = language;
            ApplyLanguage(language);

            _settings.Language = language == AppLanguage.English ? "en" : "fr";
            SettingsManager.Save(_settings);
        }

        /// <summary>
        /// Traduction de l'UI principale (20.0) : réassigne le Text de chaque
        /// libellé/bouton/case à cocher fixe, les items des ComboBox (en
        /// conservant l'index sélectionné, jamais comparés par texte — voir
        /// HandleThemeChangedFromSettings) et les en-têtes de colonnes. Les
        /// messages d'erreur, notifications, logs, le guide de démarrage et
        /// l'historique des nouveautés restent en français (voir
        /// UI/Localization.cs). Thème/langue/dossier/cookies/proxy/reconnexion
        /// automatique vivent désormais dans SettingsForm (19.0), traduits par
        /// sa propre méthode ApplyLanguage quand cette fenêtre est ouverte.
        /// </summary>
        private void ApplyLanguage(AppLanguage lang)
        {
            string L(string key) => Localization.Get(key, lang);

            paramsButton.Text = L("button.settings");
            checkUpdateButton.Text = L("button.checkUpdate");
            tutorialButton.Text = L("button.tutorial");
            reportBugButton.Text = L("button.reportBug");
            diagnosticButton.Text = L("button.diagnostic");
            legalButton.Text = L("button.legal");
            modeToggleButton.Text = _advancedMode ? L("mode.switchToSimple") : L("mode.switchToAdvanced");

            _trayOpenItem.Text = L("tray.open");
            _traySettingsItem.Text = L("tray.settings");
            _trayCloseItem.Text = L("tray.close");

            grpRecord.Title = L("panel.record");
            urlLabel.Text = L("label.url");
            startButton.Text = L("button.start");
            stopAllButton.Text = L("button.stopAll");
            addFavoriteButton.Text = L("button.addFavorite");

            qualityLabel.Text = L("label.quality");
            var qualityIndex = qualityCombo.SelectedIndex;
            qualityCombo.Items.Clear();
            qualityCombo.Items.AddRange(new object[] { L("quality.best"), L("quality.medium"), L("quality.worst") });
            qualityCombo.SelectedIndex = qualityIndex < 0 ? 0 : qualityIndex;

            codecLabel.Text = L("label.codec");
            var codecIndex = codecCombo.SelectedIndex;
            codecCombo.Items.Clear();
            codecCombo.Items.AddRange(new object[] { L("codec.copy"), L("codec.h264"), L("codec.h265") });
            codecCombo.SelectedIndex = codecIndex < 0 ? 0 : codecIndex;

            formatLabel.Text = L("label.format");
            var formatIndex = formatCombo.SelectedIndex;
            formatCombo.Items.Clear();
            formatCombo.Items.AddRange(new object[] { L("format.mp4"), L("format.mkv"), L("format.mov") });
            formatCombo.SelectedIndex = formatIndex < 0 ? 0 : formatIndex;

            // L'ordre des items doit rester celui de RecordingTimer.PresetMinutes,
            // la sélection étant convertie en minutes par son index.
            durationLabel.Text = L("label.duration");
            var durationIndex = durationCombo.SelectedIndex;
            durationCombo.Items.Clear();
            durationCombo.Items.AddRange(new object[]
            {
                L("duration.unlimited"), L("duration.15min"), L("duration.30min"),
                L("duration.1h"), L("duration.2h"), L("duration.4h"), L("duration.8h")
            });
            durationCombo.SelectedIndex = durationIndex < 0 ? 0 : durationIndex;

            grpProgress.Title = L("panel.progress");

            grpHistory.Title = L("panel.history");
            historyListView.Columns[0].Text = L("column.file");
            historyListView.Columns[1].Text = L("column.size");
            historyListView.Columns[2].Text = L("column.duration");
            historyListView.Columns[3].Text = L("column.date");
            refreshHistoryButton.Text = L("button.refresh");
            openHistoryFolderButton.Text = L("button.openFolder");
            openHistoryFileButton.Text = L("button.openFile");

            grpFavorites.Title = L("panel.favorites");
            loadFavoriteButton.Text = L("button.load");
            removeFavoriteButton.Text = L("button.removeFavorite");

            grpWatch.Title = L("panel.watch");
            addWatchButton.Text = L("button.watchAdd");
            removeWatchButton.Text = L("button.watchRemove");
            watchListView.Columns[0].Text = L("column.room");
            watchListView.Columns[1].Text = L("column.watchState");
            RefreshWatchStates();
            grpDonate.Title = L("panel.donate");
            sponsorButton.Text = L("button.sponsor");
            donateButton.Text = L("button.donate");
            websiteButton.Text = L("button.website");
            donateLabel.Text = L("label.donate");

            grpLogs.Title = L("panel.logs");

            foreach (var row in _jobRows)
                RefreshJobRowLabels(row);
        }

        /// <summary>
        /// Transition douce clair/sombre (9.2) : interpole les deux palettes sur
        /// une courte durée au lieu du saut instantané de couleurs d'origine.
        /// ApplyIcons() n'a rien à refaire pendant l'animation (IconColor est
        /// fixe dans les deux thèmes, voir son commentaire), un seul appel à la
        /// fin suffit, par cohérence avec le comportement précédent.
        /// </summary>
        private void AnimateThemeTransition(AppTheme from, AppTheme to)
        {
            var start = ThemeManager.GetPalette(from);
            var end = ThemeManager.GetPalette(to);
            const int durationMs = 220;

            var sw = Stopwatch.StartNew();
            var timer = new System.Windows.Forms.Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                if (IsDisposed) { timer.Stop(); timer.Dispose(); return; }

                var t = Math.Min(1f, (float)sw.ElapsedMilliseconds / durationMs);
                ThemeManager.ApplyPalette(this, ThemeManager.LerpPalette(start, end, t));

                if (t >= 1f)
                {
                    timer.Stop();
                    timer.Dispose();
                    ThemeManager.Apply(this, to);
                    ApplyIcons();
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Anime l'Opacity de la fenêtre entière vers <paramref name="target"/>
        /// (9.2) : WinForms n'exposant pas d'opacité par contrôle, un fondu par
        /// panneau demanderait un contournement bien plus lourd (fenêtres
        /// calquées) pour un gain limité — un fondu de toute la fenêtre reste le
        /// bon compromis ici. Réutilisé pour le fondu d'ouverture (Opacity 0→1)
        /// et pour le clignotement léger au changement de mode simple/avancé.
        /// </summary>
        private void AnimateOpacity(double target, int durationMs, Action? onComplete = null)
        {
            var start = Opacity;
            var sw = Stopwatch.StartNew();
            var timer = new System.Windows.Forms.Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                if (IsDisposed) { timer.Stop(); timer.Dispose(); return; }

                var t = Math.Min(1.0, (double)sw.ElapsedMilliseconds / durationMs);
                Opacity = start + (target - start) * t;

                if (t >= 1.0)
                {
                    timer.Stop();
                    timer.Dispose();
                    onComplete?.Invoke();
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Clignotement léger (creux puis retour à 1.0) au changement de mode
        /// simple/avancé (9.2) : le contenu se redimensionne instantanément
        /// (ClientSize dans ApplyUiMode), ce court fondu adoucit la coupure sans
        /// faire disparaître complètement la fenêtre pendant le redimensionnement.
        /// </summary>
        private void PulseOpacity()
        {
            AnimateOpacity(0.55, 90, () => AnimateOpacity(1.0, 90));
        }

        /// <summary>
        /// Désigne l'icône de chaque bouton (3.1). Depuis 39.0 elle n'est plus
        /// rendue ici : ThemedButton connaît son nom d'icône et la redessine
        /// lui-même à la couleur de son texte courant. Ça règle un problème que
        /// l'ancien code ne pouvait pas avoir — tant que TOUS les boutons
        /// étaient bleus, un blanc fixe convenait ; avec des boutons secondaires
        /// à texte sombre, une icône blanche serait invisible.
        ///
        /// Appelé une fois au démarrage : les couleurs, elles, sont réappliquées
        /// par ThemeManager à chaque changement de thème.
        /// </summary>
        private void ApplyIcons()
        {
            startButton.IconName = "play";
            stopAllButton.IconName = "stop";
            paramsButton.IconName = "sliders";
            checkUpdateButton.IconName = "update";
            tutorialButton.IconName = "book";
            reportBugButton.IconName = "alert";
            diagnosticButton.IconName = "pulse";
            sponsorButton.IconName = "heart";
            websiteButton.IconName = "globe";
        }

        /// <summary>
        /// Mode simple / avancé (3.2). En mode simple : uniquement URL,
        /// Démarrer/Tout arrêter et la liste des enregistrements en cours.
        /// En mode avancé : tout (qualité/codec/format, guide, mises à jour,
        /// rapport de bug, favoris, don, logs). Thème/langue/dossier/cookies/
        /// proxy/reconnexion automatique ont déménagé dans SettingsForm (19.0),
        /// accessible via "Paramètres" dans les deux modes. Le choix est
        /// mémorisé entre les lancements.
        /// </summary>
        private void ApplyUiMode(bool advanced, bool animate = true)
        {
            _advancedMode = advanced;

            // 105 (pas 75) depuis l'ajout du bouton Diagnostic (2.3) sur une
            // troisième rangée de la barre du haut, puis 111 depuis le passage
            // de ces boutons de 24 à 26 px de haut (jambages rognés) : les trois
            // rangées descendent de 2 px chacune, la dernière finissant 6 px plus
            // bas. Garde la même gouttière de 12 px sous la barre du haut.
            const int grpRecordY = 111;
            // 218 (et non 172) depuis l'ajout du minuteur (87.0) sur une deuxième
            // rangée d'options : advancedOptionsPanel est passé de 66 à 112 px.
            const int grpRecordHeightAdvanced = 218;
            const int grpRecordHeightSimple = 110;
            const int sectionGap = 20; // 7.3 : espacement moderne entre sections (20-24px)

            advancedOptionsPanel.Visible = advanced;
            tutorialButton.Visible = advanced;
            checkUpdateButton.Visible = advanced;
            reportBugButton.Visible = advanced;
            diagnosticButton.Visible = advanced;
            grpHistory.Visible = advanced;
            grpFavorites.Visible = advanced;
            grpWatch.Visible = advanced;
            grpDonate.Visible = advanced;
            grpLogs.Visible = advanced;

            grpRecord.Location = new Point(12, grpRecordY);
            grpRecord.Height = advanced ? grpRecordHeightAdvanced : grpRecordHeightSimple;

            var progressY = grpRecordY + grpRecord.Height + sectionGap;
            grpProgress.Location = new Point(12, progressY);

            int naturalHeight;
            if (advanced)
            {
                grpHistory.Location = new Point(12, progressY + grpProgress.Height + sectionGap);
                grpFavorites.Location = new Point(12, grpHistory.Bottom + sectionGap);
                grpWatch.Location = new Point(12, grpFavorites.Bottom + sectionGap);
                grpDonate.Location = new Point(12, grpWatch.Bottom + sectionGap);
                grpLogs.Location = new Point(12, grpDonate.Bottom + sectionGap);
                naturalHeight = grpLogs.Bottom + sectionGap;
            }
            else
            {
                naturalHeight = progressY + grpProgress.Height + sectionGap;
            }

            // Taille "naturelle" du contenu : appliquée à la fenêtre pour un
            // confort immédiat au changement de mode, et comme plancher de
            // défilement (AutoScrollMinSize) si l'utilisateur réduit ensuite
            // la fenêtre manuellement en dessous de cette taille.
            ClientSize = new Size(700, naturalHeight);
            contentPanel.AutoScrollMinSize = new Size(700, naturalHeight);

            // Par Localization (pas de littéral français en dur) : ce texte doit
            // rester dans la langue active même quand on change de mode après
            // avoir choisi l'anglais — bug corrigé au passage (19.0).
            modeToggleButton.Text = advanced
                ? Localization.Get("mode.switchToSimple", _currentLanguage)
                : Localization.Get("mode.switchToAdvanced", _currentLanguage);

            _settings.AdvancedMode = advanced;
            SettingsManager.Save(_settings);

            if (animate) PulseOpacity();
        }

        private async void OnCheckUpdateClick(object? sender, EventArgs e)
        {
            checkUpdateButton.Enabled = false;
            try
            {
                // Toujours une vraie interrogation de l'API, jamais _pendingUpdate :
                // l'utilisateur qui clique attend une réponse à jour, pas le
                // résultat mis en cache par la dernière vérification horaire.
                var update = await UpdateChecker.CheckForUpdateAsync(CurrentVersion);
                if (update == null)
                {
                    ClearPendingUpdate();
                    MessageBox.Show(this, Localization.Format("update.upToDate", CurrentVersion),
                        Localization.Get("dialog.updates"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _pendingUpdate = update;
                await PromptAndInstallAsync(update);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Localization.Format("error.updateCheckFailed", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                checkUpdateButton.Enabled = true;
            }
        }

        /// <summary>
        /// Propose l'installation d'une mise à jour déjà détectée. Partagé par
        /// le bouton "Rechercher une mise à jour" et par le clic sur la
        /// notification de la vérification automatique (79.0), pour que les
        /// deux chemins avertissent des enregistrements en cours à l'identique.
        /// </summary>
        private async Task PromptAndInstallAsync(UpdateInfo update)
        {
            var runningJobs = _jobRows.Count(r => r.Job.Engine.State == DownloadState.Running);
            var warning = runningJobs > 0
                ? Localization.Format("update.runningJobsWarning", runningJobs)
                : "";

            var result = MessageBox.Show(this,
                Localization.Format("update.availableBody", update.Version, CurrentVersion, warning),
                Localization.Get("update.availableTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            AppendLog($"[{DateTime.Now:HH:mm:ss}] Téléchargement de la mise à jour v{update.Version}...");
            await UpdateInstaller.DownloadAndInstallAsync(update, AppConfig.AppDir);
        }

        // Premier passage rapproché (1 min) plutôt qu'immédiat : le démarrage
        // enchaîne déjà validation du dossier, purge des logs, contrôle des ACL
        // et dialogues de premier lancement — inutile d'y ajouter un appel
        // réseau. Les passages suivants sont horaires (79.0).
        private const int AutoUpdateFirstDelayMs = 60 * 1000;
        private const int AutoUpdateIntervalMs = 60 * 60 * 1000;

        private void StartAutoUpdateChecks()
        {
            _autoUpdateTimer = new System.Windows.Forms.Timer { Interval = AutoUpdateFirstDelayMs };
            _autoUpdateTimer.Tick += async (s, e) =>
            {
                if (_autoUpdateTimer!.Interval != AutoUpdateIntervalMs)
                    _autoUpdateTimer.Interval = AutoUpdateIntervalMs;
                await CheckForUpdateInBackgroundAsync();
            };
            _autoUpdateTimer.Start();
        }

        /// <summary>
        /// Vérification de fond (79.0) : silencieuse par construction. Elle ne
        /// vole jamais le focus (pas de MessageBox) — une notification de la
        /// zone de notification, cliquable, laisse l'utilisateur décider quand
        /// s'en occuper, ce qui compte pour une application qui tourne en
        /// arrière-plan pendant des enregistrements de plusieurs heures.
        /// </summary>
        private async Task CheckForUpdateInBackgroundAsync()
        {
            if (!_settings.AutoUpdateCheck || _autoUpdateCheckRunning) return;
            _autoUpdateCheckRunning = true;
            try
            {
                var update = await UpdateChecker.CheckForUpdateAsync(CurrentVersion);

                // L'appel réseau dure jusqu'à 10 s : l'application a pu être
                // fermée entre-temps (OnFormClosing arrête le timer et met
                // _autoUpdateTimer à null, puis libère _notifyIcon). Sans ce
                // garde-fou, la reprise après await toucherait une icône déjà
                // libérée.
                if (_autoUpdateTimer == null || IsDisposed) return;

                if (update == null)
                {
                    ClearPendingUpdate();
                    return;
                }

                _pendingUpdate = update;
                if (!UpdateChecker.ShouldNotify(update.Version, _settings.LastNotifiedUpdateVersion)) return;

                _settings.LastNotifiedUpdateVersion = update.Version;
                SettingsManager.Save(_settings);

                SetTrayText(Localization.Format("tray.updateAvailable", update.Version));
                Logger.Log($"Mise à jour v{update.Version} détectée par la recherche automatique.");
                ShowNotification(
                    Localization.Get("notify.updateAvailable.title"),
                    Localization.Format("notify.updateAvailable.body", update.Version),
                    onClick: () =>
                    {
                        ShowMainWindow();
                        _ = PromptAndInstallAsync(update);
                    });
            }
            catch (Exception ex)
            {
                // Réseau coupé, DNS injoignable ou quota de l'API GitHub atteint :
                // rien de tout ça ne doit interrompre l'utilisateur, contrairement
                // au bouton où il attend explicitement une réponse. Log seulement,
                // et on retentera au passage suivant.
                Logger.Log($"Recherche automatique de mise à jour échouée : {ex.Message}", LogLevel.WARN);
            }
            finally
            {
                _autoUpdateCheckRunning = false;
            }
        }

        /// <summary>
        /// Remet l'info-bulle de la zone de notification à son état neutre après
        /// une mise à jour finalement installée (ou une release retirée) — sans
        /// ça, l'icône continuerait d'annoncer une version déjà en place.
        /// </summary>
        private void ClearPendingUpdate()
        {
            if (_pendingUpdate == null) return;
            _pendingUpdate = null;
            SetTrayText("Chaturbate Recorder");
        }

        /// <summary>
        /// NotifyIcon.Text est limité à 63 caractères par Windows et lève une
        /// exception au-delà — la version est interpolée dans le libellé, donc
        /// la longueur dépend de la traduction : on tronque plutôt que de faire
        /// planter l'appli sur un texte trop long.
        /// </summary>
        private void SetTrayText(string text)
        {
            _notifyIcon.Text = text.Length <= 63 ? text : text[..63];
        }

        /// <summary>
        /// Réduction dans la zone de notification (19.0) : le X masque la
        /// fenêtre au lieu de fermer l'application, pour ne pas interrompre les
        /// enregistrements en cours en arrière-plan. Seul "Fermer" du menu de
        /// la zone de notification (_isReallyClosing = true avant Close())
        /// déclenche la fermeture réelle ci-dessous.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isReallyClosing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();

                if (!_settings.HasSeenTrayHint)
                {
                    ShowNotification(
                        Localization.Get("notify.stillActive.title"),
                        Localization.Get("notify.stillActive.body"));
                    _settings.HasSeenTrayHint = true;
                    SettingsManager.Save(_settings);
                }
                return;
            }

            foreach (var row in _jobRows)
            {
                StopRecordingTimer(row);
                row.Job.Engine.Stop();
            }

            _autoUpdateTimer?.Stop();
            _autoUpdateTimer?.Dispose();
            _autoUpdateTimer = null;

            _watchTimer?.Stop();
            _watchTimer?.Dispose();
            _watchTimer = null;

            // 93.0 : débloque le thread de surveillance (il teste _isReallyClosing
            // au réveil et sort) avant de libérer le handle.
            _secondInstanceEvent?.Set();
            _secondInstanceEvent?.Dispose();
            _secondInstanceEvent = null;

            // Retire l'icône de la zone de notification avant fermeture : sans
            // ça, Windows laisse une icône "fantôme" jusqu'au survol suivant.
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();

            base.OnFormClosing(e);
        }

        /// <summary>Restaure/focalise la fenêtre principale (19.0, tray "Ouvrir").</summary>
        private void ShowMainWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
        }

        private void ShowSettingsDialog()
        {
            using var dialog = new SettingsForm(_settings, _currentTheme, _currentLanguage,
                HandleThemeChangedFromSettings, HandleLanguageChangedFromSettings,
                msg => AppendLog($"[{DateTime.Now:HH:mm:ss}] {msg}"));
            dialog.ShowDialog(this);
        }

        /// <summary>
        /// Signaler un bug (18.0) : ouvre un nouveau ticket GitHub pré-rempli
        /// (titre + version/OS/dossier de capture) dans le navigateur, plutôt
        /// que de collecter/envoyer quoi que ce soit depuis l'appli elle-même.
        /// </summary>
        private void OnReportBugClick(object? sender, EventArgs e)
        {
            try
            {
                var title = Uri.EscapeDataString("[Bug] ");
                var body = Uri.EscapeDataString(
                    $"**Version** : v{CurrentVersion}\n" +
                    $"**Système** : {Environment.OSVersion.VersionString}\n" +
                    $"**Dossier de capture** : {AppConfig.CaptureDir}\n\n" +
                    "**Décris le problème rencontré :**\n\n");
                var url = $"https://github.com/Tomoushie/ChaturbateRecorder/issues/new?title={title}&body={body}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Localization.Format("error.cannotOpenBugReport", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Notification "toast" (3.3) via la zone de notification — le style
        /// visuel moderne est appliqué automatiquement par Windows 10/11 sans
        /// dépendance supplémentaire. Volontairement non bloquant : l'absence
        /// de notification ne doit jamais interrompre l'appli.
        /// </summary>
        private void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info,
            Action? onClick = null)
        {
            _balloonClickAction = onClick;
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

            // Police moderne (7.2) : héritée par tous les contrôles enfants qui
            // ne définissent pas explicitement leur propre Font.
            Font = new Font("Segoe UI", 10F);

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

            // Icône de la zone de notification (3.3) : porte les notifications
            // "toast" (ShowBalloonTip) et, depuis 19.0, un menu clic droit
            // (Ouvrir/Paramètres/Fermer) puisque la fenêtre principale peut
            // désormais être masquée (réduite dans la zone de notification)
            // sans que l'application ne quitte.
            _notifyIcon = new NotifyIcon
            {
                Icon = Icon ?? SystemIcons.Application,
                Text = "Chaturbate Recorder",
                Visible = true,
            };

            _trayOpenItem = new ToolStripMenuItem("Ouvrir", null, (s, e) => ShowMainWindow());
            _traySettingsItem = new ToolStripMenuItem("Paramètres", null, (s, e) => { ShowMainWindow(); ShowSettingsDialog(); });
            _trayCloseItem = new ToolStripMenuItem("Fermer", null, (s, e) => { _isReallyClosing = true; Close(); });

            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(_trayOpenItem);
            trayMenu.Items.Add(_traySettingsItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(_trayCloseItem);
            _notifyIcon.ContextMenuStrip = trayMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
            // Notification cliquable (79.0). L'action est consommée une fois :
            // Windows peut relever l'événement pour un toast déjà traité si
            // l'utilisateur le retrouve dans le centre de notifications.
            _notifyIcon.BalloonTipClicked += (s, e) =>
            {
                var action = _balloonClickAction;
                _balloonClickAction = null;
                action?.Invoke();
            };

            // Barre du haut sur trois lignes : Paramètres/Mode toujours visibles
            // (rangée 1), Guide/Mises à jour/Signaler un bug (rangée 2) et
            // Diagnostic (rangée 3) réservés au mode avancé. Thème/langue ont
            // déménagé dans SettingsForm (19.0), ce qui simplifie cette barre
            // par rapport à la v1.13.0.
            // Hauteur 26 (et non 24) pour la même raison que les lignes de job
            // (voir BuildJobRow) : à 24, l'icône de 16 px posée en
            // ImageBeforeText ne laisse pas assez de place à Segoe UI 9pt et le
            // bas des jambages est rogné ("g" de Guide de démarrage/Diagnostic/
            // bug, "j" de mise à jour, "p" de Mode simple). Les rangées suivent
            // donc 9 / 41 / 73 (26 + 6 px de gouttière) au lieu de 9 / 39 / 69,
            // et grpRecordY dans ApplyUiMode passe de 105 à 111.
            const int topBarButtonHeight = 26;
            const int topBarRow1Y = 9;
            const int topBarRow2Y = topBarRow1Y + topBarButtonHeight + 6;
            const int topBarRow3Y = topBarRow2Y + topBarButtonHeight + 6;

            paramsButton = new ThemedButton { Location = new Point(12, topBarRow1Y), Size = new Size(130, topBarButtonHeight) };
            paramsButton.Click += (s, e) => ShowSettingsDialog();

            modeToggleButton = new ThemedButton { Location = new Point(152, topBarRow1Y), Size = new Size(130, topBarButtonHeight) };

            // 98.0 — rangee 1 (toujours visible), et non rangee 3 avec
            // Diagnostic : la confusion que ce texte dissipe concerne
            // l'utilisateur au moment ou il enregistre, pas seulement celui qui
            // explore le mode avance.
            legalButton = new ThemedButton { Location = new Point(292, topBarRow1Y), Size = new Size(130, topBarButtonHeight) };
            legalButton.Click += (s, e) =>
            {
                using var dialog = new LegalForm(_currentTheme, _currentLanguage);
                dialog.ShowDialog(this);
            };
            modeToggleButton.Click += (s, e) => ApplyUiMode(!_advancedMode);

            tutorialButton = new ThemedButton { Text = "Guide de démarrage", Location = new Point(12, topBarRow2Y), Size = new Size(190, topBarButtonHeight) };
            tutorialButton.Click += (s, e) => ShowTutorial();

            checkUpdateButton = new ThemedButton { Text = "Rechercher une mise à jour", Location = new Point(212, topBarRow2Y), Size = new Size(215, topBarButtonHeight) };
            checkUpdateButton.Click += OnCheckUpdateClick;

            reportBugButton = new ThemedButton { Location = new Point(437, topBarRow2Y), Size = new Size(160, topBarButtonHeight) };
            reportBugButton.Click += OnReportBugClick;

            diagnosticButton = new ThemedButton { Location = new Point(12, topBarRow3Y), Size = new Size(160, topBarButtonHeight) };
            diagnosticButton.Click += (s, e) => new DiagnosticForm().ShowDialog(this);

            // --- Panel : Enregistrement ---
            // Ancrage Left+Right (fenêtre redimensionnable, v1.7.0) : le panneau
            // et son contenu large (URL, dossier, proxy) suivent la largeur de
            // la fenêtre au lieu de rester figés à 660px avec du vide autour
            // quand on l'élargit — seuls les boutons de droite sont ancrés Right
            // seul, pour rester collés au bord plutôt que de s'étirer eux-mêmes.
            grpRecord = new RoundedGroupPanel { Title = "Enregistrement", Location = new Point(12, 75), Size = new Size(660, 272) };
            urlLabel = new Label { Text = "URL Chaturbate :", Location = new Point(12, 25), AutoSize = true };
            urlTextBox = new TextBox { Location = new Point(12, 48), Size = new Size(360, 24) };
            startButton = new ThemedButton { Text = "Démarrer", Location = new Point(382, 46), Size = new Size(120, 28) };
            stopAllButton = new ThemedButton { Text = "Tout arrêter", Location = new Point(512, 46), Size = new Size(136, 28) };
            addFavoriteButton = new ThemedButton { Text = "+ Favori", Location = new Point(445, 78), Size = new Size(198, 24) };

            // Options avancées (qualité/codec/format, dossier, cookies/proxy) :
            // regroupées dans un panneau dédié pour pouvoir les masquer en bloc
            // en Mode simple (3.2), coordonnées relatives au panneau lui-même.
            // Hauteur 112 (et non 66) depuis l ajout du minuteur (87.0) sur une
            // deuxieme rangee ; grpRecordHeightAdvanced dans ApplyUiMode suit.
            advancedOptionsPanel = new Panel { Location = new Point(0, 100), Size = new Size(660, 112) };

            qualityLabel = new Label { Text = "Qualité source :", Location = new Point(12, 12), AutoSize = true };
            qualityCombo = new ThemedComboBox
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

            codecLabel = new Label { Text = "Codec de sortie :", Location = new Point(214, 12), AutoSize = true };
            codecCombo = new ThemedComboBox
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

            formatLabel = new Label { Text = "Format de sortie :", Location = new Point(486, 12), AutoSize = true };
            formatCombo = new ThemedComboBox
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

            // Minuteur (87.0), deuxième rangée. Choix par enregistrement comme
            // les trois précédents : la valeur est lue au démarrage et figée
            // dans le RecordingJob, changer le menu ensuite n'affecte donc pas
            // les enregistrements déjà lancés.
            durationLabel = new Label { Text = "Durée maximale :", Location = new Point(12, 58), AutoSize = true };
            durationCombo = new ThemedComboBox
            {
                Location = new Point(12, 76),
                Size = new Size(190, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            durationCombo.Items.AddRange(new object[]
            {
                "Illimité", "15 minutes", "30 minutes",
                "1 heure", "2 heures", "4 heures", "8 heures"
            });
            durationCombo.SelectedIndex = 0;

            startButton.Click += OnStartClick;
            stopAllButton.Click += OnStopAllClick;
            addFavoriteButton.Click += OnAddFavoriteClick;

            // Dossier de sauvegarde/cookies/proxy/reconnexion automatique ont
            // déménagé dans SettingsForm (19.0) : ce panneau ne contient plus
            // que les choix par enregistrement (qualité/codec/format).
            advancedOptionsPanel.Controls.AddRange(new Control[]
            {
                qualityLabel, qualityCombo, codecLabel, codecCombo, formatLabel, formatCombo,
                durationLabel, durationCombo,
            });

            grpRecord.Controls.AddRange(new Control[]
            {
                urlLabel, urlTextBox, startButton, stopAllButton, addFavoriteButton,
                advancedOptionsPanel
            });
            urlTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            startButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            stopAllButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addFavoriteButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            advancedOptionsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // --- Panel : Enregistrements en cours (plusieurs jobs possibles) ---
            // Hauteurs suivies sur celle d'une ligne de job (BuildJobRow) : 56 de
            // conteneur + 2x2 de Margin = 60 par ligne, donc 122 laisse voir deux
            // lignes entières sans défilement, comme avant l'élargissement des
            // boutons. Le panneau grandit d'autant (140 -> 154) ; tout ce qui est
            // en dessous se replace tout seul, ApplyUiMode calculant les positions
            // à partir de grpProgress.Height.
            grpProgress = new RoundedGroupPanel { Title = "Enregistrements en cours", Location = new Point(12, 320), Size = new Size(660, 154) };
            jobsListPanel = new FlowLayoutPanel
            {
                Location = new Point(12, 22),
                Size = new Size(636, 122),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
            };
            grpProgress.Controls.Add(jobsListPanel);
            jobsListPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // --- Panel : Historique des enregistrements (4.4) ---
            // Hauteur 170 (au lieu de 130) depuis l'affichage des miniatures :
            // en vue Details, la hauteur de ligne suit celle du SmallImageList,
            // donc 27 px au lieu de ~17. Sans cet agrandissement, la liste ne
            // montrerait plus que trois enregistrements. Les panneaux suivants
            // se repositionnent tout seuls (chaine Bottom + sectionGap).
            grpHistory = new RoundedGroupPanel { Title = "Historique des enregistrements", Location = new Point(12, 468), Size = new Size(660, 170) };

            // 48x27 : rapport 16/9 des captures, et hauteur de ligne encore
            // lisible. Plus grand rendrait la liste inutilisable dans ce panneau.
            historyThumbnails = new ImageList
            {
                ImageSize = new Size(48, 27),
                ColorDepth = ColorDepth.Depth32Bit,
            };

            historyListView = new ListView
            {
                Location = new Point(12, 22),
                Size = new Size(470, 138),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                SmallImageList = historyThumbnails,
            };
            // Les quatre largeurs totalisent 445 px, pour une liste de 470 :
            // il reste de quoi loger la barre de défilement verticale (~17 px)
            // sans declencher de barre HORIZONTALE. L'ancien jeu (220+80+70+120
            // = 490) depassait deja, ce qui coupait la colonne Date et ajoutait
            // un ascenseur horizontal — visible seulement a la capture, jamais
            // signale. Toute modification de ces largeurs doit conserver le
            // total sous ~450.
            historyListView.Columns.Add("Fichier", 185);
            historyListView.Columns.Add("Taille", 70);
            historyListView.Columns.Add("Durée", 60);
            historyListView.Columns.Add("Date", 130);

            // Largeur 150 (au lieu de 120) : le Padding horizontal des boutons
            // (8.4/9.2) ne laissait plus assez de place pour "Ouvrir dossier",
            // tronqué en "Ouvrir".
            refreshHistoryButton = new ThemedButton { Text = "Actualiser", Location = new Point(492, 22), Size = new Size(150, 26) };
            openHistoryFolderButton = new ThemedButton { Text = "Ouvrir dossier", Location = new Point(492, 54), Size = new Size(150, 26) };
            openHistoryFileButton = new ThemedButton { Text = "Ouvrir fichier", Location = new Point(492, 86), Size = new Size(150, 26) };

            refreshHistoryButton.Click += (s, e) => RefreshHistoryAsync();
            openHistoryFolderButton.Click += OnOpenHistoryFolderClick;
            openHistoryFileButton.Click += OnOpenHistoryFileClick;

            grpHistory.Controls.AddRange(new Control[] { historyListView, refreshHistoryButton, openHistoryFolderButton, openHistoryFileButton });
            openHistoryFileButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            historyListView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            refreshHistoryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openHistoryFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // --- Panel : Favoris ---
            grpFavorites = new RoundedGroupPanel { Title = "Favoris", Location = new Point(12, 468), Size = new Size(660, 130) };
            favoritesListBox = new ListBox { Location = new Point(12, 22), Size = new Size(460, 98) };
            // Largeur 160 (au lieu de 140) : même correctif que ci-dessus,
            // "Supprimer favori" était tronqué en "Supprimer".
            loadFavoriteButton = new ThemedButton { Text = "Charger", Location = new Point(482, 22), Size = new Size(160, 26) };
            removeFavoriteButton = new ThemedButton { Text = "Supprimer favori", Location = new Point(482, 54), Size = new Size(160, 26) };

            loadFavoriteButton.Click += OnLoadFavoriteClick;
            removeFavoriteButton.Click += OnRemoveFavoriteClick;
            favoritesListBox.DoubleClick += OnLoadFavoriteClick;

            grpFavorites.Controls.AddRange(new Control[] { favoritesListBox, loadFavoriteButton, removeFavoriteButton });

            // --- Panel : Surveillance (88.0) ---
            grpWatch = new RoundedGroupPanel { Title = "Surveillance", Location = new Point(12, 606), Size = new Size(660, 130) };
            watchListView = new ListView
            {
                Location = new Point(12, 22),
                Size = new Size(460, 98),
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
            };
            watchListView.Columns.Add("Salon", 300);
            watchListView.Columns.Add("État", 140);

            // Hauteur de ligne (39.0) : une ListView sans ImageList colle ses
            // lignes à la hauteur de la police (~17 px), ce qui donne un pavé
            // de texte compact là où l'historique, lui, respire déjà grâce à
            // ses miniatures. WinForms n'expose pas de hauteur de ligne : une
            // ImageList vide dont seule la HAUTEUR compte est le seul levier.
            watchListView.SmallImageList = new ImageList { ImageSize = new Size(1, 24) };

            addWatchButton = new ThemedButton { Text = "+ Surveiller", Location = new Point(482, 22), Size = new Size(160, 26) };
            addWatchButton.Click += OnAddWatchClick;
            removeWatchButton = new ThemedButton { Text = "Ne plus surveiller", Location = new Point(482, 54), Size = new Size(160, 26) };
            removeWatchButton.Click += OnRemoveWatchClick;

            grpWatch.Controls.AddRange(new Control[] { watchListView, addWatchButton, removeWatchButton });
            // Ancrage APRÈS AddRange — piège documenté en bas de CLAUDE.md, et
            // déjà payé une fois en v1.23.1 sur le bouton d'import.
            watchListView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            addWatchButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            removeWatchButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            favoritesListBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            loadFavoriteButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            removeFavoriteButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // --- Panel : Soutenir le projet ---
            // Hauteur portée de 100 à 136 pour loger un troisième bouton : les
            // deux précédents descendaient déjà jusqu'à y=94.
            //
            // RoundedGroupPanel dessine son corps jusqu'à Height - 1 - ShadowSize,
            // soit Height - 4, et le dernier bouton ("Site web") finit à y=130.
            //
            // 144 et non 136 : à 136 la garde n'était que de 2 px, ce qui est
            // géométriquement correct (rien ne débordait) mais donne un bouton
            // visuellement collé à la bordure — flagrant sur les captures
            // publiées (99.0), où c'est le premier détail qui saute aux yeux.
            // 144 porte la garde à 10 px, cohérente avec les 34 px qui séparent
            // le haut du panneau du premier bouton. Ne pas descendre en dessous
            // de 134 : à 132 le bouton passait réellement sous la bordure.
            //
            // La position de grpLogs est recalculée à partir de
            // grpDonate.Bottom dans ApplyUiMode, elle suit automatiquement.
            grpDonate = new RoundedGroupPanel { Title = "Soutenir le projet", Location = new Point(12, 606), Size = new Size(660, 144) };
            sponsorButton = new ThemedButton { Text = "Sponsoriser (GitHub)", Location = new Point(12, 34), Size = new Size(220, 32) };
            donateButton = new ThemedButton { Text = "Faire un don (PayPal)", Location = new Point(12, 70), Size = new Size(220, 32) };
            websiteButton = new ThemedButton { Text = "Site web", Location = new Point(12, 106), Size = new Size(220, 24) };
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

            // 50.0 — réseaux sociaux. Rangée logée sous le texte du QR code
            // (x >= 340, y 96..122), la colonne de gauche et le QR occupant
            // déjà tout le reste du panneau : aucune hauteur ajoutée, donc
            // grpLogs et le calcul de ApplyUiMode ne bougent pas.
            // X et Reddit sont des liens de PARTAGE, pas des comptes : le
            // projet n'en a pas, et un bouton menant à un compte inexistant
            // vaudrait moins que rien. Le jour où un compte existe, il suffit
            // de remplacer l'URL ici.
            shareXButton = new ThemedButton { Text = "X", Location = new Point(340, 96), Size = new Size(100, 26) };
            shareRedditButton = new ThemedButton { Text = "Reddit", Location = new Point(448, 96), Size = new Size(100, 26) };
            githubButton = new ThemedButton { Text = "GitHub", Location = new Point(556, 96), Size = new Size(100, 26) };

            const string repoUrl = "https://github.com/Tomoushie/ChaturbateRecorder";
            var shareText = Uri.EscapeDataString("Chaturbate Recorder — enregistreur de lives open source pour Windows");
            var shareUrl = Uri.EscapeDataString(repoUrl);
            shareXButton.Click += (s, e) => OpenExternal($"https://x.com/intent/post?url={shareUrl}&text={shareText}");
            shareRedditButton.Click += (s, e) => OpenExternal($"https://www.reddit.com/submit?url={shareUrl}&title={shareText}");
            githubButton.Click += (s, e) => OpenExternal(repoUrl);

            sponsorButton.Click += OnSponsorClick;
            donateButton.Click += OnDonateClick;
            websiteButton.Click += OnWebsiteClick;

            grpDonate.Controls.AddRange(new Control[]
            {
                sponsorButton, donateButton, websiteButton, qrPictureBox, donateLabel,
                shareXButton, shareRedditButton, githubButton,
            });

            // --- Panel : Logs ---
            grpLogs = new RoundedGroupPanel { Title = "Logs", Location = new Point(12, 714), Size = new Size(660, 220) };
            logListBox = new ListBox
            {
                Location = new Point(12, 22),
                Size = new Size(636, 186),
                HorizontalScrollbar = true,
            };
            grpLogs.Controls.Add(logListBox);
            logListBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // --- Hiérarchie visuelle (39.0) ---
            // UN seul bouton d'accent par zone de l'écran, et rien d'autre :
            // « Démarrer » dans la carte Enregistrement, « Sponsoriser » dans la
            // carte Soutenir le projet. Tout le reste est secondaire par défaut
            // (voir ButtonRole.Secondary). Avant, les 18 boutons de la fenêtre
            // étaient bleus : l'action principale ne se distinguait donc en rien
            // d'un lien de partage, et l'écran n'offrait aucun point d'entrée.
            startButton.Role = ButtonRole.Primary;
            sponsorButton.Role = ButtonRole.Primary;

            // Ce qui interrompt ou supprime. Rouge en texte et bordure
            // seulement : ces quatre boutons sont visibles simultanément en
            // mode avancé, quatre aplats rouges donneraient une fenêtre en
            // alerte permanente alors que rien ne va mal.
            stopAllButton.Role = ButtonRole.Danger;
            removeFavoriteButton.Role = ButtonRole.Danger;
            removeWatchButton.Role = ButtonRole.Danger;

            // Intitulés de champ : un cran en dessous du contenu qu'ils
            // annoncent. Sans ça « Qualité source : » pèse autant que la valeur
            // choisie, et l'œil n'a aucun ordre de lecture.
            foreach (var caption in new Control[] { urlLabel, qualityLabel, codecLabel, formatLabel, durationLabel, donateLabel })
                ThemeManager.SetTextRole(caption, TextRole.Caption);

            contentPanel.Controls.AddRange(new Control[] { paramsButton, tutorialButton, checkUpdateButton, reportBugButton, diagnosticButton, modeToggleButton, legalButton, grpRecord, grpProgress, grpHistory, grpFavorites, grpWatch, grpDonate, grpLogs });
            Controls.Add(contentPanel);

            // Ancrage des panneaux eux-mêmes, posé après leur ajout à
            // contentPanel (même raison que ci-dessus : Parent doit être connu).
            grpRecord.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpProgress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpFavorites.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpWatch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpDonate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
