using System.Windows;
using System;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.ViewModels;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Fenêtre principale. Elle ne contient aucune logique : la navigation vit
    /// dans <see cref="MainViewModel"/>, l'apparence dans les dictionnaires de
    /// <c>Themes\</c>.
    ///
    /// C'est le contraste avec `MainForm.cs` et ses 3 527 lignes, où mise en
    /// page, thème, traduction, enregistrement et surveillance cohabitaient
    /// dans le même fichier.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TrayIcon? _zoneDeNotification;

        public MainWindow()
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);
            DataContext = new MainViewModel();

            // 120.0 — jamais montré à qui a déjà acheté StreamRecorderPro.
            // App.Premium est chargé au démarrage (voir App.xaml.cs), avant
            // la construction de cette fenêtre : pas besoin d'attendre Loaded.
            BoutonPremium.Visibility = App.Premium.IsLicensed ? Visibility.Collapsed : Visibility.Visible;

            // La fenetre principale EST la duree de vie de l'application ici :
            // sa fermeture doit arreter la surveillance, sinon la boucle
            // continue de sonder dans un processus qui n'affiche plus rien.
            // La zone de notification est construite APRES le DataContext :
            // elle s'abonne a Closing sur cette fenetre, et doit pouvoir la
            // masquer plutot que la fermer des le premier clic sur la croix.
            _zoneDeNotification = new TrayIcon(this);

            Closed += (s, e) =>
            {
                _zoneDeNotification?.Dispose();
                (DataContext as System.IDisposable)?.Dispose();
            };

            // Les nouveautés s'affichent APRÈS l'ouverture, pas dans le
            // constructeur : une fenêtre modale posée avant que la principale
            // existe apparaît seule, sans parent, et se retrouve derrière elle
            // au premier clic.
            Loaded += (s, e) =>
            {
                AfficherLeGuide();
                AfficherLesNouveautes();
            };
        }

        /// <summary>
        /// Montre le guide au PREMIER lancement seulement.
        ///
        /// `UserSettings.HasSeenTutorial` existait depuis le WinForms et
        /// PERSONNE ne le lisait dans la version WPF : le guide ne s'affichait
        /// jamais. Le drapeau est pose APRES l'affichage, comme
        /// `LastSeenVersion` : si la fenetre echoue a s'ouvrir, le guide
        /// reviendra au prochain lancement plutot que d'etre perdu.
        ///
        /// Avant les nouveautes : quelqu'un qui decouvre l'application n'a que
        /// faire d'un journal des versions, et les deux modales enchainees dans
        /// l'autre sens donneraient le changelog en premier.
        /// </summary>
        private void AfficherLeGuide()
        {
            try
            {
                var reglages = SettingsManager.Load();
                if (reglages.HasSeenTutorial) return;

                Logger.Log("Premier lancement : ouverture du guide de demarrage.");
                new TutorialWindow { Owner = this }.ShowDialog();
                Logger.Log("Guide de demarrage referme.");

                reglages.HasSeenTutorial = true;
                SettingsManager.Save(reglages);
            }
            catch (Exception ex)
            {
                Logger.Log($"Affichage du guide impossible : {ex.Message}", LogLevel.WARN);
            }
        }

        /// <summary>
        /// Montre les nouveautés une seule fois par version installée.
        ///
        /// La comparaison porte sur <c>UserSettings.LastSeenVersion</c>, écrit
        /// APRÈS l'affichage : si la fenêtre échoue à s'ouvrir, le réglage
        /// n'est pas consommé et les nouveautés seront proposées au prochain
        /// lancement, plutôt que perdues en silence.
        /// </summary>
        private void AfficherLesNouveautes()
        {
            try
            {
                var version = typeof(App).Assembly.GetName().Version;
                var courante = version is null
                    ? "0.0.0"
                    : $"{version.Major}.{version.Minor}.{version.Build}";

                var reglages = SettingsManager.Load();
                if (string.Equals(reglages.LastSeenVersion, courante, StringComparison.Ordinal)) return;

                // Première installation : `LastSeenVersion` est nul, et dérouler
                // TOUT l'historique depuis la 1.0 accueillerait un nouvel
                // arrivant par cinquante versions qu'il n'a jamais connues. On
                // note la version et on se tait.
                if (!string.IsNullOrEmpty(reglages.LastSeenVersion))
                {
                    Logger.Log($"Nouveautes depuis {reglages.LastSeenVersion} : ouverture.");
                    new ChangelogWindow(reglages.LastSeenVersion, courante) { Owner = this }.ShowDialog();
                    Logger.Log("Nouveautes refermees.");
                }

                reglages.LastSeenVersion = courante;
                SettingsManager.Save(reglages);
            }
            catch (Exception ex)
            {
                // Un changelog qui ne s'affiche pas ne doit jamais empêcher
                // l'application de démarrer.
                Logger.Log($"Affichage des nouveautés impossible : {ex.Message}", LogLevel.WARN);
            }
        }
    }
}
