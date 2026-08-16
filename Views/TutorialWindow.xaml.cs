using System.Windows;
using ChaturbateRecorderApp.ViewModels;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// La fenêtre ne contient aucune logique ; le modèle de vue SIGNALE la fin du guide et c'est la fenêtre qui se ferme, pour que le modèle reste testable sans application.
    /// </summary>
    public partial class TutorialWindow : Window
    {
        public TutorialWindow()
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);
            var vm = new TutorialViewModel();
            vm.Termine += Close;
            DataContext = vm;
        }
    }
}