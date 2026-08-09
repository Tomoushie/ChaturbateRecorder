using System;
using System.Globalization;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using SentinelGuard;

namespace ChaturbateRecorderApp
{
    internal static class Program
    {
        /// <summary>Noms partagés entre les deux instances (93.0).</summary>
        internal const string SingleInstanceMutexName = @"Local\ChaturbateRecorder.SingleInstance";
        internal const string ShowWindowEventName = @"Local\ChaturbateRecorder.ShowWindow";

        [STAThread]
        private static void Main()
        {
            // La langue choisie par l'utilisateur vit dans settings.json, à côté
            // de l'exe — donc dans le dossier que le contrôle ci-dessous n'a
            // justement pas encore validé. Ce contrôle reste volontairement la
            // toute première chose exécutée : pour ce seul message on se rabat
            // sur la langue de l'OS plutôt que de lire un fichier depuis un
            // emplacement encore non vérifié. MainForm réaligne ensuite
            // Localization.Current sur le réglage persisté.
            Localization.Current =
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase)
                    ? AppLanguage.French
                    : AppLanguage.English;

            // Le motif du refus est journalisé ICI depuis la fusion (36.0 suite) :
            // les validateurs de SentinelGuard ne journalisent rien eux-mêmes,
            // ils rendent la raison à l'appelant. Sans cette ligne, un refus de
            // démarrage ne laisserait aucune trace exploitable.
            if (!WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir, out var locationReason))
            {
                Logger.Log($"Emplacement d'exécution refusé : {locationReason}", LogLevel.ERROR);
                MessageBox.Show(
                    Localization.Get("error.unauthorizedLocation"),
                    Localization.Get("error.unauthorizedLocation.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 93.0 — instance unique. Lancer l'exe plusieurs fois empilait
            // autant d'icônes dans la barre des tâches et dans la zone de
            // notification, chaque instance croyant gérer seule les mêmes
            // dossiers et le même settings.json.
            //
            // Portée "Local\" (session Windows courante) et non "Global\" :
            // deux utilisateurs connectés simultanément sur la même machine
            // doivent pouvoir utiliser l'application chacun de leur côté.
            //
            // Un simple refus de démarrer ne suffirait pas : depuis 19.0 la
            // fenêtre se masque dans la zone de notification au lieu de fermer,
            // donc l'utilisateur qui relance l'exe cherche justement à la
            // retrouver. La seconde instance signale l'évènement nommé que la
            // première surveille (voir MainForm.ListenForSecondInstance), puis
            // se termine sans rien afficher.
            using var singleInstance = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);
            if (!isFirstInstance)
            {
                try
                {
                    if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out var showEvent))
                    {
                        showEvent.Set();
                        showEvent.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Instance déjà lancée, réveil impossible : {ex.Message}", LogLevel.WARN);
                }
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Crash Reporter (2.1) : capture les exceptions non gérées, thread UI
            // (ThreadException) et hors UI (AppDomain.UnhandledException), avant
            // de démarrer la boucle de messages.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            CrashReporter.Install();

            Application.Run(new MainForm());
        }
    }
}
