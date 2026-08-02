namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Un enregistrement en cours (ou terminé) parmi potentiellement plusieurs
    /// simultanés. Chaque job a son propre DownloadEngine (donc son propre
    /// process yt-dlp) — permet d'enregistrer plusieurs lives à la fois sans
    /// ouvrir plusieurs instances de l'application.
    /// </summary>
    public class RecordingJob
    {
        public required string RoomName { get; init; }
        public required string SourceUrl { get; init; }
        public required string CaptureDir { get; init; }
        public required string CodecChoice { get; init; }
        public required string ContainerExt { get; init; }
        public DownloadEngine Engine { get; } = new();

        // --- Reconnexion automatique (4.2) ---
        public bool AutoReconnectEnabled { get; set; }
        public int ReconnectAttempt;

        // Nom de fichier (sans extension) de la tentative en cours, régénéré à
        // chaque (re)démarrage (voir StartEngine dans MainForm) : identifie la
        // sortie de CE job sans ambiguïté, contrairement à une recherche par
        // RoomName seul qui peut confondre deux enregistrements différents du
        // même salon (miniature/réencodage attribués au mauvais fichier).
        public string? OutputBaseName { get; set; }
    }
}
