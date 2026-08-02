using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ChaturbateRecorderApp.Services
{
    public enum LogLevel { INFO, WARN, ERROR }

    /// <summary>
    /// Logger statique : écrit un objet JSON par ligne (JSONL) dans un fichier
    /// de session horodaté, et notifie les abonnés (ex: MainForm) pour
    /// affichage en direct dans l'UI. La source (nom du fichier appelant) est
    /// déduite automatiquement via CallerFilePath — aucun site d'appel
    /// existant n'a besoin de la fournir explicitement.
    /// </summary>
    public static class Logger
    {
        public static event Action<string, LogLevel>? OnLogLine;

        private static string SessionLogFile =>
            Path.Combine(Config.AppConfig.LogDir, $"session-{DateTime.Now:yyyy-MM-dd}.jsonl");

        public static void Log(string message, LogLevel level = LogLevel.INFO,
            [CallerFilePath] string callerFilePath = "")
        {
            var source = string.IsNullOrEmpty(callerFilePath)
                ? "Unknown"
                : Path.GetFileNameWithoutExtension(callerFilePath);

            var timestamp = DateTime.Now;
            var readableLine = $"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] [{source}] {message}";
            System.Diagnostics.Debug.WriteLine(readableLine);

            try
            {
                var sessionLogFile = SessionLogFile;
                var dir = Path.GetDirectoryName(sessionLogFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                LogRotationManager.RotateIfTooLarge(sessionLogFile, Config.AppConfig.LogMaxFileSizeBytes);

                var entry = new LogEntry
                {
                    Timestamp = timestamp.ToString("o"),
                    Level = level.ToString(),
                    Source = source,
                    Message = message,
                };
                File.AppendAllText(sessionLogFile, JsonSerializer.Serialize(entry) + Environment.NewLine);
            }
            catch
            {
                // La journalisation ne doit jamais faire planter l'application.
            }

            OnLogLine?.Invoke(readableLine, level);
        }

        private sealed class LogEntry
        {
            public string Timestamp { get; set; } = "";
            public string Level { get; set; } = "";
            public string Source { get; set; } = "";
            public string Message { get; set; } = "";
        }
    }
}
