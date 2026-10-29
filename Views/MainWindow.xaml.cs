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

        /// <summary>
        /// Bouton flottant « Passer à Premium », gardé pour pouvoir le RETIRER
        /// quand une licence est installée à chaud (2.2.0). Voir
        /// <see cref="MettreAJourLicence"/>.
        /// </summary>
        private PremiumButton? _boutonPremium;

        public MainWindow()
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);
            DataContext = new MainViewModel();

            // 120.0 — jamais montré à qui a déjà acheté StreamRecorderPro.
            // App.Premium est chargé au démarrage (voir App.xaml.cs), avant
            // la construction de cette fenêtre : pas besoin d'attendre Loaded.
            //
            // CONSTRUIT ICI, JAMAIS EN XAML STATIQUE : un `Visibility.Collapsed`
            // posé après coup sur un élément déjà déclaré ne suffit pas — voir
            // le commentaire XAML retiré, l'animation ambiante du bouton
            // plantait à l'exécution (trouvé par Tom, InvalidOperationException
            // sur le nom "Halo" introuvable) parce qu'un Control Collapsed
            // n'applique jamais son ControlTemplate mais son EventTrigger
            // "Loaded" se déclenche quand même. Ne jamais le construire du
            // tout élimine le problème à la racine plutôt que de le contourner.
            if (!App.Premium.IsLicensed)
            {
                // Qualifiés en toutes lettres : MainWindow hérite d'une
                // propriété d'INSTANCE du même nom (FrameworkElement.HorizontalAlignment),
                // qui masquerait sinon le type énuméré à cet endroit précis.
                var boutonPremium = new PremiumButton
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 16, 16),
                };
                // Ligne 1 : la ligne 0 est réservée à la bannière de licence.
                System.Windows.Controls.Grid.SetRow(boutonPremium, 1);
                GrilleRacine.Children.Add(boutonPremium);
                _boutonPremium = boutonPremium;
            }

            // Une licence installée depuis la fenêtre Premium doit se voir
            // TOUT DE SUITE : retirer le bouton d'achat et la bannière, sans
            // redémarrer. Voir PremiumBridge.LicenceChanged.
            App.Premium.LicenceChanged += MettreAJourLicence;
            MettreAJourLicence();

            // La fenetre principale EST la duree de vie de l'application ici :
            // sa fermeture doit arreter la surveillance, sinon la boucle
            // continue de sonder dans un processus qui n'affiche plus rien.
            // La zone de notification est construite APRES le DataContext :
            // elle s'abonne a Closing sur cette fenetre, et doit pouvoir la
            // masquer plutot que la fermer des le premier clic sur la croix.
            _zoneDeNotification = new TrayIcon(this);

            Closed += (s, e) =>
            {
                App.Premium.LicenceChanged -= MettreAJourLicence;
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
        /// Met l'interface en accord avec l'état de licence : retire le bouton
        /// d'achat quand elle est acceptée, et n'affiche la bannière que dans
        /// le cas INJUSTE — un fichier présent mais refusé.
        ///
        /// **Pas de bannière pour qui n'a jamais acheté.** Le bouton flottant
        /// suffit, et un rappel permanent dans une application gratuite se lit
        /// comme du harcèlement. En revanche, quelqu'un qui a payé, posé son
        /// fichier et vu le premium rester absent n'a aujourd'hui AUCUN signe
        /// explicite : c'est ce silence qui fait croire à une arnaque.
        /// </summary>
        private void MettreAJourLicence()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(MettreAJourLicence);
                return;
            }

            if (App.Premium.IsLicensed)
            {
                if (_boutonPremium != null)
                {
                    GrilleRacine.Children.Remove(_boutonPremium);
                    _boutonPremium = null;
                }
                BanniereLicence.Visibility = Visibility.Collapsed;
                return;
            }

            if (!App.Premium.LicenceFilePresent)
            {
                BanniereLicence.Visibility = Visibility.Collapsed;
                return;
            }

            TexteBanniereLicence.Text = ChaturbateRecorderApp.UI.Localization.Get(
                App.Premium.IsMachineMismatch ? "premium.bannerMachine" : "premium.bannerRefused");
            BanniereLicence.Visibility = Visibility.Visible;
        }

        private void BanniereLicence_Click(object sender, RoutedEventArgs e)
        {
            new PremiumUpgradeWindow { Owner = this }.ShowDialog();
            // L'événement a normalement déjà rafraîchi l'état ; cet appel
            // couvre le cas d'une fenêtre fermée sans installation.
            MettreAJourLicence();
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
