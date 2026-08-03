using System.Collections.Generic;

namespace ChaturbateRecorderApp.UI
{
    public enum AppLanguage { French, English }

    /// <summary>
    /// Traductions de l'UI (20.0, étendu au-delà des libellés fixes) :
    /// labels/boutons/en-têtes/ComboBox d'origine, plus messages d'erreur et
    /// de confirmation, notifications de la zone de notification, panneau
    /// Logs visible à l'écran, guide de démarrage (TutorialForm), Crash
    /// Reporter et Diagnostic. Reste explicitement hors périmètre :
    /// Config/Changelog.cs (historique versionné, coût de maintenance
    /// continu disproportionné) et les fichiers de logs sur disque
    /// (Services/Logger.cs — jamais montrés directement à l'utilisateur).
    /// Les valeurs contenant "{0}"/"{1}"... sont destinées à string.Format.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, (string Fr, string En)> Strings = new()
        {
            ["theme.label"] = ("Thème :", "Theme:"),
            ["theme.light"] = ("Clair", "Light"),
            ["theme.dark"] = ("Sombre", "Dark"),
            ["language.label"] = ("Langue :", "Language:"),

            ["button.checkUpdate"] = ("Rechercher une mise à jour", "Check for updates"),
            ["button.tutorial"] = ("Guide de démarrage", "Getting started guide"),
            ["mode.switchToSimple"] = ("Mode simple", "Simple mode"),
            ["mode.switchToAdvanced"] = ("Mode avancé", "Advanced mode"),

            ["panel.record"] = ("Enregistrement", "Recording"),
            ["label.url"] = ("URL Chaturbate :", "Chaturbate URL:"),
            ["button.start"] = ("Démarrer", "Start"),
            ["button.stopAll"] = ("Tout arrêter", "Stop all"),
            ["button.addFavorite"] = ("+ Favori", "+ Favorite"),

            ["label.quality"] = ("Qualité source :", "Source quality:"),
            ["quality.best"] = ("Meilleure qualité (recommandé)", "Best quality (recommended)"),
            ["quality.medium"] = ("Qualité moyenne (720p max)", "Medium quality (720p max)"),
            ["quality.worst"] = ("Qualité minimale (économie)", "Minimum quality (saves space)"),

            ["label.codec"] = ("Codec de sortie :", "Output codec:"),
            ["codec.copy"] = ("Copie sans réencodage (recommandé, rapide)", "Copy without re-encoding (recommended, fast)"),
            ["codec.h264"] = ("H.264 (libx264 — compatibilité universelle)", "H.264 (libx264 — universal compatibility)"),
            ["codec.h265"] = ("H.265 (libx265 — fichier plus léger)", "H.265 (libx265 — smaller file)"),

            ["label.format"] = ("Format de sortie :", "Output format:"),
            ["format.mp4"] = ("MP4 (recommandé)", "MP4 (recommended)"),
            ["format.mkv"] = ("MKV (plus robuste)", "MKV (more robust)"),
            ["format.mov"] = ("MOV", "MOV"),

            ["label.saveDir"] = ("Dossier de sauvegarde :", "Save folder:"),
            ["button.browse"] = ("Parcourir...", "Browse..."),
            ["label.cookies"] = ("Cookies (optionnel) :", "Cookies (optional):"),
            ["label.proxy"] = ("Proxy SOCKS5/HTTP (optionnel) :", "SOCKS5/HTTP proxy (optional):"),
            ["checkbox.autoReconnect"] = ("Reconnexion automatique si le live se termine de façon inattendue", "Automatically reconnect if the live ends unexpectedly"),

            ["panel.progress"] = ("Enregistrements en cours", "Active recordings"),

            ["panel.history"] = ("Historique des enregistrements", "Recording history"),
            ["column.file"] = ("Fichier", "File"),
            ["column.size"] = ("Taille", "Size"),
            ["column.duration"] = ("Durée", "Duration"),
            ["column.date"] = ("Date", "Date"),
            ["button.refresh"] = ("Actualiser", "Refresh"),
            ["button.openFolder"] = ("Ouvrir dossier", "Open folder"),

            ["panel.favorites"] = ("Favoris", "Favorites"),
            ["button.load"] = ("Charger", "Load"),
            ["button.removeFavorite"] = ("Supprimer favori", "Remove favorite"),

            ["panel.donate"] = ("Soutenir le projet", "Support the project"),
            ["button.donate"] = ("Faire un don (PayPal)", "Donate (PayPal)"),
            ["button.website"] = ("Site web", "Website"),
            ["label.donate"] = ("Scanne le QR code avec ton téléphone, ou clique sur le bouton.", "Scan the QR code with your phone, or click the button."),

            ["panel.logs"] = ("Logs", "Logs"),

            // 19.0 : fenêtre Paramètres + menu de la zone de notification.
            ["button.settings"] = ("Paramètres", "Settings"),
            ["window.settings"] = ("Paramètres", "Settings"),
            ["button.close"] = ("Fermer", "Close"),
            ["tray.open"] = ("Ouvrir", "Open"),
            ["tray.settings"] = ("Paramètres", "Settings"),
            ["tray.close"] = ("Fermer", "Close"),

            // 18.0 : bouton de rapport de bug.
            ["button.reportBug"] = ("Signaler un bug", "Report a bug"),
            ["button.diagnostic"] = ("Diagnostic", "Diagnostics"),

            // --- Erreurs / confirmations (24.0, traduction étendue) ---
            ["err.title"] = ("Erreur", "Error"),
            ["info.title"] = ("Info", "Info"),

            ["err.unauthorizedLocation.title"] = ("Emplacement non autorisé", "Unauthorized location"),
            ["err.unauthorizedLocation.body"] = (
                "Cette application ne peut pas s'exécuter depuis cet emplacement (partage réseau, dossier temporaire/éphémère ou dossier compressé NTFS). Déplace l'exécutable vers un dossier local standard.",
                "This application cannot run from this location (network share, temporary/ephemeral folder, or compressed NTFS folder). Move the executable to a standard local folder."),
            ["err.invalidCaptureLogDir"] = (
                "Dossier de capture ou de logs invalide (sandbox de chemins).",
                "Invalid capture or log folder (path sandbox)."),
            ["err.noChangelogDetails"] = ("Aucun détail disponible pour cette version.", "No details available for this version."),
            ["dialog.whatsNew.title"] = ("Nouveautés", "What's new"),
            ["err.cannotOpenPage"] = ("Impossible d'ouvrir la page : {0}", "Could not open the page: {0}"),
            ["err.cannotOpenFolder"] = ("Impossible d'ouvrir le dossier : {0}", "Could not open the folder: {0}"),
            ["err.binaryNotFound"] = ("{0} introuvable : {1}", "{0} not found: {1}"),
            ["err.cannotComputeHash"] = ("Impossible de calculer le hash de {0}.", "Could not compute the hash of {0}."),
            ["confirm.trustBinary.title"] = ("Vérification de {0}", "Verifying {0}"),
            ["confirm.trustBinary.body"] = (
                "Le hash de {0} ne correspond ni à la version testée par le mainteneur, ni à un hash déjà approuvé sur cette machine.\n\nHash calculé : {1}\n\nC'est normal si tu viens de télécharger une version plus récente depuis une source officielle — yt-dlp et ffmpeg sont mis à jour fréquemment. Si tu ne sais pas d'où vient ce fichier, réponds Non.\n\nFaire confiance à ce {0} et continuer ?",
                "The hash of {0} doesn't match either the version tested by the maintainer or a hash already approved on this machine.\n\nComputed hash: {1}\n\nThis is normal if you just downloaded a newer version from an official source — yt-dlp and ffmpeg are updated frequently. If you don't know where this file came from, answer No.\n\nTrust this {0} and continue?"),
            ["err.authenticodeInvalid"] = ("Signature Authenticode invalide pour {0}.", "Invalid Authenticode signature for {0}."),
            ["err.urlRejected"] = (
                "URL refusée par la validation de sécurité (voir log de session).",
                "URL rejected by security validation (see session log)."),
            ["err.caPinningFailed"] = ("Échec du pinning CA pour les binaires.", "CA pinning failed for the binaries."),
            ["err.invalidOutputDir"] = ("Dossier de sortie invalide ou interdit par la sandbox : {0}", "Invalid output folder, or blocked by the sandbox: {0}"),
            ["err.tlsVerificationFailed"] = ("Échec de la vérification TLS du serveur distant ({0}).", "TLS verification of the remote server failed ({0})."),
            ["info.jobAlreadyRunning"] = ("Un enregistrement pour '{0}' est déjà en cours.", "A recording for '{0}' is already running."),
            ["err.invalidLogPath"] = ("Chemin de log invalide.", "Invalid log path."),
            ["err.cannotStartDownload"] = ("Impossible de démarrer le téléchargement : {0}", "Could not start the download: {0}"),
            ["info.invalidOrDuplicateFavorite"] = ("URL invalide ou déjà présente dans les favoris.", "Invalid URL, or already in favorites."),
            ["err.cannotOpenDonateLink"] = ("Impossible d'ouvrir le lien de don : {0}", "Could not open the donation link: {0}"),
            ["err.cannotOpenWebsite"] = ("Impossible d'ouvrir le site web : {0}", "Could not open the website: {0}"),
            ["info.alreadyLatestVersion"] = ("Tu utilises déjà la dernière version (v{0}).", "You're already using the latest version (v{0})."),
            ["dialog.updates.title"] = ("Mises à jour", "Updates"),
            ["confirm.updateAvailable.title"] = ("Mise à jour disponible", "Update available"),
            ["confirm.updateAvailable.body"] = (
                "Version v{0} disponible (actuelle : v{1}).\n\nTélécharger et installer maintenant ? L'application redémarrera automatiquement.{2}",
                "Version v{0} is available (current: v{1}).\n\nDownload and install now? The app will restart automatically.{2}"),
            ["confirm.updateAvailable.runningJobsWarning"] = (
                "\n\n⚠ {0} enregistrement(s) en cours seront interrompus par le redémarrage.",
                "\n\n⚠ {0} recording(s) in progress will be interrupted by the restart."),
            ["err.updateCheckFailed"] = ("Échec de la vérification des mises à jour : {0}", "Update check failed: {0}"),
            ["err.cannotOpenBugReport"] = ("Impossible d'ouvrir le formulaire de rapport de bug : {0}", "Could not open the bug report form: {0}"),

            ["err.invalidCaptureDir"] = ("Dossier invalide ou interdit par la sandbox de chemins.", "Invalid folder, or blocked by the path sandbox."),
            ["err.invalidCookiesFile"] = ("Fichier invalide ou interdit par la sandbox de chemins.", "Invalid file, or blocked by the path sandbox."),
            ["dialog.chooseCaptureDir"] = ("Choisis le dossier de sauvegarde des enregistrements", "Choose the folder to save recordings to"),
            ["dialog.chooseCookiesFile.title"] = (
                "Choisis un fichier cookies.txt (format Netscape, exporté depuis ton navigateur)",
                "Choose a cookies.txt file (Netscape format, exported from your browser)"),
            ["dialog.chooseCookiesFile.filter"] = (
                "Fichiers cookies (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
                "Cookie files (*.txt)|*.txt|All files (*.*)|*.*"),
            ["placeholder.proxyExample"] = ("ex: socks5://127.0.0.1:9050", "e.g. socks5://127.0.0.1:9050"),

            // --- Notifications (zone de notification) ---
            ["notif.recordingCompleted.title"] = ("Enregistrement terminé", "Recording finished"),
            ["notif.recordingFailed.title"] = ("Erreur d'enregistrement", "Recording error"),
            ["notif.recordingFailed.body"] = ("{0} : flux inaccessible ou interrompu de façon inattendue.", "{0}: stream unreachable or unexpectedly interrupted."),
            ["notif.favoriteAdded.title"] = ("Favori ajouté", "Favorite added"),
            ["notif.trayHint.title"] = ("Toujours actif", "Still active"),
            ["notif.trayHint.body"] = (
                "Chaturbate Recorder continue de tourner dans la zone de notification. Clic droit sur l'icône pour ouvrir, accéder aux paramètres ou fermer complètement.",
                "Chaturbate Recorder keeps running in the notification area. Right-click the icon to open, access settings, or close completely."),

            // --- Lignes d'état par enregistrement (jamais retraduites après
            // création avant ce passage — voir MainForm.ApplyLanguage) ---
            ["job.open"] = ("Ouvrir", "Open"),
            ["job.preparing"] = ("Préparation...", "Preparing..."),
            ["job.stop"] = ("Stop", "Stop"),
            ["job.remove"] = ("Retirer", "Remove"),
            ["job.cancelled"] = ("Annulé", "Cancelled"),
            ["job.running"] = ("En cours...", "Running..."),
            ["job.reconnectingIn"] = ("Reco. dans {0}s...", "Reconnecting in {0}s..."),
            ["job.cancelReconnect"] = ("Annuler", "Cancel"),
            ["job.state.Completed"] = ("Terminé", "Completed"),
            ["job.state.Failed"] = ("Échec", "Failed"),
            ["job.state.Stopped"] = ("Arrêté", "Stopped"),

            // --- Formats dépendant de la langue (unités, date) ---
            ["units.bytes"] = ("o", "B"),
            ["units.kb"] = ("Ko", "KB"),
            ["units.mb"] = ("Mo", "MB"),
            ["units.gb"] = ("Go", "GB"),
            ["format.dateTime"] = ("dd/MM/yyyy HH:mm", "MM/dd/yyyy HH:mm"),

            // --- Panneau Logs (visible à l'écran, distinct de Services/Logger.cs) ---
            ["log.autoReconnectCancelled"] = ("Reconnexion automatique annulée.", "Automatic reconnection cancelled."),
            ["log.jobFinished"] = ("Job terminé (état : {0}).", "Job finished (state: {0})."),
            ["log.downloadInterrupted"] = ("Téléchargement interrompu.", "Download interrupted."),
            ["log.reconnectScheduled"] = ("Reconnexion automatique dans {0}s (tentative {1}/{2})...", "Automatic reconnection in {0}s (attempt {1}/{2})..."),
            ["log.reconnectAttempt"] = ("Nouvelle tentative de connexion ({0}/{1})...", "New connection attempt ({0}/{1})..."),
            ["log.noVideoFound"] = ("Aucune vidéo trouvée.", "No video found."),
            ["log.thumbnailCreated"] = ("Miniature créée : {0}", "Thumbnail created: {0}"),
            ["log.thumbnailError"] = ("Erreur création miniature.", "Error creating thumbnail."),
            ["log.reencodeCancelledNoVideo"] = ("Réencodage annulé : aucune vidéo trouvée.", "Re-encoding cancelled: no video found."),
            ["log.reencodeStarted"] = ("Réencodage ({0}) démarré en arrière-plan : {1}", "Re-encoding ({0}) started in the background: {1}"),
            ["log.reencodeFinished"] = ("Réencodage ({0}) terminé : {1}", "Re-encoding ({0}) finished: {1}"),
            ["log.reencodeFailed"] = ("Échec du réencodage ({0}).", "Re-encoding failed ({0})."),
            ["log.reencodeError"] = ("Erreur réencodage : {0}", "Re-encoding error: {0}"),
            ["log.startingRecording"] = ("Démarrage de l'enregistrement...", "Starting recording..."),
            ["log.downloadingUpdate"] = ("Téléchargement de la mise à jour v{0}...", "Downloading update v{0}..."),
            ["log.saveDirChanged"] = ("Dossier de sauvegarde changé : {0}", "Save folder changed: {0}"),
            ["log.cookiesFileChanged"] = ("Fichier cookies changé : {0}", "Cookies file changed: {0}"),

            // --- Guide de démarrage (TutorialForm) ---
            ["tutorial.windowTitle"] = ("Guide de démarrage", "Getting started guide"),
            ["tutorial.back"] = ("◀ Précédent", "◀ Back"),
            ["tutorial.next"] = ("Suivant ▶", "Next ▶"),
            ["tutorial.finish"] = ("Terminer", "Finish"),
            ["tutorial.progress"] = ("Étape {0} / {1}", "Step {0} / {1}"),

            ["tutorial.step1.title"] = ("Bienvenue", "Welcome"),
            ["tutorial.step1.body"] = (
                "Chaturbate Recorder enregistre des lives en local, avec des vérifications de sécurité intégrées (hash des binaires, sandbox de chemins, ACL).\n\nCe guide rapide passe en revue les fonctionnalités principales — tu peux le rouvrir à tout moment via le bouton \"Guide de démarrage\".",
                "Chaturbate Recorder records lives locally, with built-in security checks (binary hashes, path sandboxing, ACLs).\n\nThis quick guide covers the main features — you can reopen it anytime via the \"Getting started guide\" button."),
            ["tutorial.step2.title"] = ("Démarrer un enregistrement", "Starting a recording"),
            ["tutorial.step2.body"] = (
                "Colle l'URL Chaturbate dans le champ en haut, puis clique sur \"Démarrer\".\n\nChaque enregistrement tourne indépendamment : tu peux en lancer plusieurs en même temps sans ouvrir plusieurs fenêtres. \"Tout arrêter\" stoppe tous les enregistrements en cours d'un coup.",
                "Paste the Chaturbate URL into the field at the top, then click \"Start\".\n\nEach recording runs independently: you can start several at once without opening multiple windows. \"Stop all\" stops every ongoing recording at once."),
            ["tutorial.step3.title"] = ("Qualité, codec et format", "Quality, codec, and format"),
            ["tutorial.step3.body"] = (
                "Trois menus déroulants te laissent choisir :\n\n•  la qualité source (meilleure / moyenne / minimale)\n•  le codec de sortie (copie sans perte, ou réencodage H.264/H.265 fait après coup, sans bloquer l'appli)\n•  le conteneur (MP4, MKV — plus robuste en cas d'arrêt brutal —, ou MOV)",
                "Three dropdown menus let you choose:\n\n•  the source quality (best / medium / minimum)\n•  the output codec (lossless copy, or H.264/H.265 re-encoding done afterward, without blocking the app)\n•  the container (MP4, MKV — more resilient to an abrupt stop —, or MOV)"),
            ["tutorial.step4.title"] = ("Dossier de sauvegarde", "Save folder"),
            ["tutorial.step4.body"] = (
                "Le bouton \"Parcourir...\" te permet de choisir où sont enregistrées tes vidéos.\n\nCe choix est mémorisé automatiquement pour les prochains lancements.",
                "The \"Browse...\" button lets you choose where your videos are saved.\n\nThis choice is remembered automatically for future launches."),
            ["tutorial.step5.title"] = ("Confidentialité", "Privacy"),
            ["tutorial.step5.body"] = (
                "Le champ \"Cookies\" permet d'importer une session déjà connectée depuis ton navigateur (fichier cookies.txt), pour accéder au contenu réservé à un compte.\n\nLe champ \"Proxy\" route le trafic via un SOCKS5/HTTP de ton choix, pour masquer ton IP réelle vis-à-vis du site distant.",
                "The \"Cookies\" field lets you import an already logged-in session from your browser (cookies.txt file), to access content restricted to an account.\n\nThe \"Proxy\" field routes traffic through a SOCKS5/HTTP proxy of your choice, to hide your real IP from the remote site."),
            ["tutorial.step6.title"] = ("Suivi des enregistrements", "Tracking recordings"),
            ["tutorial.step6.body"] = (
                "Chaque enregistrement actif apparaît comme une ligne dans \"Enregistrements en cours\", avec :\n\n•  sa propre barre de progression\n•  un bouton \"Ouvrir\" (accès direct à la page du stream)\n•  un bouton Stop individuel (qui devient \"Retirer\" une fois terminé)",
                "Each active recording appears as a row in \"Active recordings\", with:\n\n•  its own progress bar\n•  an \"Open\" button (direct access to the stream's page)\n•  an individual Stop button (which becomes \"Remove\" once finished)"),
            ["tutorial.step7.title"] = ("Sécurité et mises à jour", "Security and updates"),
            ["tutorial.step7.body"] = (
                "Avant chaque enregistrement, l'appli vérifie le hash de yt-dlp.exe et ffmpeg.exe, et surveille l'emplacement d'exécution du programme.\n\nLe bouton \"Rechercher une mise à jour\" en haut de la fenêtre vérifie automatiquement les nouvelles versions publiées sur GitHub.",
                "Before every recording, the app checks the hash of yt-dlp.exe and ffmpeg.exe, and monitors the program's execution location.\n\nThe \"Check for updates\" button at the top of the window automatically checks for new versions published on GitHub."),

            // --- Crash Reporter (2.1) ---
            ["crash.windowTitle"] = ("Erreur inattendue", "Unexpected error"),
            ["crash.titleFatal"] = ("⚠️ Chaturbate Recorder a rencontré une erreur fatale et doit fermer.", "⚠️ Chaturbate Recorder encountered a fatal error and must close."),
            ["crash.titleRecoverable"] = ("⚠️ Chaturbate Recorder a rencontré une erreur inattendue.", "⚠️ Chaturbate Recorder encountered an unexpected error."),
            ["crash.detailsWithFile"] = ("{0} : {1}\r\n\r\nRapport complet enregistré dans :\r\n{2}", "{0}: {1}\r\n\r\nFull report saved to:\r\n{2}"),
            ["crash.detailsNoFile"] = ("{0} : {1}\r\n\r\nLe rapport détaillé n'a pas pu être enregistré sur disque.", "{0}: {1}\r\n\r\nThe detailed report could not be saved to disk."),
            ["crash.openFolder"] = ("Ouvrir le dossier des logs", "Open logs folder"),
            ["crash.restart"] = ("Redémarrer", "Restart"),
            ["crash.continue"] = ("Continuer", "Continue"),

            // --- Diagnostic (2.3) ---
            ["diag.copy"] = ("Copier", "Copy"),
            ["diag.sectionBinaryVersions"] = ("--- Binaires (versions) ---", "--- Binaries (versions) ---"),
            ["diag.checking"] = ("(vérification...)", "(checking...)"),
            ["diag.sectionNetwork"] = ("--- Réseau ---", "--- Network ---"),
            ["diag.reachable"] = ("joignable", "reachable"),
            ["diag.unreachable"] = ("injoignable", "unreachable"),
            ["diag.application"] = ("Application : v{0}", "Application: v{0}"),
            ["diag.system"] = ("Système : {0} ({1} bits)", "System: {0} ({1}-bit)"),
            ["diag.sectionHashIntegrity"] = ("--- Intégrité des binaires (hash SHA256) ---", "--- Binary integrity (SHA256 hash) ---"),
            ["diag.sectionExecDir"] = ("--- Dossier d'exécution ---", "--- Execution folder ---"),
            ["diag.authorizedLocation"] = ("Emplacement autorisé : {0}", "Authorized location: {0}"),
            ["diag.yes"] = ("oui", "yes"),
            ["diag.no"] = ("non", "no"),
            ["diag.sectionAcl"] = ("--- ACL (droits d'écriture élargis détectés ?) ---", "--- ACL (broad write access detected?) ---"),
            ["diag.execDirLabel"] = ("Dossier d'exécution", "Execution folder"),
            ["diag.captureDirLabel"] = ("Dossier de capture", "Capture folder"),
            ["diag.logDirLabel"] = ("Dossier de logs", "Log folder"),
            ["diag.proxyConfigured"] = ("Proxy configuré : {0}", "Configured proxy: {0}"),
            ["diag.none"] = ("aucun", "none"),
            ["diag.notFound"] = ("introuvable", "not found"),
            ["diag.hashOkMaintainer"] = ("OK (version testée par le mainteneur)", "OK (version tested by the maintainer)"),
            ["diag.hashOkApproved"] = ("OK (approuvé manuellement)", "OK (manually approved)"),
            ["diag.hashFailed"] = ("ÉCHEC (hash inattendu — pas encore approuvé)", "FAILED (unexpected hash — not yet approved)"),
            ["diag.aclPermissive"] = ("{0} : permissive — {1}", "{0}: permissive — {1}"),
            ["diag.aclOk"] = ("{0} : OK", "{0}: OK"),
            ["diag.unknownVersion"] = ("version inconnue", "unknown version"),
            ["diag.versionError"] = ("erreur ({0})", "error ({0})"),
        };

        public static string Get(string key, AppLanguage lang)
        {
            if (!Strings.TryGetValue(key, out var pair)) return key;
            return lang == AppLanguage.English ? pair.En : pair.Fr;
        }

        /// <summary>Raccourci pour les clés contenant des "{0}"/"{1}"... (string.Format).</summary>
        public static string Format(string key, AppLanguage lang, params object?[] args) =>
            string.Format(Get(key, lang), args);
    }
}
