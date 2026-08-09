using System;
using System.IO;

namespace ChaturbateRecorderApp.Config
{
    /// <summary>
    /// Configuration centralisée de l'application.
    /// Équivalent du hashtable $Config et des variables de toggles de sécurité
    /// du script PowerShell d'origine.
    ///
    /// A REMPLIR avant utilisation : YtDlpExpectedSha256, FfmpegExpectedSha256,
    /// et éventuellement les valeurs de pinning CA/TLS si tu actives ces toggles.
    /// </summary>
    public static class AppConfig
    {
        public static readonly string AppDir = AppContext.BaseDirectory;

        public static string FFmpegPath        = Path.Combine(AppDir, "ffmpeg.exe");
        public static string YtDlpPath         = Path.Combine(AppDir, "yt-dlp.exe");
        // Dossiers par defaut, propres a l'utilisateur courant.
        //
        // Ils valaient "E:\Streamlink\videos" et "E:\Streamlink\logs" jusqu'a la
        // v1.26.x : les chemins du poste du mainteneur, codes en dur. Sur toute
        // machine sans disque E:, le premier Directory.CreateDirectory du
        // demarrage levait DirectoryNotFoundException et l'application mourait
        // AVANT d'afficher quoi que ce soit — et le rapport de crash lui-meme ne
        // pouvait pas s'ecrire, puisqu'il va dans LogDir\crashes. Signale par un
        // utilisateur qui avait pourtant tout installe correctement.
        //
        // Lecon : une valeur par defaut n'est pas une preference, c'est le
        // premier contact d'un inconnu avec le logiciel. Aucun chemin absolu
        // specifique a une machine ne doit y figurer.
        public static string CaptureDir        = DefaultCaptureDir();
        public static string LogDir            = DefaultLogDir();

        /// <summary>
        /// Dossier de capture par defaut : "Vidéos" de l'utilisateur. Repli sur
        /// le dossier de l'application si le profil est indisponible (compte de
        /// service, profil temporaire), cas rare mais qui ne doit pas empecher
        /// le demarrage.
        /// </summary>
        public static string DefaultCaptureDir()
        {
            var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            if (string.IsNullOrWhiteSpace(videos)) videos = AppDir;
            return Path.Combine(videos, "Chaturbate Recorder");
        }

        /// <summary>
        /// Logs dans LocalAppData plutot qu'a cote de l'exe : le dossier existe
        /// toujours, il est inscriptible sans elevation, et il survit a un
        /// deplacement de l'application. C'est aussi la que va le rapport de
        /// crash, qui doit pouvoir s'ecrire meme quand tout le reste a echoue.
        /// </summary>
        public static string DefaultLogDir()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(local)) local = AppDir;
            return Path.Combine(local, "ChaturbateRecorder", "logs");
        }
        public static string FavoritesFile     = Path.Combine(AppDir, "favorites.json");
        public static string DonateQrPath      = Path.Combine(AppDir, "donate_qr.png");
        public static string DonateQrExpectedSha256 = "FD21762BBE7C23A1CBDB5AB18210FDC2F6466B6840E20D2E795548D80F73CB71";
        public static string DonateUrl         = "https://paypal.me/tomoushie";
        // Parrainage récurrent (80.0), distinct du don ponctuel PayPal ci-dessus.
        public static string SponsorUrl        = "https://github.com/sponsors/Tomoushie";
        public static string WebsiteUrl        = "https://tomoushie.github.io/ChaturbateRecorder/";
        public static int    ThumbnailOffsetSeconds = 10;
        // Watchdog anti-freeze (2.1) : durée max sans nouvelle ligne de sortie
        // yt-dlp/ffmpeg avant de considérer le process figé et le tuer.
        public static int    YtDlpWatchdogTimeoutSeconds = 120;

        // --- Rotation des logs (2.4) ---
        // Fichiers du dossier de logs non modifiés depuis plus longtemps que ça
        // sont supprimés automatiquement au démarrage.
        public static int  LogRetentionDays    = 14;
        // Taille max d'un fichier de log avant rotation (renommage horodaté).
        public static long LogMaxFileSizeBytes = 20 * 1024 * 1024;

        // --- Confidentialité / session (13.0, 16.0) ---
        // CookiesFilePath : fichier cookies.txt (format Netscape) exporté depuis
        // le navigateur, transmis à yt-dlp via --cookies pour accéder au contenu
        // réservé à un compte connecté. Aucun mot de passe n'est jamais géré ou
        // stocké par l'application elle-même.
        public static string CookiesFilePath   = "";
        // ProxyUrl : proxy SOCKS5/HTTP (ex: socks5://127.0.0.1:9050) transmis à
        // yt-dlp via --proxy pour masquer l'IP réelle vis-à-vis du site distant.
        // L'application ne fournit pas de proxy/VPN elle-même.
        public static string ProxyUrl          = "";

        // --- Reconnexion automatique (4.2) ---
        // Si le live se termine (erreur ou fin de flux) sans arrêt manuel, et
        // que l'option est cochée pour ce job, on retente après ce délai.
        public static int AutoReconnectDelaySeconds = 30;
        public static int AutoReconnectMaxAttempts   = 5;

        // --- Historique des enregistrements (4.4) ---
        // ffprobe.exe est optionnel : uniquement utilisé pour afficher la durée
        // des vidéos dans l'historique. S'il est absent, la durée affiche "N/A"
        // sans bloquer le reste de la fonctionnalité.
        public static string FFprobePath = Path.Combine(AppDir, "ffprobe.exe");

        public static readonly string[] Blacklist = { "example.com", "badhost.org" };
        // 40.0 — la liste vient de Platforms.AllowedDomains : une plateforme
        // prise en charge et un domaine autorisé sont la même chose, et deux
        // listes à tenir finiraient par diverger (une plateforme ajoutée à
        // l'une, oubliée dans l'autre, et le bac à sable refuse ses URLs).
        // La correspondance de SentinelGuard porte sur le suffixe : les
        // sous-domaines (www., m., fr.) sont couverts sans être listés.
        public static readonly string[] Whitelist = Services.Platforms.AllowedDomains;

        // --- Vérification binaire (hash + Authenticode) ---
        public static string YtDlpExpectedSha256           = "52FE3C26DCF71FBDC85B528589020BB0B8E383155CFA81B64DD447BBE35E24B8";
        public static bool   YtDlpRequireAuthenticode       = false;
        public static string YtDlpExpectedSignerThumbprint  = "";
        public static string YtDlpExpectedSignerSubject     = "";

        public static string FfmpegExpectedSha256           = "AD8F211BC894755E0061C55AB280AE00E8D3D4F15A8CC4372B24CFA247B5942E";
        public static bool   FfmpegRequireAuthenticode       = false;
        public static string FfmpegExpectedSignerThumbprint  = "";
        public static string FfmpegExpectedSignerSubject     = "";

        // --- Pinning CA local pour les binaires — désactivé par défaut ---
        // Si activé : yt-dlp.exe ET ffmpeg.exe doivent être signés Authenticode
        // avec un certificat dont le thumbprint correspond exactement à
        // TrustedCaThumbprint (pinning du certificat FEUILLE du signataire,
        // pas d'une CA racine au sens strict). Les builds publiques de yt-dlp
        // et ffmpeg ne sont généralement PAS signées : sans re-signature par tes
        // soins, ce toggle bloquera systématiquement le démarrage.
        public static bool   EnableCaPinning     = false;
        public static string TrustedCaThumbprint = "";
        public static string TrustedCaIssuer     = "";

        // --- Pinning TLS du serveur distant — désactivé par défaut ---
        public static bool   EnableTlsServerPinning   = false;
        public static string ServerExpectedThumbprint = "";
        public static string ServerExpectedIssuer     = "";
    }
}
