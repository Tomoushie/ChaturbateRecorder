using System;
using System.Text.RegularExpressions;

namespace ChaturbateRecorder.Security
{
    /// <summary>
    /// Validation stricte des URLs (schéma, hôte, segments, query string).
    /// </summary>
    public static class UrlValidator
    {
        private static readonly string[] ForbiddenSchemes = { "javascript", "file", "ftp", "data", "blob" };

        private static readonly Regex LabelPattern =
            new(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?$", RegexOptions.Compiled);

        // Un segment ne doit JAMAIS contenir '/' : '/' est le séparateur de segments,
        // pas un caractère valide à l'intérieur d'un segment.
        private static readonly Regex SegmentPattern =
            new(@"^[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled);

        private static readonly Regex SegmentForbiddenChars =
            new("[%\\?&=#\\s\"'<>\\\\;:@\x00]", RegexOptions.Compiled);

        // '%' n'est autorisé que comme début d'une séquence d'échappement valide %XX.
        private static readonly Regex QueryStringPattern =
            new(@"^([a-zA-Z0-9_\-\.=&]|%[0-9A-Fa-f]{2})+$", RegexOptions.Compiled);

        public static bool IsValidDomainFormat(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain)) return false;
            var normalized = domain.TrimEnd('.');
            if (normalized.Length == 0 || normalized.Length > 253) return false;

            foreach (var label in normalized.Split('.'))
            {
                if (!LabelPattern.IsMatch(label)) return false;
            }
            return true;
        }

        public static bool IsDomainAllowed(string domain, string[] whitelist, string[] blacklist)
        {
            if (!IsValidDomainFormat(domain)) return false;
            var normalizedDomain = domain.TrimEnd('.').ToLowerInvariant();

            foreach (var blocked in blacklist)
            {
                var blockedNormalized = blocked.TrimEnd('.').ToLowerInvariant();
                if (normalizedDomain == blockedNormalized || normalizedDomain.EndsWith("." + blockedNormalized))
                    return false;
            }

            if (whitelist != null && whitelist.Length > 0)
            {
                foreach (var allowed in whitelist)
                {
                    var allowedNormalized = allowed.TrimEnd('.').ToLowerInvariant();
                    if (normalizedDomain == allowedNormalized || normalizedDomain.EndsWith("." + allowedNormalized))
                        return true;
                }
                return false;
            }

            return true;
        }

        public static bool IsSafePathSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment)) return false;
            if (segment == "../" || segment == ".." || segment == "./" || segment == ".") return false;
            if (SegmentForbiddenChars.IsMatch(segment)) return false;
            if (!SegmentPattern.IsMatch(segment)) return false;
            if (segment.Length > 255) return false;
            return true;
        }

        public static bool IsSafeQueryString(string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            if (query.Length > 512) return false;
            return QueryStringPattern.IsMatch(query);
        }

        public static bool IsSafeUrl(string urlToTest, string[] allowedDomains, string[] blacklist) =>
            IsSafeUrl(urlToTest, allowedDomains, blacklist, out _);

        public static bool IsSafeUrl(string urlToTest, string[] allowedDomains, string[] blacklist, out string? reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(urlToTest))
            {
                reason = "URL vide.";
                return false;
            }

            Uri uri;
            try { uri = new Uri(urlToTest); }
            catch (Exception ex)
            {
                reason = $"URL malformée : {ex.Message}";
                return false;
            }

            var scheme = uri.Scheme.ToLowerInvariant();
            if (Array.IndexOf(ForbiddenSchemes, scheme) >= 0)
            {
                reason = $"Schéma interdit : {scheme}://";
                return false;
            }
            if (scheme != "https")
            {
                reason = "Seul le schéma https:// est autorisé.";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                reason = "Identifiants intégrés dans l'URL interdits.";
                return false;
            }

            // Interdiction explicite des cibles locales/loopback (défense en profondeur :
            // la whitelist de domaine ci-dessous les rejette déjà, mais un blocage
            // explicite protège aussi si la whitelist est élargie par erreur).
            if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1")
            {
                reason = $"Hôte local/loopback interdit : {uri.Host}";
                return false;
            }

            if (!IsDomainAllowed(uri.Host, allowedDomains, blacklist))
            {
                reason = $"Domaine non autorisé : {uri.Host}";
                return false;
            }

            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in segments)
            {
                if (!IsSafePathSegment(seg))
                {
                    reason = $"Segment de chemin invalide : '{seg}'";
                    return false;
                }
            }

            var query = uri.Query.TrimStart('?');
            if (!IsSafeQueryString(query))
            {
                reason = $"Query string invalide : '{query}'";
                return false;
            }

            return true;
        }
    }
}
