using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>Une ligne de cookie au format Netscape.</summary>
    public sealed class CookieEntry
    {
        public required string Domain { get; init; }
        public required bool IncludeSubdomains { get; init; }
        public required string Path { get; init; }
        public required bool Secure { get; init; }
        /// <summary>Epoch UNIX, ou 0 pour un cookie de session.</summary>
        public required long Expires { get; init; }
        public required string Name { get; init; }
        public required string Value { get; init; }
    }

    /// <summary>
    /// Lecture d'un cookies.txt au format Netscape (92.0). Jusqu'ici
    /// l'application se contentait de passer le CHEMIN du fichier à yt-dlp
    /// (--cookies) sans jamais l'ouvrir ; l'import des favoris est le premier
    /// cas où elle doit s'en servir pour ses propres requêtes.
    ///
    /// Volontairement tolérant : un export de navigateur n'est jamais tout à
    /// fait canonique, et refuser une ligne de trop revient à perdre
    /// silencieusement l'authentification.
    /// </summary>
    public static class CookieFileReader
    {
        /// <summary>
        /// Préfixe utilisé par les exports de Chrome/Firefox pour marquer un
        /// cookie HttpOnly. C'est un commentaire en apparence, mais la ligne
        /// porte de vraies données : la sauter ferait perdre exactement les
        /// cookies de session, qui sont presque tous HttpOnly.
        /// </summary>
        private const string HttpOnlyPrefix = "#HttpOnly_";

        public static List<CookieEntry> Parse(IEnumerable<string> lines)
        {
            var cookies = new List<CookieEntry>();

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var line = raw;
                if (line.StartsWith(HttpOnlyPrefix, StringComparison.Ordinal))
                    line = line[HttpOnlyPrefix.Length..];
                else if (line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                // Tabulation par spécification, mais certains exports alignent
                // sur des espaces. On accepte les deux plutôt que de rendre
                // l'import dépendant de l'outil d'export utilisé.
                var parts = line.Split('\t');
                if (parts.Length < 7)
                    parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Au moins 7 champs, pas exactement 7 : une colonne
                // supplémentaire (SameSite, ajoutée par certaines extensions)
                // ne doit pas faire rejeter la ligne.
                if (parts.Length < 7) continue;

                // La valeur peut être vide ; le nom, jamais.
                if (string.IsNullOrEmpty(parts[5])) continue;

                // Expiration illisible -> 0, c'est-à-dire cookie de session.
                // Ne JAMAIS écarter une expiration à 0 : c'est le cas normal
                // d'un cookie de session, précisément celui qui authentifie.
                var expires = long.TryParse(parts[4], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var exp) && exp > 0 ? exp : 0;

                cookies.Add(new CookieEntry
                {
                    Domain = parts[0],
                    IncludeSubdomains = parts[1].Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                    Path = string.IsNullOrEmpty(parts[2]) ? "/" : parts[2],
                    Secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                    Expires = expires,
                    Name = parts[5],
                    Value = parts[6],
                });
            }

            return cookies;
        }

        /// <summary>
        /// Vrai si le fichier ressemble à un export Netscape exploitable. Sert
        /// à distinguer "mauvais format" de "bon format mais session expirée",
        /// deux pannes que l'utilisateur ne peut pas départager autrement.
        /// </summary>
        public static bool LooksLikeNetscapeFormat(IEnumerable<string> lines)
            => Parse(lines).Count > 0;
    }
}
