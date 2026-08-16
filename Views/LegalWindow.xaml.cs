using System.Windows;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// La fenêtre n'affiche que du texte fixe.
    /// </summary>
    public partial class LegalWindow : Window
    {
        public LegalWindow()
        {
            InitializeComponent();

            // Le texte vient de la table de chaines (`legal.body`), traduit FR
            // et EN depuis 98.0. Le recopier dans le XAML en aurait fait une
            // seconde version a maintenir — et sur un texte juridique, deux
            // versions qui divergent posent un vrai probleme.
            Corps.Text = ChaturbateRecorderApp.UI.Localization.Get("legal.body");
            Title = ChaturbateRecorderApp.UI.Localization.Get("window.legal");
        }

        private void Fermer_Click(object sender, RoutedEventArgs e) => Close();
    }
}