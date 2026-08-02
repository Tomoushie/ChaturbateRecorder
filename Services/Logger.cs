using System;
using System.IO;

namespace ChaturbateRecorderApp.Services
{
    public enum LogLevel { INFO, WARN, ERROR }

    /// <summary>
    /// Logger statique : écrit dans un fichier de session horodaté et notifie
    /// les abonnés (ex: MainForm) pour affichage en direct dans l'UI.
    /// </summary>
    public static class Logger
    {
        public static event Action<string, LogLevel>? OnLogLine;

        private static string SessionLogFile =>
            Path.Combine(Config.AppConfig.LogDir, $"session-{DateTime.Now:yyyy-MM-dd}.log");

        public static void Log(string message, LogLevel level = LogLevel.INFO)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            System.Diagnostics.Debug.WriteLine(line);

            try
            {
                var dir = Path.GetDirectoryName(SessionLogFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.AppendAllText(SessionLogFile, line + Environment.NewLine);
            }
            catch
            {
                // La journalisation ne doit jamais faire planter l'application.
            }

            OnLogLine?.Invoke(line, level);
        }
    }
}
