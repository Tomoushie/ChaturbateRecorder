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
        public static string CaptureDir        = @"E:\Streamlink\videos";
        public static string LogDir            = @"E:\Streamlink\logs";
        public static string FavoritesFile     = Path.Combine(AppDir, "favorites.json");
        public static string DonateQrPath      = Path.Combine(AppDir, "donate_qr.png");
        public static string DonateQrExpectedSha256 = "FD21762BBE7C23A1CBDB5AB18210FDC2F6466B6840E20D2E795548D80F73CB71";
        public static string DonateUrl         = "https://paypal.me/tomoushie";
        public static int    ThumbnailOffsetSeconds = 10;

        public static readonly string[] Blacklist = { "example.com", "badhost.org" };
        public static readonly string[] Whitelist = { "chaturbate.com" };

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
