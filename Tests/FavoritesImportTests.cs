using System.Linq;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Import des favoris (92.0). Seules les parties déterministes sont
    /// testables : la lecture du cookies.txt et l'extraction des salons depuis
    /// du HTML figé. La requête réseau elle-même ne l'est pas — et c'est
    /// précisément pour ça que ces deux morceaux sont isolés du reste.
    /// </summary>
    public class CookieFileReaderTests
    {
        [Fact]
        public void ParsesAStandardNetscapeLine()
        {
            var c = Assert.Single(CookieFileReader.Parse(new[]
            {
                "# Netscape HTTP Cookie File",
                ".chaturbate.com\tTRUE\t/\tTRUE\t1785850468\t__cf_bm\tabc123",
            }));

            Assert.Equal(".chaturbate.com", c.Domain);
            Assert.True(c.IncludeSubdomains);
            Assert.Equal("/", c.Path);
            Assert.True(c.Secure);
            Assert.Equal(1785850468, c.Expires);
            Assert.Equal("__cf_bm", c.Name);
            Assert.Equal("abc123", c.Value);
        }

        /// <summary>
        /// Le cas qui casse le plus d'imports maison : Chrome et Firefox
        /// préfixent les cookies HttpOnly par "#HttpOnly_". Sauter toute ligne
        /// commençant par '#' fait perdre exactement les cookies de session,
        /// qui sont presque tous HttpOnly — donc l'authentification.
        /// </summary>
        [Fact]
        public void KeepsHttpOnlyLinesDespiteTheirHashPrefix()
        {
            var c = Assert.Single(CookieFileReader.Parse(new[]
            {
                "#HttpOnly_.chaturbate.com\tTRUE\t/\tTRUE\t0\tsessionid\tsecret",
            }));

            Assert.Equal("sessionid", c.Name);
            Assert.Equal(".chaturbate.com", c.Domain);
        }

        /// <summary>
        /// Second piège classique : une expiration à 0 signifie « cookie de
        /// session », pas « cookie invalide ». L'écarter revient à jeter le
        /// cookie d'authentification.
        /// </summary>
        [Fact]
        public void KeepsSessionCookiesWhoseExpiryIsZero()
        {
            var c = Assert.Single(CookieFileReader.Parse(new[]
            {
                "chaturbate.com\tFALSE\t/\tFALSE\t0\tsessionid\tsecret",
            }));

            Assert.Equal(0, c.Expires);
        }

        [Fact]
        public void IgnoresCommentsAndBlankLinesButNotData()
        {
            var cookies = CookieFileReader.Parse(new[]
            {
                "# Netscape HTTP Cookie File",
                "# This is a generated file!  Do not edit.",
                "",
                "   ",
                ".chaturbate.com\tTRUE\t/\tTRUE\t1785850468\ta\t1",
                ".chaturbate.com\tTRUE\t/\tTRUE\t1785850468\tb\t2",
            });

            Assert.Equal(new[] { "a", "b" }, cookies.Select(c => c.Name));
        }

        /// <summary>
        /// Une colonne en plus (SameSite, ajoutée par certaines extensions) ne
        /// doit pas faire rejeter la ligne : d'où "au moins 7", pas "exactement
        /// 7" — l'erreur que décrivait la note d'origine de l'item 92.0.
        /// </summary>
        [Fact]
        public void AcceptsAnExtraColumn()
        {
            var c = Assert.Single(CookieFileReader.Parse(new[]
            {
                ".chaturbate.com\tTRUE\t/\tTRUE\t1785850468\tcsrftoken\tv\tNone",
            }));

            Assert.Equal("csrftoken", c.Name);
            Assert.Equal("v", c.Value);
        }

        [Fact]
        public void AcceptsSpaceAlignedExports()
        {
            var c = Assert.Single(CookieFileReader.Parse(new[]
            {
                ".chaturbate.com   TRUE   /   TRUE   1785850468   __cf_bm   abc123",
            }));

            Assert.Equal("__cf_bm", c.Name);
        }

        [Fact]
        public void RejectsTruncatedLinesAndJsonExports()
        {
            var cookies = CookieFileReader.Parse(new[]
            {
                ".chaturbate.com\tTRUE\t/\tTRUE",
                "[{\"name\":\"sessionid\",\"value\":\"secret\"}]",
            });

            Assert.Empty(cookies);
        }

        /// <summary>
        /// Export réel de Cookie-Editor (l'outil utilisé par le mainteneur),
        /// valeurs remplacées par des factices — aucun jeton réel ne doit
        /// entrer dans le dépôt, un sessionid vaut un mot de passe.
        ///
        /// Ce qu'il verrouille : sur 6 cookies, **5 portent le préfixe
        /// `#HttpOnly_`**, dont `sessionid`. Un lecteur qui saute les lignes
        /// commençant par '#' n'en garderait qu'un seul — `csrftoken` — et
        /// l'authentification échouerait sans que rien ne le signale. C'est le
        /// cas réel, pas une hypothèse.
        /// </summary>
        [Fact]
        public void ReadsARealCookieEditorExport()
        {
            var cookies = CookieFileReader.Parse(new[]
            {
                "# Netscape HTTP Cookie File",
                "# http://curl.haxx.se/rfc/cookie_spec.html",
                "# This file was generated by Cookie-Editor",
                "#HttpOnly_.chaturbate.com\tTRUE\t/\tTRUE\t1785883141\t__cf_bm\tFAKE-cf-value",
                "#HttpOnly_.chaturbate.com\tTRUE\t/\tTRUE\t1791052637\tsessionid\tFAKESESSIONID",
                "#HttpOnly_.chaturbate.com\tTRUE\t/\tTRUE\t1820428637\t__utfpp\tf:FAKE:FAKE",
                "#HttpOnly_.chaturbate.com\tTRUE\t/\tTRUE\t1788440236\taffkey\t\"FAKEBASE64==\"",
                ".chaturbate.com\tTRUE\t/\tTRUE\t1817324733\tcsrftoken\tFAKECSRF",
                "#HttpOnly_.chaturbate.com\tTRUE\t/\tTRUE\t1820408233\tsbr\tsec:FAKE:FAKE",
            });

            Assert.Equal(6, cookies.Count);
            Assert.Contains(cookies, c => c.Name == "sessionid" && c.Value == "FAKESESSIONID");
            Assert.Contains(cookies, c => c.Name == "csrftoken");
            // Les valeurs base64 contiennent des '=' et des guillemets : elles
            // doivent traverser le lecteur telles quelles, sans être tronquées.
            Assert.Contains(cookies, c => c.Name == "affkey" && c.Value.EndsWith("==\""));
            Assert.All(cookies, c => Assert.Equal(".chaturbate.com", c.Domain));
        }

        [Fact]
        public void AnEmptyValueIsValidButAnEmptyNameIsNot()
        {
            var cookies = CookieFileReader.Parse(new[]
            {
                ".chaturbate.com\tTRUE\t/\tTRUE\t0\tempty\t",
                ".chaturbate.com\tTRUE\t/\tTRUE\t0\t\torphan",
            });

            var c = Assert.Single(cookies);
            Assert.Equal("empty", c.Name);
            Assert.Equal("", c.Value);
        }
    }

    public class FavoritesExtractionTests
    {
        private const string FavoritesHtml = """
            <html><body>
              <div class="roomList">
                <a href="/alice_example/"><img src="/thumb/alice_example.jpg"></a>
                <a href="/alice_example/">alice_example</a>
                <a href="/bob_example/">bob_example</a>
                <a href="/tags/">Tags</a>
                <a href="/accounts/settings/">Settings</a>
                <a href="https://chaturbate.com/external_link/">Ad</a>
              </div>
            </body></html>
            """;

        [Fact]
        public void ExtractsRoomNamesInOrder()
        {
            Assert.Equal(new[] { "alice_example", "bob_example" },
                FavoritesImporter.ExtractRoomNames(FavoritesHtml));
        }

        /// <summary>
        /// Une vignette porte plusieurs liens vers le même salon (image, pseudo,
        /// badge) : sans dédoublonnage, chaque modèle serait proposé deux ou
        /// trois fois.
        /// </summary>
        [Fact]
        public void DeduplicatesRepeatedLinksToTheSameRoom()
        {
            Assert.Single(FavoritesImporter.ExtractRoomNames(
                "<a href=\"/alice_example/\">a</a><a href=\"/alice_example/\">b</a>"));
        }

        /// <summary>
        /// Les liens de navigation du site sont des href de premier niveau
        /// exactement comme les salons : sans liste d'exclusion, "tags" et
        /// "accounts" seraient importés comme des modèles.
        /// </summary>
        [Fact]
        public void SkipsSiteNavigationSegments()
        {
            var names = FavoritesImporter.ExtractRoomNames(FavoritesHtml);
            Assert.DoesNotContain("tags", names);
            Assert.DoesNotContain("accounts", names);
            Assert.DoesNotContain("external_link", names);
        }

        [Fact]
        public void ReturnsNothingWhenThePageHasNoRoom()
        {
            Assert.Empty(FavoritesImporter.ExtractRoomNames("<html><body>rien</body></html>"));
        }

        /// <summary>
        /// Une session expirée renvoie la page de connexion avec un code HTTP
        /// 200. Sans cette détection, l'import annoncerait « aucun favori » à
        /// quelqu'un qui doit en réalité se reconnecter — le message enverrait
        /// chercher le problème au mauvais endroit.
        /// </summary>
        [Fact]
        public void DetectsTheLoginPageServedWithA200()
        {
            Assert.True(FavoritesImporter.LooksLikeLoginPage(
                "<form id=\"login_form\" action=\"/auth/login/\">"));
            Assert.False(FavoritesImporter.LooksLikeLoginPage(FavoritesHtml));
        }
    }
}
