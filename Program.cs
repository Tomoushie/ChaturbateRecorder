using System;
using System.Globalization;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Security;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp
{
    internal static class Program
    {
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

            if (!WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir))
            {
                MessageBox.Show(
                    Localization.Get("error.unauthorizedLocation"),
                    Localization.Get("error.unauthorizedLocation.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
