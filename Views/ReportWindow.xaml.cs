using System.Windows;
using ChaturbateRecorderApp.ViewModels;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Logique de fenêtre pour la fenêtre ReportWindow.
    /// La fenêtre ne contient aucune logique.
    /// </summary>
    public partial class ReportWindow : Window
    {
        public ReportWindow()
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);
            DataContext = new ReportViewModel();
        }

        private void Fermer_Click(object sender, RoutedEventArgs e) => Close();
    }
}