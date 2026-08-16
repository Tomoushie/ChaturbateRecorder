using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Plateformes prises en charge (40.0). L'ordre n'a aucune importance
    /// fonctionnelle : rien n'est persisté par index.
    /// </summary>
    public enum StreamPlatform
    {
        Chaturbate,
        Twitch,
        YouTube,
        TikTok,
        /// <summary>Domaine autorisé mais non reconnu — traité par défaut.</summary>
        Unknown,
    }

    /// <summary>
    /// Ce qui change d'une plateforme à l'autre (40.0), et rien d'autre : le
    /// domaine, la façon d'en tirer un nom lisible, et les phrases par
    /// lesquelles yt-dlp annonce qu'il n'y a rien à enregistrer.
    ///
    /// **Pourquoi ce n'est pas un système de plugins** : celui-ci est un item
    /// distinct (54.0). Ici, tout tient dans une table — ajouter une plateforme
    /// consiste à ajouter une ligne, pas une architecture.
    ///
    /// **Tout ce qui est ici a été MESURÉ sur le vrai yt-dlp**, jamais supposé
    /// (leçon de l'épisode 92.0, quatre versions dépensées sur une
    /// fonctionnalité dont la faisabilité n'avait pas été vérifiée) :
    /// - Twitch hors ligne : "ERROR: [twitch:stream] x: The channel is not currently live"
    /// - Twitch inexistant : "ERROR: [twitch:stream] x: x does not exist"
    /// - TikTok hors ligne : "ERROR: [tiktok:live] x: The channel is not currently live"
    /// - Chaturbate hors ligne : "ERROR: [Chaturbate] x: Room is currently offline"
    /// - YouTube : rend le code 0 MÊME sur une vidéo ordinaire, d'où le
    ///   contrôle de live_status plutôt que du seul code de sortie.
    ///
    /// **Instagram est volontairement absent** : mesuré le 2026-08-09, il
    /// redirige vers sa page de connexion et yt-dlp répond « Unsupported URL ».
    /// Sans session authentifiée, le live n'est pas atteignable — donc pas
    /// d'interface, pas de traduction et pas de test tant que ce n'est pas
    /// éprouvé pour de vrai.
    /// </summary>
    public static class Platforms
    {
        /// <summary>
        /// Domaines acceptés par le bac à sable d'URL. La correspondance de
        /// SentinelGuard porte sur le suffixe : "twitch.tv" couvre donc
        /// "www.twitch.tv" et "m.twitch.tv" sans les lister.
        /// </summary>
        public static readonly string[] AllowedDomains =
        {
            "chaturbate.com",
            "twitch.tv",
            "youtube.com",
            "youtu.be",
            "tiktok.com",
        };

        // Caractères conservés dans un nom de fichier. Le nom vient d'une URL,
        // donc de l'extérieur : il finit dans un chemin de sortie, et
        // PathValidator refuserait le fichier plutôt que de le corriger.
        private static readonly Regex UnsafeForFileName = new(@"[^A-Za-z0-9._-]", RegexOptions.Compiled);

        public static StreamPlatform Detect(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return StreamPlatform.Unknown;
            return DetectFromHost(uri.Host);
        }

        internal static StreamPlatform DetectFromHost(string host)
        {
            var h = (host ?? "").ToLowerInvariant().TrimEnd('.');

            if (Matches(h, "chaturbate.com")) return StreamPlatform.Chaturbate;
            if (Matches(h, "twitch.tv")) return StreamPlatform.Twitch;
            if (Matches(h, "youtube.com") || Matches(h, "youtu.be")) return StreamPlatform.YouTube;
            if (Matches(h, "tiktok.com")) return StreamPlatform.TikTok;
            return StreamPlatform.Unknown;
        }

        private static bool Matches(string host, string domain) =>
            host == domain || host.EndsWith("." + domain, StringComparison.Ordinal);

        /// <summary>
        /// Nom du glyphe IconManager représentant une plateforme (103.0), et
        /// libellé lisible. Regroupés ici avec la détection : une plateforme
        /// ajoutée sans son icône afficherait une case vide, ce que la table
        /// unique rend impossible à oublier.
        /// </summary>
        public static (string Icon, string Label) Badge(StreamPlatform platform) => platform switch
        {
            StreamPlatform.Twitch => ("twitch", "Twitch"),
            StreamPlatform.YouTube => ("youtube", "YouTube"),
            StreamPlatform.TikTok => ("tiktok", "TikTok"),
            StreamPlatform.Chaturbate => ("camera", "Chaturbate"),
            _ => ("camera", "Autre"),
        };

        /// <summary>
        /// Plateformes réellement annoncées à l'utilisateur, dans l'ordre
        /// d'affichage. <see cref="StreamPlatform.Unknown"/> en est exclu :
        /// c'est un cas de repli, pas une plateforme.
        /// </summary>
        public static readonly StreamPlatform[] Supported =
        {
            StreamPlatform.Chaturbate,
            StreamPlatform.Twitch,
            StreamPlatform.YouTube,
            StreamPlatform.TikTok,
        };

        /// <summary>
        /// Nom lisible d'une source, utilisé comme libellé ET comme base du nom
        /// de fichier de sortie.
        ///
        /// Le premier segment du chemin suffit pour Chaturbate et Twitch, mais
        /// PAS pour YouTube : sur "youtube.com/watch?v=ID" il vaut "watch", ce
        /// qui aurait nommé « watch » tous les enregistrements YouTube.
        /// </summary>
        public static string DisplayName(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return "stream";

            var segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            var raw = DetectFromHost(uri.Host) switch
            {
                StreamPlatform.YouTube => YouTubeName(uri, segments),
                StreamPlatform.TikTok => segments.FirstOrDefault()?.TrimStart('@'),
                _ => segments.FirstOrDefault(),
            };

            return Sanitize(raw, uri.Host);
        }

        private static string? YouTubeName(Uri uri, string[] segments)
        {
            // Une chaîne (@identifiant) donne un nom bien plus parlant que
            // l'identifiant de vidéo : on la préfère quand elle est présente.
            var handle = segments.FirstOrDefault(s => s.StartsWith('@'));
            if (handle != null) return handle.TrimStart('@');

            // youtu.be/<id> : l'identifiant est le premier segment.
            if (Matches(uri.Host.ToLowerInvariant(), "youtu.be")) return segments.FirstOrDefault();

            // youtube.com/watch?v=<id>
            var v = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(p => p.StartsWith("v=", StringComparison.OrdinalIgnoreCase));
            if (v != null) return v[2..];

            // /live/<id>, /shorts/<id>, /embed/<id>...
            return segments.Length >= 2 ? segments[^1] : segments.FirstOrDefault();
        }

        private static string Sanitize(string? raw, string host)
        {
            if (string.IsNullOrWhiteSpace(raw))
                raw = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;

            var cleaned = UnsafeForFileName.Replace(raw!, "_").Trim('_', '.', '-');
            return string.IsNullOrEmpty(cleaned) ? "stream" : cleaned;
        }
    }
}
