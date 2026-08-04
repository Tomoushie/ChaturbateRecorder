using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>Pourquoi un import a échoué — chaque cas a un message distinct.</summary>
    public enum FavoritesImportStatus
    {
        Success,
        NoCookiesConfigured,
        CookieFileUnreadable,
        CookieFileNotNetscape,
        NotAuthenticated,
        NetworkError,
        /// <summary>Page reçue, authentifiée, mais aucun salon reconnu : le site a probablement changé.</summary>
        NothingRecognised,
        /// <summary>403 : la protection anti-bot du site refuse le client, indépendamment du compte.</summary>
        BlockedByBotProtection,
    }

    public sealed class FavoritesImportResult
    {
        public required FavoritesImportStatus Status { get; init; }
        public IReadOnlyList<string> Urls { get; init; } = Array.Empty<string>();
        public string? Detail { get; init; }
    }

    /// <summary>
    /// 403 renvoyé par la protection anti-bot. Distinct d'une erreur réseau :
    /// les cookies sont bons, le compte a le droit, c'est le CLIENT qui est
    /// refusé — donc rien que l'utilisateur puisse corriger de son côté.
    /// </summary>
    internal sealed class BotProtectionException : Exception { }

    /// <summary>
    /// Import des favoris depuis le compte Chaturbate de l'utilisateur (92.0,
    /// reformulé — voir CLAUDE.md). Les favoris sont PRIVÉS : aucun identifiant
    /// public ne permet de les lire, il faut une session authentifiée. C'est le
    /// cookies.txt déjà fourni par l'utilisateur qui la porte.
    ///
    /// **Fragilité assumée** : Chaturbate n'expose aucune API publique
    /// documentée pour cette liste, donc l'extraction lit le HTML de la page.
    /// Elle cassera le jour où le site sera refondu, sans que la CI puisse le
    /// détecter — aucun test ne peut interroger le vrai site. D'où la
    /// conception : l'échec est le cas NORMAL, chaque cause a son message, et
    /// jamais d'échec silencieux. ExtractRoomNames est isolée et testée sur du
    /// HTML figé, pour que la partie déterministe reste vérifiable.
    /// </summary>
    public static class FavoritesImporter
    {
        private const string FavoritesUrl = "https://chaturbate.com/followed-cams/";

        /// <summary>
        /// Segments de premier niveau du site qui ne sont pas des salons. Sans
        /// cette liste, "/apps/", "/tags/" ou "/accounts/" seraient importés
        /// comme s'il s'agissait de modèles.
        /// </summary>
        private static readonly HashSet<string> NonRoomSegments = new(StringComparer.OrdinalIgnoreCase)
        {
            "accounts", "apps", "affiliates", "auth", "external_link", "followed-cams",
            "help", "legal", "login", "logout", "my", "password", "photo_videos",
            "privacy", "roomlogin", "security", "signup", "static", "support",
            "tags", "terms", "tipping", "token_purchase", "user",
        };

        public static async Task<FavoritesImportResult> ImportAsync(string? cookiesFilePath)
        {
            if (string.IsNullOrWhiteSpace(cookiesFilePath) || !File.Exists(cookiesFilePath))
                return new FavoritesImportResult { Status = FavoritesImportStatus.NoCookiesConfigured };

            List<CookieEntry> cookies;
            try
            {
                cookies = CookieFileReader.Parse(File.ReadAllLines(cookiesFilePath));
            }
            catch (Exception ex)
            {
                return new FavoritesImportResult
                {
                    Status = FavoritesImportStatus.CookieFileUnreadable,
                    Detail = ex.Message,
                };
            }

            if (cookies.Count == 0)
                return new FavoritesImportResult { Status = FavoritesImportStatus.CookieFileNotNetscape };

            string html;
            try
            {
                html = await FetchAsync(cookies);
            }
            catch (BotProtectionException)
            {
                return new FavoritesImportResult { Status = FavoritesImportStatus.BlockedByBotProtection };
            }
            catch (Exception ex)
            {
                return new FavoritesImportResult
                {
                    Status = FavoritesImportStatus.NetworkError,
                    Detail = ex.Message,
                };
            }

            // Une session expirée ne renvoie pas une erreur HTTP : Chaturbate
            // sert la page de connexion avec un code 200. Sans ce contrôle, on
            // conclurait "aucun favori" là où il faut dire "reconnecte-toi".
            if (LooksLikeLoginPage(html))
                return new FavoritesImportResult { Status = FavoritesImportStatus.NotAuthenticated };

            var names = ExtractRoomNames(html);
            if (names.Count == 0)
                return new FavoritesImportResult { Status = FavoritesImportStatus.NothingRecognised };

            return new FavoritesImportResult
            {
                Status = FavoritesImportStatus.Success,
                Urls = names.Select(n => $"https://chaturbate.com/{n}/").ToList(),
            };
        }

        private static async Task<string> FetchAsync(List<CookieEntry> cookies)
        {
            var jar = new CookieContainer();
            foreach (var c in cookies)
            {
                try
                {
                    // Le point de tête d'un domaine Netscape (".chaturbate.com")
                    // signifie "et ses sous-domaines" ; CookieContainer le refuse
                    // sous cette forme et veut le domaine nu.
                    var domain = c.Domain.TrimStart('.');
                    if (!domain.EndsWith("chaturbate.com", StringComparison.OrdinalIgnoreCase)) continue;
                    jar.Add(new Cookie(c.Name, c.Value, c.Path, domain) { Secure = c.Secure });
                }
                catch (Exception ex)
                {
                    // Un cookie malformé isolé ne doit pas faire échouer tout
                    // l'import : les autres suffisent souvent à authentifier.
                    Logger.Log($"Cookie ignoré ({c.Name}) : {ex.Message}", LogLevel.WARN);
                }
            }

            using var handler = new HttpClientHandler
            {
                CookieContainer = jar,
                UseCookies = true,
                // Un navigateur annonce toujours gzip/br ; ne pas décompresser
                // rend la réponse illisible et fait partie des signaux qui
                // trahissent un client non-navigateur.
                AutomaticDecompression = DecompressionMethods.All,
            };
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };

            // Jeu d'en-têtes complet d'un Chrome réel. Le seul User-Agent ne
            // suffit pas : la protection anti-bot de Chaturbate compare
            // l'ensemble des en-têtes, et une requête qui annonce Chrome sans
            // les Sec-Fetch-* ni Accept-Language se distingue immédiatement.
            var h = http.DefaultRequestHeaders;
            h.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            h.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            h.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7");
            h.Add("Upgrade-Insecure-Requests", "1");
            h.Add("Sec-Fetch-Dest", "document");
            h.Add("Sec-Fetch-Mode", "navigate");
            h.Add("Sec-Fetch-Site", "same-origin");
            h.Add("Sec-Fetch-User", "?1");
            h.Add("sec-ch-ua", "\"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\", \"Google Chrome\";v=\"131\"");
            h.Add("sec-ch-ua-mobile", "?0");
            h.Add("sec-ch-ua-platform", "\"Windows\"");
            // On arrive normalement depuis le site, pas de nulle part.
            h.Referrer = new Uri("https://chaturbate.com/");

            var response = await http.GetAsync(FavoritesUrl);

            // 403 avec des cookies valides ne veut pas dire « pas le droit » :
            // c'est la protection anti-bot qui refuse le client, pas le compte
            // qui manque d'autorisation. Distingué des autres erreurs réseau
            // parce que la conduite à tenir n'a rien à voir.
            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new BotProtectionException();

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        internal static bool LooksLikeLoginPage(string html)
            => html.Contains("id=\"login_form\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains("name=\"login\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains("/auth/login/", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Extrait les noms de salons des liens de la page. Déterministe et sans
        /// réseau : c'est la seule partie de l'import qui puisse être testée.
        /// Conserve l'ordre d'apparition et dédoublonne (une vignette porte
        /// plusieurs liens vers le même salon : image, pseudo, badge).
        /// </summary>
        internal static List<string> ExtractRoomNames(string html)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Un salon est un lien de premier niveau : href="/pseudo/". Les
            // pseudos Chaturbate sont en minuscules, chiffres et underscore.
            foreach (Match m in Regex.Matches(html, @"href=[""']/([a-zA-Z0-9_]+)/[""']"))
            {
                var name = m.Groups[1].Value;
                if (NonRoomSegments.Contains(name)) continue;
                if (seen.Add(name)) names.Add(name);
            }

            return names;
        }
    }
}
