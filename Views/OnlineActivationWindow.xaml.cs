using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
// PAS de `using ChaturbateRecorderApp.UI;` : System.Windows.Localization est
// homonyme (piège n°7 du projet). Les appels sont qualifiés en toutes lettres.

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Activation en ligne (2.2.0) : clé de téléchargement itch.io → licence
    /// signée → installation immédiate, sans redémarrage.
    ///
    /// La fenêtre ne fait que l'interface : la requête vit dans
    /// <see cref="OnlineActivation"/>, l'écriture et la restauration dans
    /// <see cref="LicenceInstaller"/>.
    /// </summary>
    public partial class OnlineActivationWindow : Window
    {
        private readonly CancellationTokenSource _annulation = new();
        private bool _occupe;
        private bool _ferme;

        public OnlineActivationWindow()
        {
            InitializeComponent();
            ChaturbateRecorderApp.UI.WindowChrome.Suivre(this);

            ChampIdMachine.Text = MachineId.Obtenir();

            Loaded += (_, _) => ChampCle.Focus();
            Closed += (_, _) =>
            {
                _ferme = true;
                _annulation.Cancel();
                _annulation.Dispose();
            };
        }

        private async void Activer_Click(object sender, RoutedEventArgs e)
        {
            if (_occupe) return;

            var cle = ChampCle.Text.Trim();
            if (cle.Length == 0)
            {
                Afficher(ChaturbateRecorderApp.UI.Localization.Get("premium.onlineKeyRequired"), succes: false);
                ChampCle.Focus();
                return;
            }

            _occupe = true;
            BoutonActiver.IsEnabled = false;
            Afficher(ChaturbateRecorderApp.UI.Localization.Get("premium.onlineActivating"), succes: true);

            try
            {
                var resultat = await OnlineActivation.ActiverAsync(
                    cle, ChampIdMachine.Text.Trim(), _annulation.Token);

                if (_ferme) return;

                switch (resultat.Issue)
                {
                    case IssueActivation.Aucune:
                        InstallerLaLicence(resultat);
                        break;

                    case IssueActivation.Refusee:
                        Logger.Log($"Activation en ligne refusée : {resultat.Code} ({resultat.Detail}).", LogLevel.WARN);
                        Afficher(ChaturbateRecorderApp.UI.Localization.Get(
                            OnlineActivation.CleErreur(resultat.Code)), succes: false);
                        break;

                    case IssueActivation.Reseau:
                        Logger.Log($"Activation en ligne : réseau — {resultat.Detail}.", LogLevel.WARN);
                        Afficher(ChaturbateRecorderApp.UI.Localization.Get("premium.onlineNetwork"), succes: false);
                        break;

                    case IssueActivation.Desactive:
                    case IssueActivation.Serveur:
                    case IssueActivation.ReponseInvalide:
                    default:
                        Logger.Log($"Activation en ligne indisponible : {resultat.Issue} ({resultat.Detail}).", LogLevel.WARN);
                        Afficher(ChaturbateRecorderApp.UI.Localization.Get("premium.onlineUnavailable"), succes: false);
                        break;
                }
            }
            finally
            {
                _occupe = false;
                if (!_ferme) BoutonActiver.IsEnabled = true;
            }
        }

        /// <summary>
        /// Écrit la licence reçue et la fait relire au composant. Le service
        /// <see cref="LicenceInstaller"/> sauvegarde l'ancienne et la restaure
        /// si la nouvelle est refusée — même garantie que l'installation
        /// manuelle.
        /// </summary>
        private void InstallerLaLicence(ResultatActivation resultat)
        {
            if (string.IsNullOrWhiteSpace(resultat.LicenceJson))
            {
                Afficher(ChaturbateRecorderApp.UI.Localization.Get("premium.onlineUnavailable"), succes: false);
                return;
            }

            var octets = Encoding.UTF8.GetBytes(resultat.LicenceJson);
            var installation = LicenceInstaller.InstallerContenu(
                octets,
                () =>
                {
                    App.Premium.Reload();
                    return App.Premium.IsLicensed;
                });

            if (installation.Succes)
            {
                var nom = App.Premium.LicensedTo;
                if (string.IsNullOrWhiteSpace(nom)) nom = resultat.Nom;

                BoutonActiver.IsEnabled = false;
                ChampCle.IsEnabled = false;
                Afficher(ChaturbateRecorderApp.UI.Localization.Format("premium.installOk", nom), succes: true);
                return;
            }

            Logger.Log($"Activation en ligne : licence reçue mais non installée — "
                       + $"{installation.Issue} ({installation.Detail}), "
                       + $"composant : {App.Premium.LicenceProblem}.", LogLevel.WARN);

            var cleMessage = installation.Issue == IssueInstallation.LicenceRefusee
                ? (App.Premium.IsMachineMismatch ? "premium.installMachine" : "premium.installRefusee")
                : installation.Issue == IssueInstallation.FichierSource
                    ? "premium.installSource"
                    : "premium.installErreur";

            Afficher(ChaturbateRecorderApp.UI.Localization.Get(cleMessage), succes: false);
        }

        private void CopierId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ChampIdMachine.Text);
                Afficher(ChaturbateRecorderApp.UI.Localization.Get("premium.copied"), succes: true);
            }
            catch (Exception ex)
            {
                Logger.Log($"Presse-papiers indisponible : {ex.Message}", LogLevel.WARN);
            }
        }

        private void OuvrirPage_Click(object sender, RoutedEventArgs e)
        {
            var url = (AppConfig.ActivationPageUrl ?? "").Trim();
            if (url.Length == 0)
            {
                Afficher(ChaturbateRecorderApp.UI.Localization.Get("premium.onlineUnavailable"), succes: false);
                return;
            }

            var identifiant = ChampIdMachine.Text.Trim();
            if (identifiant.Length > 0)
            {
                url += (url.Contains('?') ? "&" : "?") + "id=" + Uri.EscapeDataString(identifiant);
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log($"Ouverture de la page d'activation impossible : {ex.Message}", LogLevel.WARN);
                Afficher(ChaturbateRecorderApp.UI.Localization.Get("premium.onlineUnavailable"), succes: false);
            }
        }

        private void Afficher(string texte, bool succes)
        {
            TexteStatut.Text = texte;
            TexteStatut.Visibility = Visibility.Visible;
            TexteStatut.SetResourceReference(
                System.Windows.Controls.TextBlock.ForegroundProperty,
                succes ? "Brush.Success" : "Brush.Danger");
        }

        private void Fermer_Click(object sender, RoutedEventArgs e) => Close();
    }
}
