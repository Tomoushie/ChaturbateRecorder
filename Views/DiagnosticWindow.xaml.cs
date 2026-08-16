using System;
using System.Windows;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.Views;

namespace ChaturbateRecorderApp.Views
{
    public partial class DiagnosticWindow : Window
    {
        public DiagnosticWindow()
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);
            Rapport.Text = DiagnosticReport.Statique(null);
            _ = ChargerAsync();
        }

        private async System.Threading.Tasks.Task ChargerAsync()
        {
            try
            {
                Rapport.Text = await DiagnosticReport.CompletAsync(null);
            }
            catch (Exception ex)
            {
                Logger.Log($"Diagnostic complet impossible : {ex.Message}", LogLevel.WARN);
            }
        }

        private void Copier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(Rapport.Text);
            }
            catch (Exception ex)
            {
                Logger.Log($"Échec de la copie du rapport : {ex.Message}", LogLevel.WARN);
            }
        }

        private void Actualiser_Click(object sender, RoutedEventArgs e) => _ = ChargerAsync();

        private void Fermer_Click(object sender, RoutedEventArgs e) => Close();
    }
}