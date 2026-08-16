using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaturbateRecorderApp.Services
{
    public enum CookieFileProblem
    {
        None,
        /// <summary>Fichier vide ou uniquement des commentaires.</summary>
        Empty,
        /// <summary>Ligne magique absente : Python refuse le fichier avant même de le lire.</summary>
        MissingNetscapeHeader,
        /// <summary>Ligne HttpOnly sans le « # » initial — le défaut réellement rencontré.</summary>
        HttpOnlyPrefixMissingHash,
        /// <summary>Moins de 7 colonnes séparées par tabulation.</summary>
        TooFewFields,
        /// <summary>Domaine et indicateur de sous-domaines incohérents.</summary>
        DomainFlagMismatch,
    }

    public sealed class CookieFileCheck
    {
        public required CookieFileProblem Problem { get; init; }
        /// <summary>Numéro de ligne 1-based, 0 si le défaut ne vise pas une ligne.</summary>
        public int Line { get; init; }
        public bool IsValid => Problem == CookieFileProblem.None;
    }

    /// <summary>
    /// Contrôle qu'un cookies.txt sera accepté par yt-dlp, AVANT de l'enregistrer
    /// dans les réglages.
    ///
    /// **Pourquoi ce contrôle existe** (constaté le 2026-08-05 sur un vrai
    /// fichier) : yt-dlp délègue la lecture à `http.cookiejar` de Python, qui
    /// est bien plus strict que le format ne le laisse croire. Un fichier
    /// refusé fait échouer **tous** les enregistrements, et rend la
    /// surveillance (88.0) définitivement muette — le contrôle d'état renvoie
    /// alors une erreur qui n'est pas « hors ligne », donc l'état Unknown, qui
    /// ne déclenche jamais rien. Les deux pannes se présentent comme un
    /// problème de réseau ou de salon, jamais comme un problème de cookies.
    ///
    /// **Aggravant** : yt-dlp ignore SILENCIEUSEMENT un fichier absent, mais
    /// échoue durement sur un fichier malformé. Les deux échecs les plus
    /// probables sont donc ceux qui se voient le moins.
    /// </summary>
    public static class CookieFileValidator
    {
        private const string HttpOnlyPrefix = "#HttpOnly_";

        public static CookieFileCheck Validate(IEnumerable<string> lines)
        {
            var all = lines?.ToList() ?? new List<string>();
            var sawHeader = false;
            var sawData = false;

            for (var i = 0; i < all.Count; i++)
                {
                var raw = (all[i] ?? "").TrimEnd('\r');
                var lineNo = i + 1;

                if (string.IsNullOrWhiteSpace(raw)) continue;

                // Ligne magique : Python la vérifie sur la PREMIÈRE ligne lue et
                // refuse tout le fichier sinon.
                if (!sawHeader && raw.StartsWith("#", StringComparison.Ordinal)
                                && raw.Contains("HTTP Cookie File", StringComparison.OrdinalIgnoreCase))
                {
                    sawHeader = true;
                    continue;
                }

                // LE défaut réellement rencontré : Cookie-Editor (ou une
                // manipulation ultérieure) laisse « HttpOnly_ » sans le « # ».
                // Python ne reconnaît alors pas le préfixe, ne saute pas la
                // ligne comme un commentaire, et bute sur le domaine.
                if (raw.StartsWith("HttpOnly_", StringComparison.Ordinal))
                    return new CookieFileCheck { Problem = CookieFileProblem.HttpOnlyPrefixMissingHash, Line = lineNo };

                var line = raw;
                if (line.StartsWith(HttpOnlyPrefix, StringComparison.Ordinal))
                    line = line[HttpOnlyPrefix.Length..];
                else if (line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var fields = line.Split('\t');
                if (fields.Length < 7)
                    return new CookieFileCheck { Problem = CookieFileProblem.TooFewFields, Line = lineNo };

                // L'invariant que http.cookiejar impose par une assertion : un
                // domaine commençant par un point signifie « et ses
                // sous-domaines », et doit donc aller de pair avec TRUE.
                var domainSpecified = fields[1].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                if (domainSpecified != fields[0].StartsWith(".", StringComparison.Ordinal))
                    return new CookieFileCheck { Problem = CookieFileProblem.DomainFlagMismatch, Line = lineNo };

                sawData = true;
            }

            if (!sawHeader) return new CookieFileCheck { Problem = CookieFileProblem.MissingNetscapeHeader };
            if (!sawData) return new CookieFileCheck { Problem = CookieFileProblem.Empty };

            return new CookieFileCheck { Problem = CookieFileProblem.None };
        }
    }
}
