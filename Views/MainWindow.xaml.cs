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
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            // La fenetre principale EST la duree de vie de l'application ici :
            // sa fermeture doit arreter la surveillance, sinon la boucle
            // continue de sonder dans un processus qui n'affiche plus rien.
            Closed += (s, e) => (DataContext as System.IDisposable)?.Dispose();

            // Les nouveautés s'affichent APRÈS l'ouverture, pas dans le
            // constructeur : une fenêtre modale posée avant que la principale
            // existe apparaît seule, sans parent, et se retrouve derrière elle
            // au premier clic.
            Loaded += (s, e) => AfficherLesNouveautes();
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
                    new ChangelogWindow(reglages.LastSeenVersion, courante) { Owner = this }.ShowDialog();
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
