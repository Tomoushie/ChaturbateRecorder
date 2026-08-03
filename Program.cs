using System;
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
            if (!WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir))
            {
                // MainForm n'existe pas encore à ce stade : langue relue depuis
                // les paramètres persistés, comme CrashReporter le fait.
                var language = SettingsManager.Load().Language == "en" ? AppLanguage.English : AppLanguage.French;
                MessageBox.Show(
                    Localization.Get("err.unauthorizedLocation.body", language),
                    Localization.Get("err.unauthorizedLocation.title", language), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
