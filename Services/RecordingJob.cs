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
    }
}
