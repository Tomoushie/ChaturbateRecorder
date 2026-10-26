using System.Windows.Controls;
namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Vue minimal pour l'affichage des streams. N'contient aucune logique métier.
    /// Tout le traitement est effectué dans le ViewModel et dans les dictionnaires de thèmes.
    /// </summary>
    public partial class StreamsView : UserControl
    {
        public StreamsView()
        {
            InitializeComponent();
        }
    }
}