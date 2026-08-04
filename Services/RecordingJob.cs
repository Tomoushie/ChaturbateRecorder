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

        // --- Minuteur (87.0) ---
        // Durée maximale d'enregistrement en minutes, 0 = illimité. Choisi par
        // enregistrement au démarrage, comme la qualité/le codec/le format.
        //
        // Le décompte part du premier démarrage et n'est PAS remis à zéro par
        // une reconnexion automatique : « arrêter après 2 h » désigne 2 h de
        // temps écoulé, pas 2 h par tentative — sinon une room instable
        // enregistrerait indéfiniment.
        public int TimerMinutes { get; set; }

        /// <summary>
        /// Instant d'arrêt programmé, en UTC. Null tant que l'enregistrement
        /// n'a pas démarré, ou si aucun minuteur n'est demandé.
        /// </summary>
        public DateTime? StopAtUtc { get; set; }

        // Nom de fichier (sans extension) de la tentative en cours, régénéré à
        // chaque (re)démarrage (voir StartEngine dans MainForm) : identifie la
        // sortie de CE job sans ambiguïté, contrairement à une recherche par
        // RoomName seul qui peut confondre deux enregistrements différents du
        // même salon (miniature/réencodage attribués au mauvais fichier).
        public string? OutputBaseName { get; set; }
    }
}
