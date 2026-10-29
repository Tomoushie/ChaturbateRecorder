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
        // `init` et non seulement `get` : le coordinateur peut ainsi recevoir
        // une FABRIQUE de moteurs et poser autre chose qu'un vrai yt-dlp. La
        // valeur par defaut reste un moteur reel, donc rien ne change pour
        // l'application.
        public DownloadEngine Engine { get; init; } = new();

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

        // --- Clips et marqueurs (v2.3) ---

        /// <summary>
        /// Début de la TENTATIVE en cours, en UTC. Remis à zéro à chaque
        /// (re)démarrage : un marqueur posé pendant la tentative 2 ne doit pas
        /// être appliqué au fichier de la tentative 1, qui est un autre fichier.
        /// </summary>
        public DateTime? AttemptStartedAtUtc { get; set; }

        /// <summary>
        /// Positions (secondes depuis le début de la tentative) marquées par
        /// l'utilisateur. Vidées à chaque nouvelle tentative, après export.
        /// </summary>
        public System.Collections.Generic.List<double> Markers { get; } = new();
    }
}
