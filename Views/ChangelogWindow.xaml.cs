using System.Windows;
using ChaturbateRecorderApp.ViewModels;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// La fenetre ne contient aucune logique, elle met en forme ce que le modele de vue lui donne.
    /// </summary>
    public partial class ChangelogWindow : Window
    {
        public ChangelogWindow(string? depuisVersion, string jusquaVersion)
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);
            DataContext = new ChangelogViewModel(depuisVersion, jusquaVersion);
        }

        private void Fermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}