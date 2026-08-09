using System;
using System.Text.RegularExpressions;

namespace SentinelGuard
{
    /// <summary>
    /// Strict URL validation: scheme, host, path segments and query string.
    /// </summary>
    public static class UrlValidator
    {
        private static readonly string[] ForbiddenSchemes = { "javascript", "file", "ftp", "data", "blob" };

        private static readonly Regex LabelPattern =
            new(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?$", RegexOptions.Compiled);

        // Un segment ne doit JAMAIS contenir '/' : '/' est le séparateur de segments,
        // pas un caractère valide à l'intérieur d'un segment.
        //
        // '@' est autorisé DANS UN SEGMENT depuis 1.2.0 : c'est la forme des
        // identifiants de chaîne chez YouTube (/@NASA/live), TikTok
        // (/@compte/live) ou Mastodon, et les refuser rendait ces plateformes
        // inatteignables. Ce n'est pas un relâchement de la propriété qui
        // compte : le '@' dangereux est celui de l'AUTORITÉ (user:pass@hôte),
        // qui ne peut par construction pas se trouver dans un segment — une
        // autorité se termine au premier '/' — et qui est refusé un cran plus
        // haut par le contrôle explicite de Uri.UserInfo dans IsSafeUrl.
        private static readonly Regex SegmentPattern =
            new(@"^[a-zA-Z0-9_\-\.@]+$", RegexOptions.Compiled);

        private static readonly Regex SegmentForbiddenChars =
            new("[%\\?&=#\\s\"'<>\\\\;:\x00]", RegexOptions.Compiled);

        // '%' n'est autorisé que comme début d'une séquence d'échappement valide %XX.
        private static readonly Regex QueryStringPattern =
            new(@"^([a-zA-Z0-9_\-\.=&]|%[0-9A-Fa-f]{2})+$", RegexOptions.Compiled);

        /// <summary>
        /// Checks that <paramref name="domain"/> is a syntactically valid DNS
        /// name: at most 253 characters, every label 1–63 characters of
        /// letters, digits or hyphens, and never starting or ending with a
        /// hyphen. A single trailing dot (fully-qualified form) is tolerated.
        /// </summary>
        /// <param name="domain">The domain name to check.</param>
        /// <returns><see langword="true"/> if the format is valid.</returns>
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

        /// <summary>
        /// Decides whether a domain is allowed, matching subdomains as well:
        /// an entry <c>example.com</c> also covers <c>cdn.example.com</c>.
        /// The deny list always wins over the allow list.
        /// </summary>
        /// <param name="domain">The domain to test.</param>
        /// <param name="whitelist">
        /// Allowed domains. If empty or <see langword="null"/>, any domain not
        /// denied is accepted — pass a non-empty list to get strict allow-listing.
        /// </param>
        /// <param name="blacklist">Denied domains, checked first.</param>
        /// <returns>
        /// <see langword="true"/> if the domain is well-formed, not denied, and
        /// either allow-listed or the allow list is empty.
        /// </returns>
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

        /// <summary>
        /// Checks a single URL path segment (the part between two slashes).
        /// Rejects traversal segments (<c>.</c>, <c>..</c>), percent-encoding,
        /// query/fragment delimiters, whitespace, quotes and backslashes, so a
        /// segment cannot smuggle in a second path or escape upwards.
        /// </summary>
        /// <param name="segment">A single segment, without its slashes.</param>
        /// <returns><see langword="true"/> if the segment is safe.</returns>
        public static bool IsSafePathSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment)) return false;
            if (segment == "../" || segment == ".." || segment == "./" || segment == ".") return false;
            if (SegmentForbiddenChars.IsMatch(segment)) return false;
            if (!SegmentPattern.IsMatch(segment)) return false;
            if (segment.Length > 255) return false;
            return true;
        }

        /// <summary>
        /// Checks a URL query string. An empty query is accepted. Otherwise it
        /// must be at most 512 characters and contain only unreserved
        /// characters, <c>=</c>, <c>&amp;</c>, or well-formed <c>%XX</c> escape
        /// sequences — a lone <c>%</c> is rejected.
        /// </summary>
        /// <param name="query">The query string, without the leading <c>?</c>.</param>
        /// <returns><see langword="true"/> if the query string is safe.</returns>
        public static bool IsSafeQueryString(string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            if (query.Length > 512) return false;
            return QueryStringPattern.IsMatch(query);
        }

        /// <summary>
        /// Full URL check: HTTPS only (<c>javascript:</c>, <c>file:</c>,
        /// <c>ftp:</c>, <c>data:</c> and <c>blob:</c> are refused), host allowed
        /// by <paramref name="allowedDomains"/> / <paramref name="blacklist"/>,
        /// and every path segment and the query string individually safe.
        /// </summary>
        /// <param name="urlToTest">The URL to validate.</param>
        /// <param name="allowedDomains">
        /// Allowed domains, subdomains included. Empty means "any domain not denied".
        /// </param>
        /// <param name="blacklist">Denied domains, which always take precedence.</param>
        /// <returns><see langword="true"/> if the URL is safe to open.</returns>
        public static bool IsSafeUrl(string urlToTest, string[] allowedDomains, string[] blacklist) =>
            IsSafeUrl(urlToTest, allowedDomains, blacklist, out _);

        /// <summary>
        /// Same as <see cref="IsSafeUrl(string, string[], string[])"/>, but also
        /// reports why the URL was rejected.
        /// </summary>
        /// <param name="urlToTest">The URL to validate.</param>
        /// <param name="allowedDomains">Allowed domains, subdomains included.</param>
        /// <param name="blacklist">Denied domains, which always take precedence.</param>
        /// <param name="reason">
        /// On rejection, a human-readable explanation; <see langword="null"/> on success.
        /// </param>
        /// <returns><see langword="true"/> if the URL is safe to open.</returns>
        public static bool IsSafeUrl(string urlToTest, string[] allowedDomains, string[] blacklist, out string? reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(urlToTest))
            {
                reason = "URL is empty.";
                return false;
            }

            Uri uri;
            try { uri = new Uri(urlToTest); }
            catch (Exception ex)
            {
                reason = $"Malformed URL: {ex.Message}";
                return false;
            }

            var scheme = uri.Scheme.ToLowerInvariant();
            if (Array.IndexOf(ForbiddenSchemes, scheme) >= 0)
            {
                reason = $"Scheme not allowed: {scheme}://";
                return false;
            }
            if (scheme != "https")
            {
                reason = "Only the https:// scheme is allowed.";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                reason = "Credentials embedded in the URL are not allowed.";
                return false;
            }

            // Interdiction explicite des cibles locales/loopback (défense en profondeur :
            // la whitelist de domaine ci-dessous les rejette déjà, mais un blocage
            // explicite protège aussi si la whitelist est élargie par erreur).
            if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1")
            {
                reason = $"Local/loopback host not allowed: {uri.Host}";
                return false;
            }

            if (!IsDomainAllowed(uri.Host, allowedDomains, blacklist))
            {
                reason = $"Domain not allowed: {uri.Host}";
                return false;
            }

            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in segments)
            {
                if (!IsSafePathSegment(seg))
                {
                    reason = $"Invalid path segment: '{seg}'";
                    return false;
                }
            }

            var query = uri.Query.TrimStart('?');
            if (!IsSafeQueryString(query))
            {
                reason = $"Invalid query string: '{query}'";
                return false;
            }

            return true;
        }
    }
}
