using System;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Security;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            if (!WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir))
            {
                MessageBox.Show(
                    "Cette application ne peut pas s'exécuter depuis cet emplacement " +
                    "(partage réseau, dossier temporaire/éphémère ou dossier compressé NTFS). " +
                    "Déplace l'exécutable vers un dossier local standard.",
                    "Emplacement non autorisé", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
