// Views/MainWindow.xaml.cs
using System.Windows;

namespace ChaturbateRecorderApp.Views
{
    public partial class MainWindow : Window
    {
        // Le DataContext est injecté via App.xaml.cs, donc pas besoin de le créer ici
        // ou de l'assigner manuellement sauf si vous avez une logique spécifique à la fenêtre.
        public MainWindow()
        {
            InitializeComponent();
            // Le DataContext est automatiquement défini par WPF via l'injection dans le constructeur de la fenêtre via App.xaml.cs
        }
    }
}