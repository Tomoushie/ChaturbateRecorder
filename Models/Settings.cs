// Models/AppSettings.cs
//
// FICHIER DU SQUELETTE, À RÉCONCILIER : il double `UserSettings` de
// `Services/SettingsManager.cs`, qui est la classe réellement persistée dans
// settings.json et lue par l'application publiée. Deux modèles de réglages
// dans le même assembly finiraient par diverger, et c'est celui-ci qui est
// faux : il range les données sous %AppData%\StreamRecorderPro alors que
// l'application existante écrit dans %LocalAppData%\ChaturbateRecorder —
// les réglages de tous les utilisateurs actuels deviendraient invisibles.
// Tranché avec la couche ViewModels.
using System.IO;                    // manquait : Path était introuvable (CS0103)
using System;                       // idem pour Environment
using System.Text.Json.Serialization;

namespace ChaturbateRecorderApp.Models
{
    public class AppSettings
    {
        // --- Dossiers ---
        public string CaptureDir { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Valeur par défaut
        public string LogDir { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StreamRecorderPro", "Logs");
        public string ToolsDir { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools"); // Ex: pour yt-dlp, FFmpeg
        public string TempDir { get; set; } = Path.GetTempPath();
        public string ThumbnailsDir { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StreamRecorderPro", "Thumbnails");

        // --- Chemins des binaires ---
        public string YtDlpPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "yt-dlp.exe");
        public string FFmpegPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ffmpeg.exe");
        public string FFprobePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ffprobe.exe");

        // --- Options de connexion ---
        public string CookiesFilePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cookies.txt");
        public string ProxyUrl { get; set; } = "";

        // --- Options de surveillance ---
        public int WatchIntervalSeconds { get; set; } = 60; // Ex: 60 secondes
        public bool AutoReconnectDefault { get; set; } = true;

        // --- Options d'enregistrement ---
        public string DefaultFormat { get; set; } = "best"; // Ex: pour yt-dlp
        public string DefaultContainer { get; set; } = "mkv"; // Ex: mkv, mp4

        // --- Options d'interface ---
        public string Theme { get; set; } = "Light"; // Ex: Light, Dark
        public string Language { get; set; } = "en"; // Ex: fr, en
        // Note : Le mode simple/avancé est supprimé (selon CLAUDE.md), donc pas d'option ici.

        // --- Options de journalisation ---
        public bool ShowLogs { get; set; } = false; // Panneau des logs visible par défaut

        // --- Options diverses ---
        public int LogMaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 Mo
        public int YtDlpWatchdogTimeoutSeconds { get; set; } = 180; // 3 minutes

        // Ajoutez d'autres propriétés si nécessaire
    }
}