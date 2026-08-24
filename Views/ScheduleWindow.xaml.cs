using System;
using System.Linq;
using System.Windows;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;

// System.Windows apporte son propre Localization (attributs XAML), homonyme
// de la table de chaînes du projet : alias obligatoire, sinon CS0104.
using Localization = ChaturbateRecorderApp.UI.Localization;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Planification horaire d'un salon (premium). Petite fenêtre modale,
    /// même famille que <see cref="DiagnosticWindow"/> : pas de ViewModel
    /// séparé, l'écran est trop simple pour le justifier.
    ///
    /// Granularité à l'HEURE, volontairement : le sondage de surveillance a
    /// lui-même une cadence de l'ordre de la minute (`WatchIntervalSeconds`),
    /// une précision plus fine n'apporterait rien de vérifiable à l'écran.
    /// </summary>
    public partial class ScheduleWindow : Window
    {
        /// <summary>Résultat après un <c>ShowDialog() == true</c>.</summary>
        public bool Active { get; private set; }
        public int DebutMinutes { get; private set; }
        public int FinMinutes { get; private set; }

        public ScheduleWindow(string nomSalon, bool active, int debutMinutes, int finMinutes)
        {
            InitializeComponent();
            WindowChrome.Suivre(this);

            Title = Localization.Format("schedule.title", nomSalon);

            var heures = Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToList();
            Debut.ItemsSource = heures;
            Fin.ItemsSource = heures;

            // -1 (jamais configuré) retombe sur une fenêtre de soirée
            // raisonnable par défaut, plutôt que sur 00:00 — poser « minuit »
            // comme point de départ silencieux aurait fait croire à un choix.
            Debut.SelectedIndex = debutMinutes >= 0 ? debutMinutes / 60 : 20;
            Fin.SelectedIndex = finMinutes >= 0 ? finMinutes / 60 : 23;
            ActiverCase.IsChecked = active;

            Enregistrer.Click += (s, e) =>
            {
                Active = ActiverCase.IsChecked == true;
                DebutMinutes = Debut.SelectedIndex * 60;
                FinMinutes = Fin.SelectedIndex * 60;
                DialogResult = true;
            };
            Annuler.Click += (s, e) => DialogResult = false;

            Avertissement.Visibility = App.Premium.IsLicensed ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
