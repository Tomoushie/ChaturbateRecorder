using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Capture les exceptions non gérées (thread UI via Application.ThreadException,
    /// et tout le reste via AppDomain.UnhandledException — thread pool, Task non
    /// observée, etc.), les journalise dans un fichier de crash dédié (distinct des
    /// logs JSON habituels), puis affiche un dialogue proposant d'ouvrir le dossier
    /// de logs et/ou de redémarrer proprement.
    /// </summary>
    public static class CrashReporter
    {
        private static readonly string CrashDir = Path.Combine(AppConfig.LogDir, "crashes");

        public static void Install()
        {
            Application.ThreadException += (s, e) => Handle(e.Exception, isTerminating: false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Handle(e.ExceptionObject as Exception ?? new Exception("Exception non-.NET inconnue : " + e.ExceptionObject), e.IsTerminating);
        }

        private static void Handle(Exception ex, bool isTerminating)
        {
            string? crashFile = null;
            try
            {
                crashFile = WriteCrashLog(ex);
            }
            catch (Exception logEx)
            {
                // Le crash reporter lui-même ne doit jamais faire plus de dégâts
                // que le crash d'origine — best effort uniquement, jamais lancé.
                Debug.WriteLine($"Échec de l'écriture du rapport de crash : {logEx.Message}");
            }

            try
            {
                Logger.Log($"Exception non gérée ({(isTerminating ? "fatale" : "récupérée")}) : {ex.GetType().Name} — {ex.Message}", LogLevel.ERROR);
            }
            catch
            {
                // idem : ne jamais laisser le logging normal aggraver la situation.
            }

            try
            {
                // Pas de référence à l'instance MainForm ici (classe statique,
                // appelée depuis n'importe quel thread) : la langue est relue
                // depuis les paramètres persistés plutôt que depuis l'état en
                // mémoire, comme MainForm le fait elle-même à la construction.
                var language = SettingsManager.Load().Language == "en" ? AppLanguage.English : AppLanguage.French;
                using var form = new CrashReportForm(ex, crashFile, isTerminating, language);
                form.ShowDialog();
            }
            catch
            {
                // Si même l'affichage du dialogue échoue (UI dans un état trop
                // corrompu pour créer une nouvelle fenêtre), on ne bloque pas
                // plus longtemps le processus de fermeture/poursuite.
            }
        }

        private static string WriteCrashLog(Exception ex)
        {
            Directory.CreateDirectory(CrashDir);
            var path = Path.Combine(CrashDir, $"crash-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

            var sb = new StringBuilder();
            sb.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Version de l'application : {typeof(CrashReporter).Assembly.GetName().Version}");
            sb.AppendLine($"Système : {Environment.OSVersion} ({(Environment.Is64BitProcess ? "64" : "32")} bits)");
            sb.AppendLine();
            sb.AppendLine(ex.ToString());

            File.WriteAllText(path, sb.ToString());
            return path;
        }

        /// <summary>
        /// Relance l'exécutable puis termine le process courant. Utilisé aussi
        /// bien après une exception récupérable (l'utilisateur choisit de
        /// redémarrer par précaution) que juste avant qu'un crash fatal ne
        /// termine le process de toute façon.
        /// </summary>
        public static void RestartApplication()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Échec du redémarrage automatique : {ex.Message}");
            }
            finally
            {
                Environment.Exit(1);
            }
        }
    }
}
