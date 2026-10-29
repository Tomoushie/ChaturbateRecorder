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
        /// VRAI depuis le 25-08 : page itch.io vérifiée publique et achetable
        /// ($10 USD ou plus), les trois fichiers confirmés présents
        /// (StreamRecorderPro.dll, LISEZMOI-ACHETEUR.txt, README-BUYER.txt).
        /// Passée par plusieurs états avant ça — brouillon (404 public),
        /// publique mais vide, puis Restricted (page inaccessible même avec
        /// le lien, itch.io exige alors une autorisation explicite par
        /// personne — pas adapté ici) — voir la mémoire du projet pour le
        /// détail. Ne repasser à `false` que si la page redevient
        /// indisponible.
        /// </summary>
        private const bool CarteItchIoDisponible = true;

        public PremiumUpgradeWindow()
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);

            CarteItchIo.Visibility = CarteItchIoDisponible ? Visibility.Visible : Visibility.Collapsed;

            // À transmettre avec N'IMPORTE QUEL moyen de paiement (120.0) :
            // la licence est désormais liée à cette installation. Voir
            // Services/MachineId.cs pour pourquoi il est généré ICI et non
            // dans StreamRecorderPro (doit exister même sans le composant).
            TexteIdMachine.Text = MachineId.Obtenir();

            // Les IBAN/BIC ne sont pas dans le XAML : ce sont des données,
            // pas des libellés (voir le commentaire du fichier XAML).
            TexteIban1.Text = FormaterIban(AppConfig.PremiumIban1);
            TexteIban2.Text = FormaterIban(AppConfig.PremiumIban2);
            TexteBic2.Text = AppConfig.PremiumIban2Bic;

            ImageQrIban1.Source = QrPour(AppConfig.PremiumIban1, bic: null);
            ImageQrIban2.Source = QrPour(AppConfig.PremiumIban2, AppConfig.PremiumIban2Bic);

            // Le service d'activation est une commodité : sans endpoint
            // configuré (build local, service retiré), le bouton disparaît
            // plutôt que d'offrir un chemin qui échoue.
            BoutonActiverEnLigne.Visibility = string.IsNullOrWhiteSpace(AppConfig.ActivationEndpoint)
                ? Visibility.Collapsed
                : Visibility.Visible;

            // La fenêtre peut s'ouvrir alors que la licence vient d'être
            // installée (bouton « Activer » de la bannière, resté ouvert) :
            // ne pas proposer une seconde installation comme si de rien
            // n'était.
            if (App.Premium.IsLicensed)
            {
                BoutonInstallerLicence.IsEnabled = false;
                BoutonActiverEnLigne.IsEnabled = false;
                AfficherInstallation(
                    ChaturbateRecorderApp.UI.Localization.Format("premium.installOk", App.Premium.LicensedTo),
                    succes: true);
            }
        }

        /// <summary>
        /// Choisit un licence.key et l'installe au bon endroit. Le service
        /// sauvegarde l'ancienne licence et la restaure si la nouvelle est
        /// refusée — voir Services/LicenceInstaller.cs.
        /// </summary>
        private void InstallerLicence_Click(object sender, RoutedEventArgs e)
        {
            var dialogue = new Microsoft.Win32.OpenFileDialog
            {
                Title = ChaturbateRecorderApp.UI.Localization.Get("premium.choisirLicence"),
                Filter = ChaturbateRecorderApp.UI.Localization.Get("premium.filtreLicence"),
                CheckFileExists = true,
                Multiselect = false,
                RestoreDirectory = true,
            };

            if (dialogue.ShowDialog(this) != true) return;

            var resultat = LicenceInstaller.Installer(
                dialogue.FileName,
                () =>
                {
                    App.Premium.Reload();
                    return App.Premium.IsLicensed;
                });

            switch (resultat.Issue)
            {
                case IssueInstallation.Aucune:
                    BoutonInstallerLicence.IsEnabled = false;
                    AfficherInstallation(
                        ChaturbateRecorderApp.UI.Localization.Format("premium.installOk", App.Premium.LicensedTo),
                        succes: true);
                    break;

                case IssueInstallation.LicenceRefusee:
                    Logger.Log($"Premium : licence choisie refusée — {App.Premium.LicenceProblem}.", LogLevel.WARN);
                    AfficherInstallation(
                        ChaturbateRecorderApp.UI.Localization.Get(
                            App.Premium.IsMachineMismatch ? "premium.installMachine" : "premium.installRefusee"),
                        succes: false);
                    break;

                case IssueInstallation.FichierSource:
                    Logger.Log($"Premium : fichier de licence inutilisable — {resultat.Detail}.", LogLevel.WARN);
                    AfficherInstallation(
                        ChaturbateRecorderApp.UI.Localization.Get("premium.installSource"),
                        succes: false);
                    break;

                default:
                    Logger.Log($"Premium : installation de licence impossible — {resultat.Detail}.", LogLevel.ERROR);
                    AfficherInstallation(
                        ChaturbateRecorderApp.UI.Localization.Get("premium.installErreur"),
                        succes: false);
                    break;
            }
        }

        /// <summary>
        /// Ouvre l'activation en ligne. La fenêtre installe elle-même la
        /// licence ; au retour, on reflète l'état pour ne pas laisser un
        /// bouton actif sur un produit déjà activé.
        /// </summary>
        private void ActiverEnLigne_Click(object sender, RoutedEventArgs e)
        {
            new OnlineActivationWindow { Owner = this }.ShowDialog();

            if (!App.Premium.IsLicensed) return;

            BoutonInstallerLicence.IsEnabled = false;
            BoutonActiverEnLigne.IsEnabled = false;
            AfficherInstallation(
                ChaturbateRecorderApp.UI.Localization.Format("premium.installOk", App.Premium.LicensedTo),
                succes: true);
        }

        /// <summary>
        /// Ouvre le client mail avec l'identifiant d'installation déjà rempli.
        /// C'est le chemin de secours quand aucune licence n'a été reçue, ou
        /// pour demander un transfert après un changement d'ordinateur.
        /// </summary>
        private void DemanderLicence_Click(object sender, RoutedEventArgs e)
        {
            var identifiant = MachineId.Obtenir();
            var sujet = ChaturbateRecorderApp.UI.Localization.Get("premium.requestSubject");
            var corps = ChaturbateRecorderApp.UI.Localization.Format(
                "premium.requestBody",
                string.IsNullOrWhiteSpace(identifiant) ? "(indisponible)" : identifiant);

            Ouvrir($"mailto:{AppConfig.SupportEmail}"
                   + $"?subject={Uri.EscapeDataString(sujet)}"
                   + $"&body={Uri.EscapeDataString(corps)}");
        }

        private void AfficherInstallation(string texte, bool succes)
        {
            TexteInstallation.Text = texte;
            TexteInstallation.Visibility = Visibility.Visible;
            // SetResourceReference et non une couleur figée : le thème peut
            // changer pendant que la fenêtre est ouverte.
            TexteInstallation.SetResourceReference(
                System.Windows.Controls.TextBlock.ForegroundProperty,
                succes ? "Brush.Success" : "Brush.Danger");
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

        private void CopierIdMachine_Click(object sender, RoutedEventArgs e) =>
            CopierEtConfirmer(MachineId.Obtenir());

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
