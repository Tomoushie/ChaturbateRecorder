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

    }
}