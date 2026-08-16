using System.Windows.Controls;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Vue ne contient aucune logique
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Ouvre le diagnostic. Le geste vit dans la VUE et non dans le modele
        /// de vue : ouvrir une fenetre est de l'interface, et un modele de vue
        /// qui instancie des Window n'est plus testable sans application.
        /// </summary>
        private void Diagnostic_Click(object sender, System.Windows.RoutedEventArgs e) =>
            new DiagnosticWindow { Owner = System.Windows.Window.GetWindow(this) }.ShowDialog();


        private void Signalement_Click(object sender, System.Windows.RoutedEventArgs e) =>
            new ReportWindow { Owner = System.Windows.Window.GetWindow(this) }.ShowDialog();


        private void Legalite_Click(object sender, System.Windows.RoutedEventArgs e) =>
            new LegalWindow { Owner = System.Windows.Window.GetWindow(this) }.ShowDialog();


        /// <summary>
        /// Le guide s'ouvre aussi a la demande, et pas seulement au premier
        /// lancement : son propre texte annonce qu'on peut le rouvrir a tout
        /// moment. Sans ce bouton, il mentirait.
        /// </summary>
        private void Guide_Click(object sender, System.Windows.RoutedEventArgs e) =>
            new TutorialWindow { Owner = System.Windows.Window.GetWindow(this) }.ShowDialog();

    }
}