using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
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
        private Label urlLabel = null!;
        private PlatformStrip platformStrip = null!;
        private TextBox urlTextBox = null!;
        private ThemedButton startButton = null!;
        private ThemedButton stopAllButton = null!;
        private ThemedButton addRoomButton = null!;
        private FlowLayoutPanel roomsListPanel = null!;
        private Label roomsEmptyLabel = null!;
        // Une seule instance pour toutes les cartes : un ToolTip par bouton
        // ferait autant de fenêtres natives que de salons.
        private readonly ToolTip _cardTips = new();
        private Panel advancedOptionsPanel = null!;
        private RoundedGroupPanel grpRecord = null!;
        // 97.0 étape 2c — remplace grpProgress + grpFavorites + grpWatch.
        private RoundedGroupPanel grpRooms = null!;
        private RoundedGroupPanel grpHistory = null!;
        private ListView historyListView = null!;
        private ThemedButton refreshHistoryButton = null!;
        private ThemedButton openHistoryFolderButton = null!;
        // 4.1 : ouverture directe de la vidéo, et miniatures dans la liste.
        private ThemedButton openHistoryFileButton = null!;
        private ImageList historyThumbnails = null!;
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
        // 97.0 — charpente : navigation à gauche, une vue par section.
        private SideBar sideBar = null!;
        private Panel viewStreams = null!;
        private Panel viewHistory = null!;
        private Panel viewSettings = null!;
        private Panel viewSupport = null!;
        private ThemedButton toggleLogsButton = null!;
        private ThemedButton thanksButton = null!;
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
        private sealed class RoomRow
        {
            /// <summary>Adresse du salon. C'est elle qui identifie la ligne, pas le nom
            /// affiché : deux plateformes peuvent donner le même nom lisible.</summary>
            public string Url = "";
            public RoomCard Card = null!;
            public ThemedButton PrimaryButton = null!;
            public ThemedButton OpenButton = null!;
            public ThemedButton RemoveButton = null!;

            /// <summary>
            /// **Le salon existe sans enregistrement, et c'est tout le
            /// changement de 97.0.** Une carte représente un salon connu ; le
            /// job n'est qu'un locataire, présent pendant la capture et son
            /// résultat. Les trois anciens panneaux décrivaient au contraire
            /// trois objets distincts pour un même salon.
            /// </summary>
            public RecordingJob? Job;
            public Action? RestartEngine;
            public System.Windows.Forms.Timer? PendingReconnectTimer;

            /// <summary>Dernier sondage. Sert à Resolve, qui fait primer le job dessus.</summary>
            public RoomStatus PollStatus = RoomStatus.Unknown;

            /// <summary>
            /// Ce salon a-t-il DÉJÀ été sondé une fois ?
            ///
            /// Sans ce drapeau, un salon jamais contrôlé s'affiche « Indéterminé »
            /// — le libellé d'un sondage qui a ÉCHOUÉ. L'ancienne liste de
            /// surveillance distinguait les deux avec « En attente... », et
            /// confondre « je n'ai pas encore regardé » avec « j'ai regardé sans
            /// pouvoir conclure » fait soupçonner une panne là où il n'y a rien.
            /// </summary>
            public bool Polled;

            /// <summary>
            /// Salon absent de la liste persistée : une adresse collée puis
            /// enregistrée sans être ajoutée. Sa carte vit le temps de la
            /// capture, exactement comme l'ancienne ligne de job — l'enregistrer
            /// d'office ferait grossir la liste de quelqu'un sans qu'il l'ait
            /// demandé.
            /// </summary>
            public bool Ephemeral;

            /// <summary>
            /// Miroir de <see cref="RoomEntry.AutoRecord"/>, tenu à jour ici
            /// parce qu'une carte éphémère n'a pas d'entrée persistée à
            /// interroger.
            /// </summary>
            public bool AutoRecordFlag;

            public JobRowStatus JobStatus = JobRowStatus.Preparing;
            public DownloadState? FinishedState;
            public int ReconnectDelaySeconds;
            public bool HasProgressPct;
            public double LastProgressPct;

            // Minuteur (87.0). Le temps restant s'affiche désormais sur la
            // ligne de détail de la carte ; ce timer déclenche toujours l'arrêt
            // à l'échéance — un seul objet à arrêter.
            public System.Windows.Forms.Timer? CountdownTimer;
            public string Countdown = "";
        }

        // --- État ---
        // 97.0 — une seule liste de salons remplace FavoritesManager et
        // WatchListManager. Les deux anciens fichiers sont lus une dernière
        // fois par RoomStore.Load(), qui migre puis les laisse en place.
        private readonly RoomStore _rooms = new();
        private System.Windows.Forms.Timer? _watchTimer;
        // Empêche deux passages de se chevaucher : un contrôle prend quelques
        // secondes par salon, une liste fournie peut dépasser l'intervalle.
        private bool _watchTickRunning;
        private readonly List<RoomRow> _roomRows = new();
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

            // 97.0 — un seul chargement pour les deux anciennes listes. Load()
            // migre favorites.json + watchlist.json vers rooms.json au premier
            // lancement, et NE SUPPRIME PAS les anciens fichiers : revenir à une
            // version antérieure doit rester possible.
            _rooms.Load();
            foreach (var salon in _rooms.Rooms)
            {
                var ligne = BuildRoomRow(salon.Url, ephemeral: false);
                ligne.AutoRecordFlag = salon.AutoRecord;
                RefreshCard(ligne);
                _roomRows.Add(ligne);
            }
            UpdateRoomsEmptyState();

            LoadQrImage();
            ThemeManager.Apply(this, _currentTheme);
            ApplyIcons();
            ApplyLanguage(_currentLanguage);
            // 97.0 — la fenêtre s'ouvre sur la section Streams. Le réglage
            // AdvancedMode n'est plus lu : il pilotait un mode qui n'existe
            // plus. Il reste dans settings.json sans effet, plutôt que d'être
            // supprimé — un réglage retiré de force ferait échouer la lecture
            // d'un fichier écrit par une version antérieure.
            ClientSize = new Size(SideBar.DefaultWidth + 700, 720);
            BasculerLogs(_settings.ShowLogs);
            ShowView("streams");
            RefreshHistoryAsync();
            ShowFirstRunDialogs();

            // Fondu d'ouverture (9.2) : la fenêtre démarre invisible (Opacity=0)
            // et remonte à pleine opacité une fois affichée — Shown ne se
            // déclenche qu'après Application.Run(new MainForm()), donc après les
            // dialogues de premier lancement éventuels ci-dessus.
            // FONDU D'OUVERTURE SUPPRIMÉ (9.2 → 97.0), et ce n'est pas un
            // renoncement esthétique : il produisait chez le mainteneur, à
            // CHAQUE lancement et sur tous les onglets, des encoches sombres
            // aux coins arrondis des boutons, qui ne partaient qu'au survol du
            // bouton concerné.
            //
            // Une fenêtre dont Opacity < 1 est une fenêtre EN COUCHE : Windows
            // compose son rendu avec l'alpha courant, et un contrôle peint
            // pendant le fondu garde ce résultat composité tant que rien ne le
            // repeint. Un repaint complet différé après le fondu a été essayé :
            // insuffisant chez lui. Sans opacité partielle, il n'y a plus de
            // fenêtre en couche, donc plus de composition partielle possible.
            //
            // Le défaut n'a jamais pu être reproduit sur la machine de
            // développement — d'où le choix de supprimer la CAUSE possible
            // plutôt que de continuer à traiter un symptôme invisible d'ici.
            // Un agrément décoratif ne vaut pas un défaut visible à chaque
            // lancement.

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
            using var tutorial = new TutorialForm(_currentTheme);
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
        /// Largeur réservée aux trois boutons d'action, à droite de
        /// l'interrupteur. La carte s'en sert pour placer son interrupteur sans
        /// jamais passer dessous — d'où une constante partagée plutôt que deux
        /// nombres à garder d'accord.
        /// </summary>
        private const int CardActionsWidth = 180;

        /// <summary>
        /// Crée la carte d'un salon et la pose dans la liste.
        ///
        /// **Une carte existe dès que le salon est connu**, bien avant qu'un
        /// enregistrement ne commence, et lui survit. C'est tout l'écart avec
        /// l'ancienne ligne de job, qui naissait et mourait avec sa capture :
        /// un même salon pouvait alors figurer à trois endroits à la fois
        /// (favoris, surveillance, enregistrement en cours), chacun avec sa
        /// propre vérité sur ce qu'il faisait.
        /// </summary>
        private RoomRow BuildRoomRow(string url, bool ephemeral)
        {
            var (icone, _) = Platforms.Badge(Platforms.Detect(url));

            var card = new RoomCard
            {
                RoomName = Platforms.DisplayName(url),
                PlatformIcon = icone,
                ActionsWidth = CardActionsWidth,
                Palette = ThemeManager.GetPalette(_currentTheme),
                Width = Math.Max(320, roomsListPanel.Width - SystemInformation.VerticalScrollBarWidth - 6),
            };

            // Icône seule pour « ouvrir » et « retirer » : ThemedButton centre
            // le groupe icône+texte, un texte vide laisse donc l'icône au
            // milieu. Trois libellés complets ne tiendraient pas dans les
            // 180 px réservés sans réduire la carte à sa zone de boutons.
            var primary = new ThemedButton { Size = new Size(104, 26), IconSize = 14 };
            var open = new ThemedButton { Size = new Size(30, 26), IconName = "open", IconSize = 14 };
            var remove = new ThemedButton { Size = new Size(30, 26), IconName = "trash", IconSize = 14, Role = ButtonRole.Danger };

            _cardTips.SetToolTip(open, Localization.Get("job.open", _currentLanguage));
            _cardTips.SetToolTip(remove, Localization.Get("job.remove", _currentLanguage));

            card.Controls.AddRange(new Control[] { primary, open, remove });

            var row = new RoomRow
            {
                Url = url,
                Card = card,
                PrimaryButton = primary,
                OpenButton = open,
                RemoveButton = remove,
                Ephemeral = ephemeral,
                AutoRecordFlag = false,
            };

            open.Click += (s, e) => OpenRoomPage(row);
            remove.Click += (s, e) => RemoveRoomRow(row);
            primary.Click += (s, e) => OnCardPrimaryClick(row);

            // L'interrupteur est dessiné par la carte, pas par un contrôle :
            // c'est elle qui prévient quand il bascule.
            card.AutoRecordToggled += (s, e) =>
            {
                row.AutoRecordFlag = card.AutoRecord;

                // Surveiller un salon éphémère revient à demander à le garder :
                // sans ça, l'interrupteur s'oublierait à la fermeture et rien
                // ne l'aurait dit.
                if (row.Ephemeral && card.AutoRecord)
                {
                    _rooms.Add(row.Url, autoRecord: true);
                    row.Ephemeral = false;
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] {card.RoomName} ajouté à la liste (surveillance activée).");
                }
                else if (!row.Ephemeral)
                {
                    _rooms.SetAutoRecord(row.Url, card.AutoRecord);
                }
            };

            // Parentée AVANT l'application du thème (39.0) : ThemeManager
            // remonte la chaîne des parents pour savoir sur quelle surface il
            // peint. Une carte encore orpheline recevrait le fond de la FENÊTRE.
            roomsListPanel.Controls.Add(card);
            LayoutCardActions(row);
            RefreshCard(row);
            ThemeManager.Apply(card, _currentTheme);
            return row;
        }

        /// <summary>
        /// Place les trois boutons contre le bord droit de la carte. Rappelé à
        /// chaque redimensionnement : la carte suit la largeur de la liste, et
        /// des boutons ancrés à droite par Anchor se poseraient d'après une
        /// largeur pas encore établie — le piège d'Anchor documenté en bas de
        /// CLAUDE.md, déjà payé deux fois.
        /// </summary>
        /// <summary>
        /// Largeur d'une carte.
        ///
        /// **Calculée sur `Width`, JAMAIS sur `ClientSize`, et la place de
        /// l'ascenseur vertical est réservée en permanence.** `ClientSize`
        /// rétrécit au moment où cet ascenseur apparaît : les cartes gardaient
        /// alors la largeur d'avant et débordaient, ce qui ajoutait un ascenseur
        /// HORIZONTAL — visible sur la première capture, et invisible à la
        /// lecture du code. Réserver 17 px en permanence coûte un liseré vide à
        /// droite quand la liste est courte ; c'est le prix d'une largeur qui ne
        /// dépend pas de ce qu'elle provoque.
        /// </summary>
        private void ResizeRoomCards()
        {
            var largeur = Math.Max(320,
                roomsListPanel.Width - SystemInformation.VerticalScrollBarWidth - 6);

            foreach (var row in _roomRows)
            {
                row.Card.Width = largeur;
                LayoutCardActions(row);
            }
        }

        private static void LayoutCardActions(RoomRow row)
        {
            const int marge = 14;
            const int ecart = 6;
            var y = (RoomCard.CompactHeight - row.PrimaryButton.Height) / 2;

            var x = row.Card.Width - marge - row.RemoveButton.Width;
            row.RemoveButton.Location = new Point(x, y);

            x -= ecart + row.OpenButton.Width;
            row.OpenButton.Location = new Point(x, y);

            x -= ecart + row.PrimaryButton.Width;
            row.PrimaryButton.Location = new Point(x, y);
        }

        private void OpenRoomPage(RoomRow row)
        {
            try
            {
                Process.Start(new ProcessStartInfo(row.Url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.Format("error.cannotOpenPage", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Le bouton principal change de rôle avec l'état de la carte : démarrer
        /// quand rien ne tourne, arrêter pendant la capture, annuler pendant
        /// l'attente d'une reconnexion. Un seul bouton, parce qu'à tout instant
        /// une seule de ces actions a un sens.
        /// </summary>
        private void OnCardPrimaryClick(RoomRow row)
        {
            if (row.Job is { } job && job.Engine.State == DownloadState.Running)
            {
                job.Engine.Stop();
                return;
            }

            if (row.PendingReconnectTimer != null)
            {
                row.PendingReconnectTimer.Stop();
                row.PendingReconnectTimer.Dispose();
                row.PendingReconnectTimer = null;
                if (row.Job != null)
                {
                    row.Job.AutoReconnectEnabled = false;
                    AppendJobLog(row.Job, "Reconnexion automatique annulée.");
                }
                row.JobStatus = JobRowStatus.Cancelled;
                RefreshCard(row);
                return;
            }

            StartRecording(row.Url, interactive: true);
        }

        /// <summary>
        /// Câble un enregistrement sur la carte d'un salon. Les gestionnaires du
        /// moteur pointent la LIGNE et non le job : la ligne survit à la capture
        /// et en accueillera une autre.
        /// </summary>
        private void AttachJob(RoomRow row, RecordingJob job)
        {
            row.Job = job;
            row.JobStatus = JobRowStatus.Preparing;
            row.FinishedState = null;
            row.HasProgressPct = false;
            row.LastProgressPct = 0;
            row.Countdown = "";

            job.Engine.OnLogLine      += line => SafeInvoke(() => AppendJobLog(job, line));
            job.Engine.OnProgress     += pct  => SafeInvoke(() => UpdateJobProgress(row, pct));
            job.Engine.OnStateChanged += state => SafeInvoke(() => HandleJobStateChanged(row, state));

            RefreshCard(row);
        }

        private RoomRow? FindRoomRow(string url)
        {
            var cle = RoomStore.Normalize(url);
            return _roomRows.FirstOrDefault(r => string.Equals(RoomStore.Normalize(r.Url), cle, StringComparison.OrdinalIgnoreCase));
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
        private void RemoveRoomRow(RoomRow row)
        {
            // Confirmation demandée par le mainteneur : retirer un salon coupe
            // sa capture, et c'est la seule action de cette liste qui détruise
            // quelque chose qu'on ne peut pas refaire — le direct, lui, ne se
            // rejoue pas.
            if (row.Job is { } enCours && enCours.Engine.State == DownloadState.Running)
            {
                var reponse = MessageBox.Show(this,
                    Localization.Format("room.removeRecording", row.Card.RoomName),
                    Localization.Get("dialog.confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (reponse != DialogResult.Yes) return;
            }

            StopJobMachinery(row);

            if (!row.Ephemeral) _rooms.Remove(row.Url);

            roomsListPanel.Controls.Remove(row.Card);
            _roomRows.Remove(row);
            UpdateRoomsEmptyState();
            AppendLog($"[{DateTime.Now:HH:mm:ss}] {Localization.Format("room.removed", row.Card.RoomName)}");

            // Dispose différé : l'un des appelants est le gestionnaire Click du
            // bouton de la carte détruite, qui doit avoir fini de s'exécuter
            // avant que son bouton disparaisse.
            BeginInvoke(() => row.Card.Dispose());
        }

        /// <summary>
        /// Coupe tout ce qu'un enregistrement laisse tourner : les deux minuteurs
        /// et le moteur.
        ///
        /// **Les timers comptent** (95.0) : sans ça, retirer une carte pendant
        /// une tentative de reconnexion laissait yt-dlp tourner et rejouer le
        /// cycle Running/Failed dans le vide. `AutoReconnectEnabled` est coupé
        /// AVANT `Stop()`, pour que l'état final ne reprogramme rien.
        /// </summary>
        private static void StopJobMachinery(RoomRow row)
        {
            StopRecordingTimer(row);

            if (row.PendingReconnectTimer != null)
            {
                row.PendingReconnectTimer.Stop();
                row.PendingReconnectTimer.Dispose();
                row.PendingReconnectTimer = null;
            }

            if (row.Job == null) return;
            row.Job.AutoReconnectEnabled = false;
            row.Job.Engine.Stop();
        }

        /// <summary>
        /// Seul endroit qui traduit l'état logique d'une ligne de job
        /// (JobRowStatus) en texte affiché — appelé à chaque changement d'état
        /// ET depuis ApplyLanguage pour retraduire les lignes déjà affichées
        /// sans perdre leur état courant (ex : ne pas remplacer un pourcentage
        /// en cours par "En cours...").
        /// </summary>
        private void RefreshCard(RoomRow row)
        {
            string L(string key) => Localization.Get(key, _currentLanguage);

            // L'état affiché est DÉRIVÉ, jamais stocké : Resolve croise le
            // dernier sondage, l'état du moteur et l'attente d'une reconnexion,
            // en faisant primer l'enregistrement sur le sondage. Un sondage peut
            // échouer pendant que la capture, elle, reçoit des données.
            var etat = RoomStore.Resolve(row.PollStatus, row.Job?.Engine.State, row.PendingReconnectTimer != null);
            row.Card.State = etat;
            row.Card.AutoRecord = row.AutoRecordFlag;

            var enregistre = etat == RoomRowState.Recording;
            var attente = etat == RoomRowState.Reconnecting;

            // Les libellés viennent des deux anciens panneaux : la surveillance
            // disait déjà « en ligne / hors ligne / introuvable », la ligne de
            // job « terminé / échec ». En créer des doublons ferait diverger les
            // deux jeux dès la première reformulation.
            row.Card.StateLabel = row.JobStatus == JobRowStatus.Cancelled && !enregistre && !attente
                ? L("job.cancelled")
                : etat switch
                {
                    RoomRowState.Recording => L("watch.state.recording"),
                    RoomRowState.Reconnecting => string.Format(L("job.reconnectIn"), row.ReconnectDelaySeconds),
                    RoomRowState.Live => L("watch.state.online"),
                    RoomRowState.NotFound => L("watch.state.notfound"),
                    // « En attente » tant que rien n'a été contrôlé, « Indéterminé »
                    // seulement après un sondage qui n'a pas pu conclure.
                    RoomRowState.Unknown => L(row.Polled ? "watch.state.unknown" : "watch.state.pending"),
                    RoomRowState.Failed => L("job.state.failed"),
                    RoomRowState.Finished => row.FinishedState == DownloadState.Completed
                        ? L("job.state.completed")
                        : L("job.state.stopped"),
                    _ => L("watch.state.offline"),
                };

            // Ligne de détail, visible seulement quand la carte est étendue :
            // ce qui se MESURE, par opposition à l'état qui se nomme.
            var morceaux = new List<string>();
            if (enregistre)
            {
                morceaux.Add(row.HasProgressPct
                    ? FormatProgressPct(row.LastProgressPct)
                    : L(row.JobStatus == JobRowStatus.Preparing ? "job.preparing" : "job.running"));
            }
            if (row.Countdown.Length > 0) morceaux.Add("⏱ " + row.Countdown);
            row.Card.Detail = string.Join("   ·   ", morceaux);

            // Un direct n'annonce pas de pourcentage : sa durée n'est pas connue
            // d'avance. Sans barre indéterminée, une capture bien vivante
            // s'afficherait figée à 0 %.
            row.Card.Indeterminate = (enregistre || attente) && !row.HasProgressPct;
            row.Card.Progress = row.HasProgressPct ? (int)Math.Round(row.LastProgressPct) : 0;

            row.PrimaryButton.Text = enregistre ? L("job.stop") : attente ? L("job.cancel") : L("button.start");
            row.PrimaryButton.IconName = enregistre ? "stop" : attente ? null : "play";
            // Secondary et non Primary pour « Démarrer » : la règle de 39.0 veut
            // un seul bouton d'accent par zone, et une liste de vingt salons en
            // afficherait vingt. Danger reste discret (texte et bordure rouges
            // sur fond neutre) pour la même raison.
            row.PrimaryButton.Role = enregistre || attente ? ButtonRole.Danger : ButtonRole.Secondary;
        }

        /// <summary>
        /// Démarre le minuteur d'une ligne (87.0), si l'utilisateur en a demandé
        /// un. Appelé au passage en Running.
        ///
        /// L'échéance n'est calculée qu'au PREMIER démarrage : une reconnexion
        /// automatique repasse par Running, mais ne doit pas repousser l'arrêt,
        /// sinon une room instable enregistrerait indéfiniment.
        /// </summary>
        private void StartRecordingTimer(RoomRow row)
        {
            if (row.Job is not { } job || job.TimerMinutes <= 0) return;

            job.StopAtUtc ??= DateTime.UtcNow.AddMinutes(job.TimerMinutes);
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
        private void UpdateCountdown(RoomRow row)
        {
            if (row.Job is not { } job || job.StopAtUtc is not { } echeance) return;

            var restant = echeance - DateTime.UtcNow;
            if (restant > TimeSpan.Zero)
            {
                row.Countdown = RecordingTimer.FormatRemaining(restant);
                RefreshCard(row);
                return;
            }

            // Échéance atteinte : on coupe le timer AVANT d'arrêter le moteur,
            // pour ne pas re-déclencher l'arrêt à chaque tick suivant.
            StopRecordingTimer(row);
            row.Countdown = "";

            AppendJobLog(job, $"Durée maximale atteinte ({job.TimerMinutes} min) : arrêt de l'enregistrement.");

            // Engine.Stop() marque l'arrêt comme manuel, donc l'état final sera
            // Stopped — ce qui exclut la reconnexion automatique dans
            // HandleJobStateChanged. Un minuteur qui relancerait aussitôt
            // l'enregistrement n'aurait aucun sens.
            if (job.Engine.State == DownloadState.Running)
                job.Engine.Stop();
        }

        /// <summary>
        /// Arrête et libère le minuteur d'une ligne. Sans effet s'il n'y en a
        /// pas — appelable depuis tous les chemins de sortie sans condition.
        /// </summary>
        private static void StopRecordingTimer(RoomRow row)
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

        private void UpdateJobProgress(RoomRow row, double pct)
        {
            if (row.Job is not { } job) return;

            row.HasProgressPct = true;
            row.LastProgressPct = pct;
            RefreshCard(row);
            // 95.0 : c'est ICI que le compteur de reconnexions se remet à zéro.
            // Recevoir une progression est la seule preuve que le flux existe
            // vraiment ; l'état Running ne prouve que le démarrage du processus.
            job.ReconnectAttempt = 0;
        }

        private void HandleJobStateChanged(RoomRow row, DownloadState state)
        {
            // 95.0 : une carte retirée n'a plus rien à afficher ni à notifier.
            // Le moteur peut encore lever un changement d'état après le retrait
            // (processus yt-dlp en cours de fin), ce qui produisait une
            // notification d'erreur pour un enregistrement disparu de l'écran.
            if (!_roomRows.Contains(row) || row.Job is not { } job) return;

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
                    row.JobStatus = JobRowStatus.Running;
                    row.HasProgressPct = false;
                    RefreshCard(row);
                    StartRecordingTimer(row);
                    break;

                case DownloadState.Completed:
                case DownloadState.Failed:
                case DownloadState.Stopped:
                    row.JobStatus = JobRowStatus.Finished;
                    row.FinishedState = state;
                    // Le minuteur n'a plus lieu d'etre, quelle que soit la raison de
                    // la fin. Si l'enregistrement reprend (reconnexion automatique),
                    // StartRecordingTimer le relancera sur l'echeance initiale.
                    StopRecordingTimer(row);
                    row.Countdown = "";
                    RefreshCard(row);
                    AppendJobLog(job, $"Job terminé (état : {state}).");
                    RefreshHistoryAsync();

                    // Finaliser le .part, quelle que soit la façon dont ça s'est
                    // terminé. Un live s'arrête TOUJOURS par un Kill du
                    // processus, donc yt-dlp ne renomme jamais son fichier
                    // temporaire lui-même : sans ça l'enregistrement reste un
                    // « X.mp4.part » que rien ne lit, absent de l'historique et
                    // sans miniature. Signalé en usage réel.
                    FinalizeCaptureAsync(job, avecMiniature: state != DownloadState.Failed);

                    if (state == DownloadState.Stopped)
                    {
                        AppendJobLog(job, "Téléchargement interrompu.");
                    }
                    else
                    {
                        if (state == DownloadState.Completed)
                            ShowNotification(Localization.Get("notify.recordingDone.title"), job.RoomName);
                        else
                            ShowNotification(Localization.Get("notify.recordingError.title"),
                                Localization.Format("notify.recordingError.body", job.RoomName), ToolTipIcon.Error);

                        // Reconnexion automatique (4.2) : uniquement si le job ne s'est
                        // PAS arrêté manuellement (cas déjà exclu ci-dessus) et que
                        // l'utilisateur a coché l'option pour cet enregistrement.
                        if (job.AutoReconnectEnabled && job.ReconnectAttempt < AppConfig.AutoReconnectMaxAttempts)
                            ScheduleReconnect(row);
                    }

                    // Le réencodage se fait toujours en post-traitement sur le fichier
                    // final, y compris après un arrêt manuel : yt-dlp ne réencode qu'à
                    // la fin normale d'un téléchargement, or un live s'arrête toujours
                    // par un Kill du process (STOP ou fermeture du formulaire), qui ne
                    // laisse jamais ce post-traitement interne s'exécuter.
                    if (job.CodecChoice != "copy" && state != DownloadState.Failed
                        && SafeMode.IsEnabled(SafeComponent.Ffmpeg))
                        ReencodeCaptureAsync(job);
                    break;
            }
        }

        /// <summary>
        /// Reconnexion automatique (4.2) : programme une nouvelle tentative
        /// après le délai configuré. Le bouton Stop de la ligne devient
        /// "Annuler" tant que la reconnexion est en attente.
        /// </summary>
        private void ScheduleReconnect(RoomRow row)
        {
            if (row.Job is not { } job) return;

            job.ReconnectAttempt++;
            var attempt = job.ReconnectAttempt;
            var delaySeconds = AppConfig.AutoReconnectDelaySeconds;

            AppendJobLog(job, $"Reconnexion automatique dans {delaySeconds}s (tentative {attempt}/{AppConfig.AutoReconnectMaxAttempts})...");
            row.JobStatus = JobRowStatus.ReconnectPending;
            row.ReconnectDelaySeconds = delaySeconds;

            var timer = new System.Windows.Forms.Timer { Interval = delaySeconds * 1000 };
            row.PendingReconnectTimer = timer;
            // Posé AVANT le rafraîchissement : c'est la présence de ce minuteur
            // que Resolve lit pour décider de l'état « reconnexion ».
            RefreshCard(row);

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                row.PendingReconnectTimer = null;
                if (!_roomRows.Contains(row)) return;

                AppendJobLog(job, $"Nouvelle tentative de connexion ({attempt}/{AppConfig.AutoReconnectMaxAttempts})...");
                row.RestartEngine?.Invoke();
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

                    // RÉALISER LA LISTE D'IMAGES AVANT D'Y AJOUTER QUOI QUE CE
                    // SOIT. Tant que son handle natif n'existe pas, Images.Add
                    // ne recopie rien : elle GARDE la référence et ne la
                    // matérialise qu'à la création du handle. Les bitmaps
                    // libérés trois lignes plus bas sont alors déjà morts quand
                    // Windows vient les lire, et la ListView plante à
                    // l'affichage — ArgumentException « Parameter is not valid »
                    // dans OnHandleCreated, très loin de sa cause.
                    //
                    // Le défaut est une COURSE, d'où son caractère intermittent :
                    // il ne se produit que si ce rafraîchissement se termine
                    // avant que la ListView ait obtenu son handle. Constaté en
                    // v1.35.0 à deux reprises au lancement (journaux de crash du
                    // 2026-08-10, 19:54 et 21:50).
                    //
                    // `_ = historyListView.Handle` dans le constructeur couvrait
                    // déjà ce cas, mais par un ORDRE D'EXÉCUTION : une ligne
                    // lointaine devait s'exécuter avant celle-ci. Le garde-fou
                    // est ici posé là où le risque est pris, donc il ne peut
                    // plus être contourné par un chemin d'appel nouveau.
                    _ = historyThumbnails.Handle;

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
                // TOUJOURS le dossier de capture, jamais le fichier sélectionné.
                //
                // Avant, une sélection dans la liste faisait ouvrir
                // l'Explorateur AVEC ce fichier mis en évidence — ce qui
                // doublonnait « Ouvrir fichier » juste à côté et surprenait :
                // le bouton s'appelle « Ouvrir dossier », il doit ouvrir le
                // dossier, celui des Paramètres, quoi qui soit sélectionné.
                Process.Start(new ProcessStartInfo(AppConfig.CaptureDir) { UseShellExecute = true });
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
        /// <summary>
        /// Renomme le fichier temporaire de yt-dlp en son nom définitif, puis
        /// génère la miniature (97.0, signalé en usage réel).
        ///
        /// **Pourquoi c'est nécessaire** : un live ne se termine jamais tout
        /// seul. L'application tue le processus — bouton Stop, fermeture, ou
        /// watchdog — et yt-dlp n'a donc jamais l'occasion de renommer son
        /// `.part`. Le résultat restait un « X.mp4.part » : absent de
        /// l'historique (qui ne liste que .mp4/.mkv/.mov), sans miniature, et
        /// que l'utilisateur devait renommer à la main pour le lire.
        ///
        /// **Réessais, parce que le fichier peut encore être verrouillé** : le
        /// processus vient d'être tué, Windows relâche le handle avec un léger
        /// retard. Un seul essai échouait donc par intermittence — exactement
        /// le genre de défaut qu'on croit corrigé jusqu'à ce qu'il revienne.
        ///
        /// **Un .part VIDE n'est pas renommé** : cela créerait un fichier de
        /// 0 octet dans l'historique, à côté des vrais enregistrements.
        /// </summary>
        private void FinalizeCaptureAsync(RecordingJob job, bool avecMiniature)
        {
            if (job.OutputBaseName == null) return;

            var final = Path.Combine(job.CaptureDir, $"{job.OutputBaseName}.{job.ContainerExt}");
            var part = final + ".part";
            // Lu ici, sur le thread UI : SafeMode s'appuie sur un dictionnaire
            // statique sans verrou, que le contrôle des composants peut vider.
            var miniature = avecMiniature && SafeMode.IsEnabled(SafeComponent.Ffmpeg);

            Task.Run(async () =>
            {
                try
                {
                    if (!File.Exists(final) && File.Exists(part))
                    {
                        for (var essai = 0; essai < 12; essai++)
                        {
                            try
                            {
                                if (new FileInfo(part).Length == 0)
                                {
                                    Logger.Log($"Fichier temporaire vide, non renommé : {part}", LogLevel.WARN);
                                    return;
                                }
                                File.Move(part, final);
                                Logger.Log($"Enregistrement finalisé : {Path.GetFileName(final)}", LogLevel.INFO);
                                break;
                            }
                            catch (IOException) when (essai < 11)
                            {
                                await Task.Delay(250).ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Finalisation impossible ({part}) : {ex.Message}", LogLevel.ERROR);
                }

                // La miniature AVANT le rafraîchissement, et non en parallèle de
                // lui. C'est la seconde moitié du défaut signalé en usage réel :
                // le renommage du .part marchait, et pourtant l'historique
                // restait sans image. `RefreshHistoryAsync` scanne le disque et
                // lit le .jpg AU MOMENT DU SCAN ; lancée en tâche de fond, ffmpeg
                // met environ une seconde à ouvrir la vidéo et à écrire l'image
                // (mesuré sur les captures réelles, de 13 Mo à 1,6 Go). Le
                // rafraîchissement gagnait donc toujours la course, et comme rien
                // ne le rejoue, la ligne restait sans miniature jusqu'à un
                // rafraîchissement manuel. Attendre coûte une seconde avant que la
                // ligne apparaisse — infiniment moins que de ne jamais voir l'image.
                //
                // Sans ffmpeg, pas de miniature — mais la capture, elle, a bien eu
                // lieu : il n'y a aucune raison de la perdre.
                if (miniature) await GenerateThumbnailAsync(job).ConfigureAwait(false);

                SafeInvoke(RefreshHistoryAsync);
            });
        }

        /// <summary>
        /// Extrait une image de la capture, posée en .jpg à côté d'elle.
        ///
        /// **Repli sur le début du fichier** : `-ss` placé avant `-i` cherche la
        /// position demandée dans la vidéo, et au-delà de sa fin ffmpeg n'écrit
        /// RIEN — mesuré, sortie -22 et « nothing was written into output file ».
        /// Un enregistrement plus court que `ThumbnailOffsetSeconds` (un essai,
        /// un live coupé aussitôt) n'avait donc jamais de miniature. Le second
        /// appel ne coûte que dans ce cas-là, puisqu'il ne part que sur échec.
        /// </summary>
        private async Task GenerateThumbnailAsync(RecordingJob job)
        {
            var videoFile = FindOwnCaptureFile(job);
            if (videoFile == null)
            {
                SafeInvoke(() => AppendJobLog(job, "Aucune vidéo trouvée."));
                return;
            }

            var thumbnail = Path.Combine(videoFile.DirectoryName!, Path.GetFileNameWithoutExtension(videoFile.Name) + ".jpg");

            try
            {
                var ok = await ExtractFrameAsync(videoFile.FullName, thumbnail,
                    AppConfig.ThumbnailOffsetSeconds).ConfigureAwait(false);

                if (!ok && AppConfig.ThumbnailOffsetSeconds > 0)
                    ok = await ExtractFrameAsync(videoFile.FullName, thumbnail, 0).ConfigureAwait(false);

                SafeInvoke(() => AppendJobLog(job, ok
                    ? $"Miniature créée : {thumbnail}"
                    : "Erreur création miniature."));
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la génération de la miniature : {ex.Message}", LogLevel.WARN);
            }
        }

        /// <summary>
        /// Un appel à ffmpeg, une image. Le verdict est l'existence du fichier et
        /// non le code de sortie : c'est le fichier que l'historique ira lire.
        /// </summary>
        private static async Task<bool> ExtractFrameAsync(string video, string thumbnail, int offsetSeconds)
        {
            var psi = new ProcessStartInfo
            {
                FileName = AppConfig.FFmpegPath,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var a in new[]
            {
                "-ss", offsetSeconds.ToString(),
                "-i", video,
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
            if (p == null) return false;

            using var delai = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                await p.WaitForExitAsync(delai.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Un ffmpeg figé garderait un handle de lecture sur la capture et
                // ferait échouer le réencodage qui suit. L'ancien code se contentait
                // d'abandonner l'attente en le laissant tourner.
                try { p.Kill(entireProcessTree: true); } catch { /* déjà parti */ }
                return false;
            }

            return File.Exists(thumbnail);
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

        // BuildPlatformImageList a disparu avec la ListView de surveillance
        // (97.0 étape 2c). Son ImageList existait pour porter un pictogramme
        // par ligne (103.0) ET pour fixer la hauteur de ligne, faute d'autre
        // levier en WinForms. La carte de salon dessine son pictogramme
        // elle-même, à la couleur du thème — ce que l'ImageList ne savait pas
        // faire, d'où le gris fixe de compromis — et choisit sa hauteur.

        private static string RoomNameFromUrl(string url)
        {
            // Délégué à Platforms depuis 40.0 : le premier segment d'URL suffit
            // pour Chaturbate et Twitch, mais donne « watch » sur toutes les
            // adresses YouTube du type /watch?v=… — donc le même nom pour tous
            // les enregistrements de la plateforme.
            return Platforms.DisplayName(url);
        }

        /// <summary>
        /// « + Ajouter » : fait entrer l'adresse saisie dans la liste des salons.
        /// Remplace à la fois « + Favori » et « + Surveiller », qui ajoutaient le
        /// même salon à deux endroits différents. La surveillance s'active
        /// ensuite d'un clic sur l'interrupteur de la carte — c'est la
        /// distinction que le mainteneur avait établie et que RoomEntry.AutoRecord
        /// préserve : figurer dans la liste ne suffit PAS à être surveillé.
        /// </summary>
        private void OnAddRoomClick(object? sender, EventArgs e)
        {
            var url = urlTextBox.Text.Trim();

            // Meme controle que pour un enregistrement : une URL refusee par le
            // sandbox ne doit pas entrer dans une liste qui la rappellera toutes
            // les deux minutes.
            if (!UrlValidator.IsSafeUrl(url, AppConfig.Whitelist, AppConfig.Blacklist, out var motif))
            {
                Logger.Log($"URL refusée pour la liste des salons ({url}) : {motif}", LogLevel.ERROR);
                MessageBox.Show(this, Localization.Get("error.urlRejected"),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Une carte éphémère existe déjà (enregistrement lancé sans ajout) :
            // on la PROMEUT au lieu d'en créer une seconde pour le même salon.
            if (FindRoomRow(url) is { } existante)
            {
                if (!existante.Ephemeral)
                {
                    MessageBox.Show(this, Localization.Format("room.alreadyKnown", RoomNameFromUrl(url)),
                        Localization.Get("dialog.info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _rooms.Add(existante.Url);
                existante.Ephemeral = false;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] {Localization.Format("room.added", RoomNameFromUrl(url))}");
                return;
            }

            if (!_rooms.Add(url))
            {
                MessageBox.Show(this, Localization.Format("room.alreadyKnown", RoomNameFromUrl(url)),
                    Localization.Get("dialog.info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ligne = BuildRoomRow(url, ephemeral: false);
            _roomRows.Add(ligne);
            UpdateRoomsEmptyState();
            AppendLog($"[{DateTime.Now:HH:mm:ss}] {Localization.Format("room.added", RoomNameFromUrl(url))}");
        }

        /// <summary>
        /// Le message « aucun salon » n'est visible que quand la liste est vide.
        /// Une liste vide sans un mot est un écran cassé : rien ne dit s'il n'y a
        /// rien à montrer ou si le chargement a échoué.
        /// </summary>
        private void UpdateRoomsEmptyState()
        {
            roomsEmptyLabel.Visible = _roomRows.Count == 0;
        }

        private bool IsRecording(string url)
        {
            return FindRoomRow(url) is { Job: { } job } && job.Engine.State == DownloadState.Running;
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
            if (_watchTickRunning) return;

            // 97.0 — SEULS les salons dont l'interrupteur est armé sont sondés.
            // C'est la décision d'origine du mainteneur, préservée telle quelle :
            // figurer dans la liste ne suffit pas à être surveillé, sinon trente
            // favoris déclencheraient trente appels réseau toutes les deux
            // minutes. La copie est prise ici : la liste peut changer pendant le
            // passage, qui dure plusieurs secondes par salon.
            var aSonder = _roomRows.Where(r => r.AutoRecordFlag).ToList();
            if (aSonder.Count == 0) return;

            _watchTickRunning = true;
            try
            {
                foreach (var row in aSonder)
                {
                    if (IsDisposed || _watchTimer == null) return;
                    // Retirée entre-temps : sa carte n'existe plus.
                    if (!_roomRows.Contains(row)) continue;

                    // Deja en cours d'enregistrement : rien a controler, et
                    // surtout rien a redemarrer.
                    if (row.Job is { } encours && encours.Engine.State == DownloadState.Running) continue;

                    // L'enregistrement précédent est fini : on détache son
                    // résultat avant de sonder, sans quoi Resolve continuerait
                    // d'afficher « terminé » par-dessus un salon revenu en ligne
                    // — l'enregistrement prime sur le sondage, par conception.
                    if (row.Job != null) row.Job = null;

                    var status = await RoomStatusChecker.CheckAsync(
                        AppConfig.YtDlpPath, row.Url,
                        SafeMode.IsEnabled(SafeComponent.Cookies) ? AppConfig.CookiesFilePath : "",
                        SafeMode.IsEnabled(SafeComponent.Proxy) ? AppConfig.ProxyUrl : "");

                    // L'appel dure plusieurs secondes : la fenetre a pu etre
                    // fermee entre-temps.
                    if (IsDisposed || _watchTimer == null) return;
                    if (!_roomRows.Contains(row)) continue;

                    row.PollStatus = status;
                    row.Polled = true;
                    RefreshCard(row);

                    if (status == RoomStatus.NotFound)
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] Surveillance : {RoomNameFromUrl(row.Url)} n'existe pas — vérifie l'adresse.");

                    // SEUL Online declenche. Unknown (reseau coupe, salon banni)
                    // ne doit jamais lancer un enregistrement dans le vide.
                    if (status != RoomStatus.Online) continue;

                    AppendLog($"[{DateTime.Now:HH:mm:ss}] Surveillance : {RoomNameFromUrl(row.Url)} est en ligne, demarrage.");
                    ShowNotification(Localization.Get("watch.started.title"),
                        Localization.Format("watch.started.body", RoomNameFromUrl(row.Url)));
                    StartRecording(row.Url, interactive: false);
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

            // Nom de la source, qui sert AUSSI de base au nom de fichier de
            // sortie : c'est Platforms qui sait le tirer de chaque forme d'URL
            // (40.0), et qui le nettoie des caractères interdits en chemin.
            var roomName = Platforms.DisplayName(urlInput);

            // Safe Mode : un seul enregistrement a la fois quand le
            // multi-stream est desactive. Refus explicite plutot que silencieux.
            if (!SafeMode.IsEnabled(SafeComponent.MultiStream)
                && _roomRows.Any(r => r.Job is { } j && j.Engine.State == DownloadState.Running))
            {
                RefuseStart(interactive, Localization.Get("safe.multiStreamOff"), Localization.Get("dialog.info"));
                return;
            }

            if (_roomRows.Any(r => r.Job is { } j && j.RoomName == roomName && j.Engine.State == DownloadState.Running))
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

            // 97.0 — le salon a peut-être déjà sa carte : on lui ATTACHE le job
            // au lieu d'ouvrir une seconde ligne pour la même adresse, ce que
            // faisaient les trois anciens panneaux. Sinon, une carte éphémère,
            // qui n'entre pas dans la liste persistée : enregistrer une adresse
            // collée ne doit pas la garder pour toujours sans qu'on l'ait
            // demandé (l'interrupteur ou « + Ajouter » le font, eux).
            var row = FindRoomRow(urlInput);
            if (row == null)
            {
                row = BuildRoomRow(urlInput, ephemeral: true);
                _roomRows.Add(row);
                UpdateRoomsEmptyState();
            }
            AttachJob(row, job);

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

            AppendJobLog(job, "Démarrage de l'enregistrement...");

            try
            {
                StartEngine();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.Format("error.cannotStartDownload", ex.Message),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Le salon garde sa carte s'il est dans la liste : c'est
                // l'ENREGISTREMENT qui a échoué, pas le salon qui disparaît.
                // Seule une carte éphémère, créée à l'instant pour ce
                // démarrage-là, n'a plus de raison d'exister.
                row.Job = null;
                if (row.Ephemeral) RemoveRoomRow(row); else RefreshCard(row);
            }
        }

        private void OnStopAllClick(object? sender, EventArgs e)
        {
            foreach (var row in _roomRows.ToList())
            {
                if (row.Job is { } job && job.Engine.State == DownloadState.Running)
                    job.Engine.Stop();
            }
        }

        // 97.0 — OnAddFavoriteClick / OnRemoveFavoriteClick / OnLoadFavoriteClick
        // ont disparu avec le panneau Favoris. Leurs trois rôles sont repris par
        // la carte de salon : « + Ajouter » (OnAddRoomClick) fait entrer une
        // adresse dans la liste, la corbeille l'en retire, et « Charger » n'a
        // plus d'objet puisque chaque carte porte son propre bouton Démarrer —
        // recopier l'adresse dans le champ pour la relancer était un détour que
        // seul l'éclatement en trois panneaux imposait.

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

            _trayOpenItem.Text = L("tray.open");
            _traySettingsItem.Text = L("tray.settings");
            _trayCloseItem.Text = L("tray.close");

            grpRecord.Title = L("panel.record");
            urlLabel.Text = L("label.url");
            platformStrip.RefreshTooltip();
            startButton.Text = L("button.start");
            stopAllButton.Text = L("button.stopAll");
            addRoomButton.Text = L("button.addRoom");

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

            grpHistory.Title = L("panel.history");
            historyListView.Columns[0].Text = L("column.file");
            historyListView.Columns[1].Text = L("column.size");
            historyListView.Columns[2].Text = L("column.duration");
            historyListView.Columns[3].Text = L("column.date");
            refreshHistoryButton.Text = L("button.refresh");
            openHistoryFolderButton.Text = L("button.openFolder");
            openHistoryFileButton.Text = L("button.openFile");

            grpRooms.Title = L("panel.rooms");
            addRoomButton.Text = L("button.addRoom");
            roomsEmptyLabel.Text = L("room.empty");

            sideBar.SetLabel("streams", L("nav.streams"));
            sideBar.SetLabel("history", L("nav.history"));
            sideBar.SetLabel("settings", L("nav.settings"));
            sideBar.SetLabel("support", L("nav.support"));

            grpDonate.Title = L("panel.donate");
            sponsorButton.Text = L("button.sponsor");
            donateButton.Text = L("button.donate");
            websiteButton.Text = L("button.website");
            thanksButton.Text = L("button.thanks");
            donateLabel.Text = L("label.donate");

            grpLogs.Title = L("panel.logs");
            toggleLogsButton.Text = L(grpLogs.Visible ? "button.hideLogs" : "button.showLogs");

            // Les cartes se retraduisent entièrement : leur état est DÉRIVÉ à
            // chaque rafraîchissement, il n'y a donc pas de libellé mémorisé
            // qu'un changement de langue pourrait perdre — c'est précisément ce
            // que la clé rangée dans ListViewItem.Name protégeait avant 97.0.
            foreach (var row in _roomRows)
            {
                _cardTips.SetToolTip(row.OpenButton, L("job.open"));
                _cardTips.SetToolTip(row.RemoveButton, L("job.remove"));
                RefreshCard(row);
            }
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
        /// <summary>
        /// Repeint toute la fenêtre une fois qu'elle est redevenue opaque.
        /// Le délai laisse Windows défaire la composition en couche avant
        /// qu'on redessine ; 80 ms restent invisibles à l'œil.
        /// </summary>
        private void RepeindreApresFondu()
        {
            var minuteur = new System.Windows.Forms.Timer { Interval = 80 };
            minuteur.Tick += (s, e) =>
            {
                minuteur.Stop();
                minuteur.Dispose();
                if (IsDisposed) return;
                Invalidate(true);
                Update();
            };
            minuteur.Start();
        }

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

                    // Repeindre TOUT une fois le fondu terminé.
                    //
                    // **ATTÉNUATION, PAS CORRECTIF PROUVÉ.** Le mainteneur voit
                    // au premier lancement des encoches sombres aux coins
                    // arrondis des ThemedButton, sur tous les onglets, qui
                    // disparaissent dès que la souris survole le bouton
                    // concerné — et UNIQUEMENT celui-là. **Le défaut n'a PAS pu
                    // être reproduit** sur la machine de développement, ni par
                    // DrawToBitmap ni par CopyFromScreen sur la fenêtre réelle
                    // avec le fondu actif. Différence de mise à l'échelle ou de
                    // composition, non élucidée.
                    //
                    // L'hypothèse : une fenêtre dont Opacity < 1 est une fenêtre
                    // EN COUCHE, dont Windows compose le rendu avec l'alpha
                    // courant. Un contrôle peint pendant le fondu verrait son
                    // résultat figé à l'opacité de l'instant, et les zones plus
                    // jamais repeintes le resteraient — ce qui expliquerait le
                    // « ça part au survol ». Un repaint complet après le fondu
                    // coûte une frame et couvre ce cas s'il est le bon.
                    //
                    // **Si le défaut persiste chez le mainteneur, la piste
                    // suivante est de supprimer le fondu d'ouverture** (9.2) :
                    // sans Opacity < 1, il n'y a plus de fenêtre en couche, donc
                    // plus de composition partielle possible. C'est un agrément
                    // décoratif contre un défaut visible à chaque lancement.
                    if (!IsDisposed) RepeindreApresFondu();

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
        /// <summary>
        /// Affiche une section et pose la mise en page de ses panneaux (97.0).
        ///
        /// **Chaque vue calcule sa propre hauteur naturelle** et la donne à son
        /// AutoScrollMinSize : une section courte ne doit pas hériter de la
        /// barre de défilement d'une autre. C'est ce que l'ancien ApplyUiMode
        /// faisait pour la page unique, appliqué ici par section.
        /// </summary>
        /// <summary>
        /// Affiche ou masque le panneau des logs (97.0) et retient le choix.
        /// Persisté parce que l'inverse serait une corvée : quelqu'un qui suit
        /// un problème devrait rouvrir le panneau à chaque lancement.
        /// </summary>
        private void BasculerLogs(bool visible)
        {
            grpLogs.Visible = visible;
            toggleLogsButton.Text = Localization.Get(visible ? "button.hideLogs" : "button.showLogs", _currentLanguage);
            _settings.ShowLogs = visible;
            SettingsManager.Save(_settings);
            LayoutCurrentView();
        }

        private string _currentViewKey = "streams";

        private (string Key, Panel Vue)[] Vues => new[]
        {
            ("streams", viewStreams), ("history", viewHistory),
            ("settings", viewSettings), ("support", viewSupport),
        };

        private void ShowView(string key)
        {
            _currentViewKey = key;
            foreach (var (k, vue) in Vues) vue.Visible = k == key;
            LayoutCurrentView();
        }

        /// <summary>
        /// Positionne ET DIMENSIONNE les panneaux de la vue affichée (97.0).
        ///
        /// **Les largeurs sont calculées, pas ancrées**, et ce n'est pas un
        /// choix de style. L'ancrage Left|Right mémorise la marge droite au
        /// moment où il est posé ; à cet instant la vue a encore sa taille par
        /// DÉFAUT (200 px), donc la marge enregistrée était négative et les
        /// panneaux débordaient de ~470 px à toutes les tailles — bords droits
        /// invisibles même en élargissant la fenêtre. C'est la variante
        /// « parent pas encore dimensionné » du piège Anchor noté en bas de ce
        /// fichier : le parent existe, ce qui suffit à tromper.
        /// </summary>
        private void LayoutCurrentView()
        {
            const int marge = 12;
            const int sectionGap = 20;
            const int largeurMini = 520;

            var vue = Array.Find(Vues, v => v.Key == _currentViewKey).Vue ?? viewStreams;
            var largeur = Math.Max(largeurMini, vue.ClientSize.Width - 2 * marge);

            int hauteur;
            switch (_currentViewKey)
            {
                case "history":
                    // L'historique OCCUPE sa section : c'est la seule chose
                    // qu'elle contient, la laisser à 170 px de haut au milieu
                    // d'un écran vide n'avait aucun sens.
                    grpHistory.Bounds = new Rectangle(marge, marge, largeur,
                        Math.Max(220, vue.ClientSize.Height - 2 * marge));
                    hauteur = grpHistory.Bottom + marge;
                    break;

                case "settings":
                    var y = marge;
                    foreach (var b in new[] { paramsButton, tutorialButton, checkUpdateButton, legalButton, diagnosticButton, reportBugButton })
                    {
                        b.Bounds = new Rectangle(marge, y, Math.Min(280, largeur), 30);
                        y += 38;
                    }
                    hauteur = y + marge;
                    break;

                case "support":
                    grpDonate.Bounds = new Rectangle(marge, marge, largeur, grpDonate.Height);
                    hauteur = grpDonate.Bottom + marge;
                    break;

                default:
                    grpRecord.Bounds = new Rectangle(marge, marge, largeur, 218);
                    grpRooms.Bounds = new Rectangle(marge, grpRecord.Bottom + sectionGap, largeur, grpRooms.Height);
                    toggleLogsButton.Location = new Point(marge, grpRooms.Bottom + sectionGap);
                    if (grpLogs.Visible)
                    {
                        grpLogs.Bounds = new Rectangle(marge, toggleLogsButton.Bottom + 10, largeur, grpLogs.Height);
                        hauteur = grpLogs.Bottom + marge;
                    }
                    else
                    {
                        hauteur = toggleLogsButton.Bottom + marge;
                    }
                    break;
            }

            vue.AutoScrollMinSize = new Size(largeurMini + 2 * marge, hauteur);

            // Repeindre TOUTE la vue, enfants compris.
            //
            // Sans ça, le premier affichage d'une section laissait un fragment
            // du contour de bouton à son ancienne position : ces contrôles
            // viennent d'être déplacés et redimensionnés, et ThemedButton peint
            // ses coins arrondis avec la couleur du parent — un parent qui n'a
            // pas repeint laisse donc l'ancien coin visible. Le défaut
            // disparaissait en quittant puis revenant sur la section, ce qui
            // forçait le repaint : signature exacte d'un rendu non invalidé.
            vue.Invalidate(true);
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
            var runningJobs = _roomRows.Count(r => r.Job is { } j && j.Engine.State == DownloadState.Running);
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

            foreach (var row in _roomRows)
            {
                StopRecordingTimer(row);
                row.Job?.Engine.Stop();
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
        /// <summary>
        /// 102.0 — le bouton ouvre désormais une fenêtre au lieu d'aller droit
        /// sur GitHub. Motif : ouvrir un navigateur sur un formulaire d'issue
        /// exige un compte, et quelqu'un qui n'en a pas — ou qui ne veut pas
        /// s'en créer un pour signaler un plantage — repartait sans rien dire.
        /// Le chemin GitHub reste proposé dans la fenêtre.
        /// </summary>
        private void OnReportBugClick(object? sender, EventArgs e)
        {
            using var dialog = new ReportForm(
                _currentTheme, _currentLanguage, CurrentVersion, _advancedMode, OpenGitHubIssue);
            dialog.ShowDialog(this);
        }

        private void OpenGitHubIssue()
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

            tutorialButton = new ThemedButton { Text = "Guide de démarrage", Location = new Point(12, topBarRow2Y), Size = new Size(190, topBarButtonHeight) };
            tutorialButton.Click += (s, e) => ShowTutorial();

            checkUpdateButton = new ThemedButton { Text = "Rechercher une mise à jour", Location = new Point(212, topBarRow2Y), Size = new Size(215, topBarButtonHeight) };
            checkUpdateButton.Click += OnCheckUpdateClick;

            reportBugButton = new ThemedButton { Location = new Point(437, topBarRow2Y), Size = new Size(160, topBarButtonHeight) };
            reportBugButton.Click += OnReportBugClick;

            diagnosticButton = new ThemedButton { Location = new Point(12, topBarRow3Y), Size = new Size(160, topBarButtonHeight) };
            diagnosticButton.Click += (s, e) => new DiagnosticForm(_currentTheme).ShowDialog(this);

            // --- Panel : Enregistrement ---
            // Ancrage Left+Right (fenêtre redimensionnable, v1.7.0) : le panneau
            // et son contenu large (URL, dossier, proxy) suivent la largeur de
            // la fenêtre au lieu de rester figés à 660px avec du vide autour
            // quand on l'élargit — seuls les boutons de droite sont ancrés Right
            // seul, pour rester collés au bord plutôt que de s'étirer eux-mêmes.
            grpRecord = new RoundedGroupPanel { Title = "Enregistrement", Location = new Point(12, 75), Size = new Size(660, 272) };
            urlLabel = new Label { Text = "URL Chaturbate :", Location = new Point(12, 25), AutoSize = true };
            // 103.0 — les plateformes prises en charge, à droite de l'intitulé
            // du champ : c'est là que se pose la question « qu'est-ce que je
            // peux coller ici ? ». Posé à x=150, après le libellé le plus long
            // des deux langues ("URL du live :" / "Stream URL:").
            platformStrip = new PlatformStrip { Location = new Point(150, 22) };
            urlTextBox = new TextBox { Location = new Point(12, 48), Size = new Size(360, 24) };
            startButton = new ThemedButton { Text = "Démarrer", Location = new Point(382, 46), Size = new Size(120, 28) };
            stopAllButton = new ThemedButton { Text = "Tout arrêter", Location = new Point(512, 46), Size = new Size(136, 28) };
            addRoomButton = new ThemedButton { Text = "+ Ajouter", Location = new Point(445, 78), Size = new Size(198, 24) };

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
            addRoomButton.Click += OnAddRoomClick;

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
                urlLabel, platformStrip, urlTextBox, startButton, stopAllButton, addRoomButton,
                advancedOptionsPanel
            });
            urlTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            startButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            stopAllButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addRoomButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            advancedOptionsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // --- Panel : Mes salons (97.0 étape 2c) ---
            //
            // **UN panneau à la place de trois.** Enregistrements en cours,
            // Favoris et Surveillance décrivaient les mêmes salons sous trois
            // angles : un salon pouvait figurer aux trois endroits à la fois,
            // avec trois vérités possibles sur ce qu'il faisait.
            //
            // Hauteur 314 (contre 154 + 130 + 130 = 414 pour les trois panneaux,
            // séparateurs compris). Une carte compacte fait 60 px PLUS 8 de
            // marge basse, soit 68 : il en faut donc 272 pour quatre salons, pas
            // 264 — mesuré sur la première capture, où l'ascenseur vertical
            // apparaissait dès la quatrième carte. 276 laisse 4 px de garde.
            grpRooms = new RoundedGroupPanel { Title = "Mes salons", Location = new Point(12, 320), Size = new Size(660, 314) };
            roomsListPanel = new FlowLayoutPanel
            {
                Location = new Point(12, 22),
                Size = new Size(636, 276),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
            };
            // Posé AVANT les cartes et sous elles dans l'ordre Z : il n'est
            // visible que lorsque la liste est vide (UpdateRoomsEmptyState).
            roomsEmptyLabel = new Label
            {
                Text = "Aucun salon pour l'instant. Colle une adresse ci-dessus, puis « + Ajouter ».",
                Location = new Point(16, 34),
                Size = new Size(600, 40),
                AutoSize = false,
                Visible = false,
            };
            ThemeManager.SetTextRole(roomsEmptyLabel, TextRole.Caption);

            grpRooms.Controls.Add(roomsEmptyLabel);
            grpRooms.Controls.Add(roomsListPanel);
            roomsListPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // Les cartes suivent la largeur de la liste. Elles ne peuvent pas
            // s'ancrer elles-mêmes : un FlowLayoutPanel ignore l'ancrage de ses
            // enfants, et l'ancrage mémoriserait de toute façon une marge prise
            // avant que la vue ait sa taille définitive (piège d'Anchor, bas de
            // CLAUDE.md, déjà payé deux fois).
            roomsListPanel.SizeChanged += (s, e) => ResizeRoomCards();

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

            // Les panneaux Favoris et Surveillance ont disparu ici (97.0 étape
            // 2c) : leur contenu vit dans grpRooms, une carte par salon. Le
            // pictogramme de plateforme que portait la ListView surveillée
            // (103.0) est désormais dessiné par la carte elle-même, à partir de
            // Platforms.Badge — donc sans ImageList, et sans le piège de
            // réalisation qui allait avec.

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
            //
            // 168 depuis 104.0 : le bouton "Remerciements" occupe une quatrième
            // ligne dans la colonne de droite (y=128..154), et la garde de 10 px
            // sous le dernier contrôle est conservée (168 - 4 - 154).
            grpDonate = new RoundedGroupPanel { Title = "Soutenir le projet", Location = new Point(12, 606), Size = new Size(660, 168) };
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

            // 104.0 — remerciements aux donateurs. Placé dans ce panneau et
            // non dans la barre du haut : c'est au moment où quelqu'un envisage
            // de donner qu'il a du sens de voir qui l'a déjà fait, et le mettre
            // ailleurs en aurait fait une fenêtre que personne n'ouvre.
            // Sur la colonne de droite, sous la rangée de partage : la colonne
            // de gauche est celle des trois actions "donner", celle-ci est
            // occupée par ce qui relève de la communauté.
            thanksButton = new ThemedButton { Text = "Remerciements", Location = new Point(340, 128), Size = new Size(316, 26) };
            thanksButton.Click += (s, e) =>
            {
                using var dialog = new SupportersForm(_currentTheme, _currentLanguage);
                dialog.ShowDialog(this);
            };

            sponsorButton.Click += OnSponsorClick;
            donateButton.Click += OnDonateClick;
            websiteButton.Click += OnWebsiteClick;

            grpDonate.Controls.AddRange(new Control[]
            {
                sponsorButton, donateButton, websiteButton, qrPictureBox, donateLabel,
                shareXButton, shareRedditButton, githubButton, thanksButton,
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
            // Les rôles Danger des anciens « Supprimer favori » et « Ne plus
            // surveiller » vivent désormais sur la corbeille de chaque carte,
            // posée par BuildRoomRow.

            // Intitulés de champ : un cran en dessous du contenu qu'ils
            // annoncent. Sans ça « Qualité source : » pèse autant que la valeur
            // choisie, et l'œil n'a aucun ordre de lecture.
            foreach (var caption in new Control[] { urlLabel, qualityLabel, codecLabel, formatLabel, durationLabel, donateLabel })
                ThemeManager.SetTextRole(caption, TextRole.Caption);

            // --- 97.0 : charpente en barre latérale + vues ---
            //
            // Les panneaux ne changent NI de contenu NI de dimensions à cette
            // étape : ils sont seulement reparentés dans la vue qui les
            // accueille. C'est ce qui permet de refondre la structure sans
            // toucher au fonctionnement, et de vérifier l'un avant l'autre.
            //
            // Le mode simple/avancé disparaît : il n'existait que pour MASQUER
            // la moitié d'un écran unique, problème que la navigation supprime.
            viewStreams = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false };
            viewHistory = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false };
            viewSettings = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false };
            viewSupport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false };

            // 97.0 — les logs sont MASQUES par defaut : ce panneau occupait un
            // tiers de la page pour une information que personne ne lit tant
            // que rien ne va mal. Le choix est persiste, sans quoi il faudrait
            // le rouvrir a chaque lancement.
            toggleLogsButton = new ThemedButton { Size = new Size(200, 26) };
            toggleLogsButton.Click += (s, e) => BasculerLogs(!grpLogs.Visible);
            viewStreams.Controls.AddRange(new Control[] { grpRecord, grpRooms, toggleLogsButton, grpLogs });
            viewHistory.Controls.Add(grpHistory);
            viewSettings.Controls.AddRange(new Control[] { paramsButton, tutorialButton, checkUpdateButton, reportBugButton, diagnosticButton, legalButton });
            viewSupport.Controls.Add(grpDonate);

            sideBar = new SideBar { Dock = DockStyle.Left };
            sideBar.AddEntry("streams", "camera");
            sideBar.AddEntry("history", "folder");
            sideBar.AddEntry("settings", "sliders");
            sideBar.AddEntry("support", "heart");
            sideBar.SelectionChanged += (s, e) => ShowView(sideBar.SelectedKey);

            contentPanel.Controls.AddRange(new Control[] { viewStreams, viewHistory, viewSettings, viewSupport });
            // La barre AVANT la zone de contenu dans l'ordre d'ajout : en
            // docking WinForms, le dernier ajouté est le plus proche du bord,
            // et Fill doit occuper ce qui reste APRÈS le Left.
            Controls.Add(contentPanel);
            Controls.Add(sideBar);

            // FORCER la création du handle de la ListView de l'historique, bien
            // qu'elle vive dans une vue masquée. Lire `.Handle` le crée quelle
            // que soit la visibilité, ce que CreateControl() ne fait pas.
            //
            // Ce n'est PLUS ce qui protège du piège ImageList de 103.0 : ce
            // garde-fou-là vit désormais dans RefreshHistoryAsync, juste avant
            // la boucle d'ajout, parce que dépendre d'une ligne lointaine
            // exécutée plus tôt est une garantie qu'un chemin d'appel nouveau
            // peut contourner sans bruit (v1.35.1, deux plantages en production).
            // Il reste utile pour le reste : une ListView sans handle ne
            // mesure pas ses colonnes, et ThemedListView.Refresh n'aurait rien
            // à ajuster.
            //
            // La ListView de surveillance a disparu avec son panneau (97.0
            // étape 2c) : la carte de salon dessine son pictogramme elle-même,
            // sans ImageList, donc sans ce piège.
            _ = historyListView.Handle;

            // PAS D'ANCRAGE Left|Right sur ces panneaux : leurs largeurs sont
            // calculées par LayoutCurrentView. L'ancrage mémorise la marge
            // droite à l'instant où il est posé, et la vue a alors sa taille par
            // défaut — d'où une marge négative et des panneaux qui débordaient
            // de ~470 px à toutes les tailles. Voir le commentaire de
            // LayoutCurrentView.
            foreach (var vue in Vues)
                vue.Vue.SizeChanged += (s, e) => { if (vue.Vue.Visible) LayoutCurrentView(); };

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
