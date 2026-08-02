using System;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Security;

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
            Application.Run(new MainForm());
        }
    }
}
