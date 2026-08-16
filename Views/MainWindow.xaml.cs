using System.Windows;
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
        }
    }
}
