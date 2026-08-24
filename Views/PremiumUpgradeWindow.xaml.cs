using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
// PAS de `using ChaturbateRecorderApp.UI;` : System.Windows.Localization est
// homonyme de UI.Localization (piège n°7 du projet) — les deux membres
// utilisés d'ici sont donc qualifiés en toutes lettres plutôt qu'importés.

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Présentation de Stream Recorder Pro (120.0) et de ses moyens de
    /// paiement — itch.io, IBAN/IBAN+BIC à poids égal, jamais PayPal ni
    /// GitHub Sponsors (voir <see cref="AppConfig.PremiumIban1"/>).
    /// </summary>
    public partial class PremiumUpgradeWindow : Window
    {
        /// <summary>
        /// FAUX tant que Tom n'a pas confirmé un fichier réellement déposé
        /// sur la page itch.io (25-08 : la page était en brouillon, puis
        /// publique le même jour mais toujours vide — « There doesn't appear
        /// to be anything here… », message standard d'itch.io pour une page
        /// sans contenu). Tant que ce drapeau reste faux, la carte itch.io ne
        /// s'affiche pas : personne ne doit pouvoir payer pour rien.
        /// À repasser à `true` UNE FOIS LE FICHIER CONFIRMÉ, pas avant.
        /// </summary>
        private const bool CarteItchIoDisponible = false;

        public PremiumUpgradeWindow()
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);

            CarteItchIo.Visibility = CarteItchIoDisponible ? Visibility.Visible : Visibility.Collapsed;

            // Les IBAN/BIC ne sont pas dans le XAML : ce sont des données,
            // pas des libellés (voir le commentaire du fichier XAML).
            TexteIban1.Text = FormaterIban(AppConfig.PremiumIban1);
            TexteIban2.Text = FormaterIban(AppConfig.PremiumIban2);
            TexteBic2.Text = AppConfig.PremiumIban2Bic;

            ImageQrIban1.Source = QrPour(AppConfig.PremiumIban1, bic: null);
            ImageQrIban2.Source = QrPour(AppConfig.PremiumIban2, AppConfig.PremiumIban2Bic);
        }

        /// <summary>
        /// Groupé par 4, comme un IBAN s'affiche partout (relevé, virement
        /// papier) — plus facile à comparer visuellement qu'un bloc de 16
        /// caractères collés.
        /// </summary>
        private static string FormaterIban(string iban)
        {
            var brut = iban.Replace(" ", "");
            var groupes = new System.Collections.Generic.List<string>();
            for (var i = 0; i < brut.Length; i += 4)
                groupes.Add(brut.Substring(i, System.Math.Min(4, brut.Length - i)));
            return string.Join(" ", groupes);
        }

        private static BitmapImage QrPour(string iban, string? bic)
        {
            var png = EpcQrGenerator.GenererPng(AppConfig.PremiumBeneficiaire, iban, bic, "StreamRecorderPro");
            using var flux = new MemoryStream(png);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = flux;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private void ItchIo_Click(object sender, RoutedEventArgs e) => Ouvrir(AppConfig.ProUrl);

        private void CopierIban1_Click(object sender, RoutedEventArgs e) =>
            CopierEtConfirmer(AppConfig.PremiumIban1);

        private void CopierIban2_Click(object sender, RoutedEventArgs e) =>
            CopierEtConfirmer(AppConfig.PremiumIban2);

        private void CopierEtConfirmer(string iban)
        {
            try
            {
                Clipboard.SetText(iban);
                TexteStatut.Text = ChaturbateRecorderApp.UI.Localization.Get("premium.copied");
                // Message éphémère : redevient vide après 2 s, comme les
                // autres confirmations ponctuelles de l'application.
                _ = EffacerStatutApresDelaiAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Presse-papiers indisponible : {ex.Message}", LogLevel.WARN);
            }
        }

        private async Task EffacerStatutApresDelaiAsync()
        {
            await Task.Delay(2000);
            TexteStatut.Text = "";
        }

        private static void Ouvrir(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogLevel.WARN);
            }
        }

        private void Fermer_Click(object sender, RoutedEventArgs e) => Close();
    }
}
