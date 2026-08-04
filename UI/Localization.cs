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

            ["label.duration"] = ("Durée maximale :", "Maximum duration:"),
            ["duration.unlimited"] = ("Illimité", "Unlimited"),
            ["duration.15min"] = ("15 minutes", "15 minutes"),
            ["duration.30min"] = ("30 minutes", "30 minutes"),
            ["duration.1h"] = ("1 heure", "1 hour"),
            ["duration.2h"] = ("2 heures", "2 hours"),
            ["duration.4h"] = ("4 heures", "4 hours"),
            ["duration.8h"] = ("8 heures", "8 hours"),

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
            ["button.sponsor"] = ("Sponsoriser (GitHub)", "Sponsor (GitHub)"),
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
            // En-tête de groupe, affiché seulement quand l'annonce couvre
            // plusieurs versions d'un coup (mise à jour ayant sauté des
            // versions) — voir MainForm.ShowChangelog.
            ["changelog.versionHeader"] = ("Version {0}", "Version {0}"),
            ["changelog.noDetails"] = (
                "Aucun détail disponible pour cette version.",
                "No details available for this version."),

            ["error.cannotOpenPage"] = ("Impossible d'ouvrir la page : {0}", "Cannot open the page: {0}"),
            ["error.cannotOpenFolder"] = ("Impossible d'ouvrir le dossier : {0}", "Cannot open the folder: {0}"),
            ["error.cannotOpenDonateLink"] = ("Impossible d'ouvrir le lien de don : {0}", "Cannot open the donation link: {0}"),
            ["error.cannotOpenWebsite"] = ("Impossible d'ouvrir le site web : {0}", "Cannot open the website: {0}"),
            ["error.cannotOpenSponsor"] = (
                "Impossible d'ouvrir la page de parrainage : {0}",
                "Cannot open the sponsor page: {0}"),
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
            // --- 79.0 : recherche automatique de mise à jour ---
            ["notify.updateAvailable.title"] = ("Mise à jour disponible", "Update available"),
            ["notify.updateAvailable.body"] = (
                "La version v{0} est disponible. Clique sur cette notification pour l'installer.",
                "Version v{0} is available. Click this notification to install it."),
            // Info-bulle de l'icône de la zone de notification : Windows la
            // tronque à 63 caractères, garder les deux traductions courtes.
            ["tray.updateAvailable"] = (
                "Chaturbate Recorder — mise à jour v{0} disponible",
                "Chaturbate Recorder — update v{0} available"),
            // --- 98.0 : note de legalite, affichee par le bouton du meme nom ---
            ["button.legal"] = ("Légalité", "Legality"),
            ["window.legal"] = ("Légalité (Belgique)", "Legality (Belgium)"),
            ["legal.body"] = (
                "Chaturbate Recorder enregistre uniquement des flux publiquement accessibles, tels que l'utilisateur peut déjà les visionner dans son navigateur. Le logiciel ne contourne aucune mesure technique de protection, n'accède à aucun système ou contenu privé et n'exploite aucune faille : les infractions d'accès non autorisé à un système informatique (art. 550bis du Code pénal) et d'atteinte aux données (art. 550ter du Code pénal) ne sont donc pas en cause.\n\nL'enregistrement d'un flux public peut relever de l'exception de copie privée, prévue par l'article XI.190, §1er, 5° du Code de droit économique (anciennement art. 22, §1er, 5° de la loi du 30 juin 1994), tant que l'usage reste strictement personnel et non commercial. En revanche, diffuser, partager, transmettre ou rendre accessible un enregistrement à caractère sexuel d'une personne sans son consentement constitue une infraction pénale en Belgique (art. 417/5 du Code pénal), indépendamment de la manière dont l'enregistrement a été obtenu.\n\nL'utilisateur est seul responsable de l'usage qu'il fait des enregistrements. Il lui appartient de vérifier les conditions d'utilisation de la plateforme — qui peuvent interdire l'enregistrement indépendamment de la loi —, le droit à l'image et la protection des données des personnes filmées, ainsi que le droit d'auteur applicable et la législation de son pays de résidence.\n\nCe texte est informatif et ne constitue pas un avis juridique.",
                "Chaturbate Recorder only records publicly accessible streams, the ones the user can already watch in their browser. The software circumvents no technical protection measure, accesses no private system or content, and exploits no vulnerability: the offences of unauthorised access to a computer system (art. 550bis of the Belgian Criminal Code) and of data interference (art. 550ter of the Criminal Code) are therefore not engaged.\n\nRecording a public stream may fall under the private-copy exception laid down in article XI.190, §1, 5° of the Code of Economic Law (formerly art. 22, §1, 5° of the Act of 30 June 1994), as long as the use remains strictly personal and non-commercial. Conversely, distributing, sharing, transmitting or making available a sexual recording of a person without their consent is a criminal offence in Belgium (art. 417/5 of the Criminal Code), regardless of how the recording was obtained.\n\nThe user alone is responsible for what they do with the recordings. It is up to them to check the platform's terms of service — which may prohibit recording independently of the law —, the image rights and data protection of the people filmed, as well as the applicable copyright and the legislation of their country of residence.\n\nThis text is informative and does not constitute legal advice."),
            // --- 88.0 : surveillance automatique ---
            ["panel.watch"] = ("Surveillance", "Monitoring"),
            ["column.room"] = ("Salon", "Room"),
            ["column.watchState"] = ("État", "State"),
            ["button.watchAdd"] = ("+ Surveiller", "+ Monitor"),
            ["button.watchRemove"] = ("Ne plus surveiller", "Stop monitoring"),
            ["watch.state.pending"] = ("En attente...", "Pending..."),
            ["watch.state.online"] = ("En ligne", "Online"),
            ["watch.state.offline"] = ("Hors ligne", "Offline"),
            ["watch.state.unknown"] = ("Indéterminé", "Unknown"),
            ["watch.state.recording"] = ("Enregistrement...", "Recording..."),
            ["watch.alreadyWatched"] = (
                "{0} est déjà surveillé.",
                "{0} is already monitored."),
            ["watch.started.title"] = ("Surveillance", "Monitoring"),
            ["watch.started.body"] = (
                "{0} est en ligne : l'enregistrement démarre.",
                "{0} is online: recording is starting."),
            ["label.watchInterval"] = (
                "Vérifier les salons surveillés toutes les :",
                "Check monitored rooms every:"),
            ["checkbox.autoUpdateCheck"] = (
                "Rechercher automatiquement les mises à jour (toutes les heures)",
                "Automatically check for updates (every hour)"),

            // --- 24.0 : guide de démarrage (TutorialForm) ---
            // Les noms d'éléments cités entre guillemets dans la prose reprennent
            // mot pour mot les libellés traduits plus haut (button.start,
            // panel.progress, job.open...) : le guide doit désigner les boutons
            // tels qu'ils s'affichent réellement dans la langue courante.
            ["tutorial.back"] = ("◀ Précédent", "◀ Back"),
            ["tutorial.next"] = ("Suivant ▶", "Next ▶"),
            ["tutorial.finish"] = ("Terminer", "Finish"),
            ["tutorial.stepProgress"] = ("Étape {0} / {1}", "Step {0} / {1}"),

            ["tutorial.welcome.title"] = ("Bienvenue", "Welcome"),
            ["tutorial.welcome.body"] = (
                "Chaturbate Recorder enregistre des lives en local, avec des vérifications de sécurité intégrées (hash des binaires, sandbox de chemins, ACL).\n\nCe guide rapide passe en revue les fonctionnalités principales — tu peux le rouvrir à tout moment via le bouton \"Guide de démarrage\".",
                "Chaturbate Recorder records live streams locally, with built-in security checks (binary hashes, path sandbox, ACL).\n\nThis quick guide walks through the main features — you can reopen it at any time from the \"Getting started guide\" button."),

            ["tutorial.start.title"] = ("Démarrer un enregistrement", "Starting a recording"),
            ["tutorial.start.body"] = (
                "Colle l'URL Chaturbate dans le champ en haut, puis clique sur \"Démarrer\".\n\nChaque enregistrement tourne indépendamment : tu peux en lancer plusieurs en même temps sans ouvrir plusieurs fenêtres. \"Tout arrêter\" stoppe tous les enregistrements en cours d'un coup.",
                "Paste the Chaturbate URL into the field at the top, then click \"Start\".\n\nEach recording runs independently: you can start several at once without opening multiple windows. \"Stop all\" stops every running recording at once."),

            ["tutorial.quality.title"] = ("Qualité, codec et format", "Quality, codec and format"),
            ["tutorial.quality.body"] = (
                "Trois menus déroulants te laissent choisir :\n\n•  la qualité source (meilleure / moyenne / minimale)\n•  le codec de sortie (copie sans perte, ou réencodage H.264/H.265 fait après coup, sans bloquer l'appli)\n•  le conteneur (MP4, MKV — plus robuste en cas d'arrêt brutal —, ou MOV)",
                "Three drop-down menus let you choose:\n\n•  the source quality (best / medium / minimum)\n•  the output codec (lossless copy, or H.264/H.265 re-encoding done afterwards, without blocking the app)\n•  the container (MP4, MKV — more robust if the recording stops abruptly — or MOV)"),

            ["tutorial.saveDir.title"] = ("Dossier de sauvegarde", "Save folder"),
            ["tutorial.saveDir.body"] = (
                "Le bouton \"Parcourir...\" te permet de choisir où sont enregistrées tes vidéos.\n\nCe choix est mémorisé automatiquement pour les prochains lancements.",
                "The \"Browse...\" button lets you choose where your videos are saved.\n\nThis choice is remembered automatically for future sessions."),

            ["tutorial.privacy.title"] = ("Confidentialité", "Privacy"),
            ["tutorial.privacy.body"] = (
                "Le champ \"Cookies\" permet d'importer une session déjà connectée depuis ton navigateur (fichier cookies.txt), pour accéder au contenu réservé à un compte.\n\nLe champ \"Proxy\" route le trafic via un SOCKS5/HTTP de ton choix, pour masquer ton IP réelle vis-à-vis du site distant.",
                "The \"Cookies\" field imports an already signed-in session from your browser (cookies.txt file), to reach content reserved for logged-in accounts.\n\nThe \"Proxy\" field routes traffic through a SOCKS5/HTTP proxy of your choice, to hide your real IP from the remote site."),

            ["tutorial.tracking.title"] = ("Suivi des enregistrements", "Tracking your recordings"),
            ["tutorial.tracking.body"] = (
                "Chaque enregistrement actif apparaît comme une ligne dans \"Enregistrements en cours\", avec :\n\n•  sa propre barre de progression\n•  un bouton \"Ouvrir\" (accès direct à la page du stream)\n•  un bouton Stop individuel (qui devient \"Retirer\" une fois terminé)",
                "Each active recording appears as a row under \"Active recordings\", with:\n\n•  its own progress bar\n•  an \"Open\" button (direct access to the stream page)\n•  its own Stop button (which becomes \"Remove\" once finished)"),

            ["tutorial.support.title"] = ("Soutenir le projet", "Supporting the project"),
            ["tutorial.support.body"] = (
                "Chaturbate Recorder est gratuit, sans publicité et sans collecte de données.\n\nSi tu souhaites soutenir son développement, le panneau \"Soutenir le projet\" propose un parrainage récurrent via GitHub Sponsors, ou un don ponctuel via PayPal.\n\nAucune fonctionnalité n'est réservée aux contributeurs : l'application est identique dans tous les cas.",
                "Chaturbate Recorder is free, ad-free, and collects no data.\n\nIf you would like to support its development, the \"Support the project\" panel offers recurring sponsorship through GitHub Sponsors, or a one-off donation through PayPal.\n\nNo feature is ever reserved for contributors: the application is the same either way."),

            ["tutorial.security.title"] = ("Sécurité et mises à jour", "Security and updates"),
            ["tutorial.security.body"] = (
                "Avant chaque enregistrement, l'appli vérifie le hash de yt-dlp.exe et ffmpeg.exe, et surveille l'emplacement d'exécution du programme.\n\nLe bouton \"Rechercher une mise à jour\" en haut de la fenêtre vérifie automatiquement les nouvelles versions publiées sur GitHub.",
                "Before each recording, the app verifies the hash of yt-dlp.exe and ffmpeg.exe, and checks where the program is running from.\n\nThe \"Check for updates\" button at the top of the window looks for new versions published on GitHub."),
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
