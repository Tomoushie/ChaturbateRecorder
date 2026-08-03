using System.Collections.Generic;

namespace ChaturbateRecorderApp.UI
{
    public enum AppLanguage { French, English }

    /// <summary>
    /// Traductions de l'UI principale (20.0) : les libellés fixes posés dans
    /// InitializeComponent (labels, boutons, cases à cocher, en-têtes de
    /// colonnes, items de ComboBox, bascule de mode), plus les libellés
    /// dynamiques des lignes de job du panneau "Enregistrements en cours"
    /// (préfixe "job.", voir MainForm.RefreshJobRowLabels). Les messages
    /// d'erreur/confirmations, notifications toast, logs, le guide de
    /// démarrage (TutorialForm) et l'historique des nouveautés (Changelog)
    /// restent en français pour l'instant — hors périmètre de ce premier
    /// passage de traduction, ils sont générés dynamiquement à des dizaines
    /// d'endroits différents du code.
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

            // Lignes de job dynamiques (panneau "Enregistrements en cours") :
            // construites en code (BuildJobRow) plutôt que par InitializeComponent,
            // donc absentes du premier passage de traduction (20.0) — voir
            // MainForm.RefreshJobRowLabels.
            ["job.open"] = ("Ouvrir", "Open"),
            ["job.stop"] = ("Stop", "Stop"),
            ["job.remove"] = ("Retirer", "Remove"),
            ["job.cancel"] = ("Annuler", "Cancel"),
            ["job.preparing"] = ("Préparation...", "Preparing..."),
            ["job.running"] = ("En cours...", "Running..."),
            ["job.cancelled"] = ("Annulé", "Cancelled"),
            ["job.reconnectIn"] = ("Reco. dans {0}s...", "Reco. in {0}s..."),
            ["job.state.completed"] = ("Terminé", "Completed"),
            ["job.state.failed"] = ("Échec", "Failed"),
            ["job.state.stopped"] = ("Arrêté", "Stopped"),

            // --- 24.0 : messages d'erreur, dialogues et notifications ---
            // Générés à la volée (MessageBox/ShowBalloonTip) plutôt que posés
            // une fois pour toutes : ils passent par Localization.Current au
            // moment de l'affichage, pas par ApplyLanguage.
            ["dialog.error"] = ("Erreur", "Error"),
            ["dialog.info"] = ("Info", "Info"),
            ["dialog.updates"] = ("Mises à jour", "Updates"),

            ["error.unauthorizedLocation.title"] = ("Emplacement non autorisé", "Unauthorized location"),
            ["error.unauthorizedLocation"] = (
                "Cette application ne peut pas s'exécuter depuis cet emplacement (partage réseau, dossier temporaire/éphémère ou dossier compressé NTFS). Déplace l'exécutable vers un dossier local standard.",
                "This application cannot run from this location (network share, temporary/ephemeral folder, or NTFS-compressed folder). Move the executable to a standard local folder."),
            ["error.invalidCaptureOrLogDir"] = (
                "Dossier de capture ou de logs invalide (sandbox de chemins).",
                "Invalid capture or log folder (path sandbox)."),

            ["changelog.title"] = ("Nouveautés — v{0}", "What's new — v{0}"),
            ["changelog.noDetails"] = (
                "Aucun détail disponible pour cette version.",
                "No details available for this version."),

            ["error.cannotOpenPage"] = ("Impossible d'ouvrir la page : {0}", "Cannot open the page: {0}"),
            ["error.cannotOpenFolder"] = ("Impossible d'ouvrir le dossier : {0}", "Cannot open the folder: {0}"),
            ["error.cannotOpenDonateLink"] = ("Impossible d'ouvrir le lien de don : {0}", "Cannot open the donation link: {0}"),
            ["error.cannotOpenWebsite"] = ("Impossible d'ouvrir le site web : {0}", "Cannot open the website: {0}"),
            ["error.cannotOpenBugReport"] = (
                "Impossible d'ouvrir le formulaire de rapport de bug : {0}",
                "Cannot open the bug report form: {0}"),

            ["error.binaryNotFound"] = ("{0} introuvable : {1}", "{0} not found: {1}"),
            ["error.cannotComputeHash"] = ("Impossible de calculer le hash de {0}.", "Cannot compute the hash of {0}."),
            ["error.invalidAuthenticode"] = (
                "Signature Authenticode invalide pour {0}.",
                "Invalid Authenticode signature for {0}."),
            ["verify.title"] = ("Vérification de {0}", "Verifying {0}"),
            ["verify.hashMismatch"] = (
                "Le hash de {0} ne correspond ni à la version testée par le mainteneur, ni à un hash déjà approuvé sur cette machine.\n\nHash calculé : {1}\n\nC'est normal si tu viens de télécharger une version plus récente depuis une source officielle — yt-dlp et ffmpeg sont mis à jour fréquemment. Si tu ne sais pas d'où vient ce fichier, réponds Non.\n\nFaire confiance à ce {0} et continuer ?",
                "The hash of {0} matches neither the version tested by the maintainer nor a hash already approved on this machine.\n\nComputed hash: {1}\n\nThis is expected if you just downloaded a newer version from an official source — yt-dlp and ffmpeg are updated frequently. If you don't know where this file came from, answer No.\n\nTrust this {0} and continue?"),

            ["error.urlRejected"] = (
                "URL refusée par la validation de sécurité (voir log de session).",
                "URL rejected by security validation (see session log)."),
            ["error.caPinningFailed"] = (
                "Échec du pinning CA pour les binaires.",
                "CA pinning failed for the binaries."),
            ["error.invalidOutputDir"] = (
                "Dossier de sortie invalide ou interdit par la sandbox : {0}",
                "Output folder invalid or forbidden by the sandbox: {0}"),
            ["error.tlsVerificationFailed"] = (
                "Échec de la vérification TLS du serveur distant ({0}).",
                "TLS verification of the remote server failed ({0})."),
            ["error.invalidLogPath"] = ("Chemin de log invalide.", "Invalid log path."),
            ["error.cannotStartDownload"] = (
                "Impossible de démarrer le téléchargement : {0}",
                "Cannot start the download: {0}"),
            ["info.alreadyRecording"] = (
                "Un enregistrement pour '{0}' est déjà en cours.",
                "A recording for '{0}' is already running."),
            ["info.invalidOrDuplicateFavorite"] = (
                "URL invalide ou déjà présente dans les favoris.",
                "Invalid URL, or already in favorites."),

            ["error.invalidFolderSandbox"] = (
                "Dossier invalide ou interdit par la sandbox de chemins.",
                "Folder invalid or forbidden by the path sandbox."),
            ["error.invalidFileSandbox"] = (
                "Fichier invalide ou interdit par la sandbox de chemins.",
                "File invalid or forbidden by the path sandbox."),

            ["update.upToDate"] = (
                "Tu utilises déjà la dernière version (v{0}).",
                "You are already running the latest version (v{0})."),
            ["update.availableTitle"] = ("Mise à jour disponible", "Update available"),
            ["update.availableBody"] = (
                "Version v{0} disponible (actuelle : v{1}).\n\nTélécharger et installer maintenant ? L'application redémarrera automatiquement.{2}",
                "Version v{0} available (current: v{1}).\n\nDownload and install now? The application will restart automatically.{2}"),
            ["update.runningJobsWarning"] = (
                "\n\n⚠ {0} enregistrement(s) en cours seront interrompus par le redémarrage.",
                "\n\n⚠ {0} recording(s) in progress will be interrupted by the restart."),
            ["error.updateCheckFailed"] = (
                "Échec de la vérification des mises à jour : {0}",
                "Update check failed: {0}"),

            ["notify.recordingDone.title"] = ("Enregistrement terminé", "Recording finished"),
            ["notify.recordingError.title"] = ("Erreur d'enregistrement", "Recording error"),
            ["notify.recordingError.body"] = (
                "{0} : flux inaccessible ou interrompu de façon inattendue.",
                "{0}: stream unreachable or interrupted unexpectedly."),
            ["notify.favoriteAdded.title"] = ("Favori ajouté", "Favorite added"),
            ["notify.stillActive.title"] = ("Toujours actif", "Still running"),
            ["notify.stillActive.body"] = (
                "Chaturbate Recorder continue de tourner dans la zone de notification. Clic droit sur l'icône pour ouvrir, accéder aux paramètres ou fermer complètement.",
                "Chaturbate Recorder keeps running in the notification area. Right-click the icon to open it, reach the settings, or close it completely."),
        };

        /// <summary>
        /// Exposé au projet de tests (voir LocalizationTests) : permet de
        /// vérifier que les deux variantes d'une même clé restent cohérentes,
        /// en particulier que leurs trous de formatage correspondent — un
        /// "{0}" présent d'un seul côté ne casserait Format() que dans une
        /// seule langue, cas typiquement manqué par un test manuel.
        /// </summary>
        internal static IReadOnlyDictionary<string, (string Fr, string En)> AllStrings => Strings;

        /// <summary>
        /// Langue courante de l'application (24.0). Les libellés fixes de l'UI
        /// reçoivent leur langue en paramètre (ils sont tous réassignés d'un
        /// coup par ApplyLanguage), mais les messages d'erreur, dialogues et
        /// notifications sont générés à la volée depuis des dizaines
        /// d'endroits — dont Program.Main, qui s'exécute AVANT que MainForm
        /// n'existe. Faire circuler un paramètre de langue jusque-là
        /// demanderait de tout réécrire : un état statique est ici le bon
        /// compromis. Tenu à jour par Program.Main et par MainForm
        /// (constructeur + HandleLanguageChangedFromSettings).
        /// </summary>
        public static AppLanguage Current { get; set; } = AppLanguage.French;

        public static string Get(string key) => Get(key, Current);

        public static string Get(string key, AppLanguage lang)
        {
            if (!Strings.TryGetValue(key, out var pair)) return key;
            return lang == AppLanguage.English ? pair.En : pair.Fr;
        }

        /// <summary>
        /// Variante formatée pour les messages à trous ("... : {0}"). Volontairement
        /// sans surcharge prenant une AppLanguage explicite : l'enum se lierait
        /// silencieusement au params object[] (donc au premier trou) au lieu de
        /// choisir la langue. Les appelants qui ont besoin d'une langue précise
        /// passent par Get(key, lang) + string.Format.
        /// </summary>
        public static string Format(string key, params object[] args) =>
            string.Format(Get(key, Current), args);
    }
}
