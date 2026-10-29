using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ChaturbateRecorderApp.Config;

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
        // Propriete et non champ static readonly : AppConfig.LogDir peut changer
        // au demarrage (repli quand le dossier configure est injoignable, voir
        // MainForm.EnsureDirectoryOrFallback). Un champ fige a l'initialisation
        // du type aurait garde l'ancien chemin — precisement celui qui vient
        // d'echouer — et le rapport de crash n'aurait toujours pas pu s'ecrire.
        private static string CrashDir => Path.Combine(AppConfig.LogDir, "crashes");

        /// <summary>
        /// Posé par la couche d'interface au démarrage. Le service ne connaît
        /// plus la fenêtre qu'il affiche : en WinForms il instanciait
        /// `CrashReportForm` directement, ce qui l'aurait fait dépendre d'un
        /// type WPF ici — et de WinForms là-bas. Le point d'accroche laisse la
        /// capture d'exception utilisable sans interface du tout, donc testable.
        /// Nul = le rapport est écrit sur disque et journalisé, sans dialogue.
        /// </summary>
        public static Action<Exception, string?, bool>? ShowCrashDialog { get; set; }

        /// <summary>
        /// N'installe QUE le filet AppDomain. En WinForms, `Install` posait
        /// aussi `Application.ThreadException` pour le thread d'interface ;
        /// l'équivalent WPF est `DispatcherUnhandledException`, qui se déclare
        /// dans App.xaml et non par abonnement — d'où `HandleDispatcher`
        /// ci-dessous, que le gestionnaire d'App.xaml.cs appelle.
        /// </summary>
        public static void Install()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Handle(e.ExceptionObject as Exception ?? new Exception("Exception non-.NET inconnue : " + e.ExceptionObject), e.IsTerminating);
        }

        /// <summary>
        /// Pendant WPF de `Application.ThreadException` : à appeler depuis le
        /// gestionnaire `DispatcherUnhandledException` d'App.xaml.cs, qui doit
        /// ensuite poser `e.Handled = true` s'il veut que l'application survive.
        /// </summary>
        public static void HandleDispatcher(Exception ex) => Handle(ex, isTerminating: false);

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
                ShowCrashDialog?.Invoke(ex, crashFile, isTerminating);
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
                    // `Application.ExecutablePath` était du WinForms. Le pendant
                    // sans dépendance de bureau est `Environment.ProcessPath`,
                    // qui rend bien le chemin de l'apphost — y compris pour la
                    // variante portable publiée en PublishSingleFile.
                    FileName = Environment.ProcessPath ?? string.Empty,
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
