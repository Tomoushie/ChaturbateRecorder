using SentinelGuard;
using Xunit;

namespace SentinelGuard.Tests
{
    public class UrlValidatorTests
    {
        private static readonly string[] Whitelist = { "chaturbate.com" };
        private static readonly string[] Blacklist = { "example.com", "badhost.org" };

        [Theory]
        [InlineData("https://chaturbate.com/someroom/")]
        [InlineData("https://fr.chaturbate.com/someroom/")]
        [InlineData("https://chaturbate.com/some-room_42/")]
        public void IsSafeUrl_AcceptsWellFormedWhitelistedUrls(string url)
        {
            Assert.True(UrlValidator.IsSafeUrl(url, Whitelist, Blacklist));
        }

        [Fact]
        public void IsSafeUrl_RejectsEmptyUrl()
        {
            Assert.False(UrlValidator.IsSafeUrl("", Whitelist, Blacklist));
        }

        [Theory]
        [InlineData("http://chaturbate.com/someroom/")]     // http non https
        [InlineData("javascript:alert(1)")]                  // schéma interdit
        [InlineData("file:///etc/passwd")]                   // schéma interdit
        [InlineData("ftp://chaturbate.com/someroom/")]        // schéma interdit
        public void IsSafeUrl_RejectsForbiddenOrNonHttpsSchemes(string url)
        {
            Assert.False(UrlValidator.IsSafeUrl(url, Whitelist, Blacklist));
        }

        [Fact]
        public void IsSafeUrl_RejectsEmbeddedCredentials()
        {
            Assert.False(UrlValidator.IsSafeUrl("https://user:pass@chaturbate.com/someroom/", Whitelist, Blacklist));
        }

        [Theory]
        [InlineData("https://www.youtube.com/@SomeChannel/live")]
        [InlineData("https://www.tiktok.com/@some.account/live")]
        public void IsSafeUrl_AcceptsHandleSegments(string url)
        {
            // '@' dans un SEGMENT est la forme des identifiants de chaîne chez
            // YouTube, TikTok ou Mastodon. Les refuser rendait ces plateformes
            // inatteignables (constaté en implémentant leur prise en charge).
            var whitelist = new[] { "youtube.com", "tiktok.com" };
            Assert.True(UrlValidator.IsSafeUrl(url, whitelist, Blacklist));
        }

        [Theory]
        [InlineData("https://user:pass@www.youtube.com/@SomeChannel/live")]
        [InlineData("https://admin@www.youtube.com/@SomeChannel/live")]
        public void IsSafeUrl_StillRejectsCredentialsEvenWithHandleSegments(string url)
        {
            // Le garde-fou qui compte, maintenant que '@' est accepté dans un
            // segment : le '@' de l'AUTORITÉ reste refusé. Une autorité se
            // termine au premier '/', les deux cas ne peuvent pas se confondre,
            // et c'est Uri.UserInfo qui tranche — pas la forme du segment.
            var whitelist = new[] { "youtube.com" };
            Assert.False(UrlValidator.IsSafeUrl(url, whitelist, Blacklist));
        }

        [Theory]
        [InlineData("https://localhost/someroom/")]
        [InlineData("https://127.0.0.1/someroom/")]
        public void IsSafeUrl_RejectsLoopbackHosts(string url)
        {
            Assert.False(UrlValidator.IsSafeUrl(url, Whitelist, Blacklist));
        }

        [Fact]
        public void IsSafeUrl_RejectsNonWhitelistedDomain()
        {
            Assert.False(UrlValidator.IsSafeUrl("https://not-chaturbate.example.net/someroom/", Whitelist, Blacklist));
        }

        [Fact]
        public void IsSafeUrl_RejectsBlacklistedDomainEvenWithoutWhitelist()
        {
            Assert.False(UrlValidator.IsSafeUrl("https://example.com/someroom/", System.Array.Empty<string>(), Blacklist));
        }

        [Theory]
        [InlineData("https://chaturbate.com/some%20room/")]
        [InlineData("https://chaturbate.com/some;room/")]
        public void IsSafeUrl_RejectsUnsafePathSegments(string url)
        {
            Assert.False(UrlValidator.IsSafeUrl(url, Whitelist, Blacklist));
        }

        [Theory]
        [InlineData("..")]
        [InlineData("../")]
        [InlineData(".")]
        [InlineData("some;room")]
        [InlineData("some room")] // espace interdit
        public void IsSafePathSegment_RejectsTraversalAndUnsafeCharacters(string segment)
        {
            // Testé directement (pas via une URL complète) : System.Uri normalise
            // déjà "../" au niveau du parsing, donc IsSafeUrl ne reçoit jamais ce
            // segment tel quel — cette protection est une défense en profondeur
            // pour tout appelant qui passerait des segments par un autre chemin.
            Assert.False(UrlValidator.IsSafePathSegment(segment));
        }

        [Theory]
        [InlineData("someroom")]
        [InlineData("some-room_42")]
        public void IsSafePathSegment_AcceptsOrdinarySegments(string segment)
        {
            Assert.True(UrlValidator.IsSafePathSegment(segment));
        }

        [Fact]
        public void IsSafeUrl_RejectsOversizedQueryString()
        {
            var query = "?" + new string('a', 600);
            Assert.False(UrlValidator.IsSafeUrl($"https://chaturbate.com/room/{query}", Whitelist, Blacklist));
        }

        [Theory]
        [InlineData("chaturbate.com", true)]
        [InlineData("fr.chaturbate.com", true)]
        [InlineData("evilchaturbate.com", false)]   // ne doit PAS matcher juste parce que ça se termine par le suffixe sans le point
        [InlineData("chaturbate.com.evil.net", false)]
        public void IsDomainAllowed_MatchesExactAndSubdomainsOnly(string domain, bool expected)
        {
            Assert.Equal(expected, UrlValidator.IsDomainAllowed(domain, Whitelist, System.Array.Empty<string>()));
        }

        [Fact]
        public void IsSafeUrl_OutReason_IsPopulatedOnRejection()
        {
            Assert.False(UrlValidator.IsSafeUrl("", Whitelist, Blacklist, out var reason));
            Assert.False(string.IsNullOrEmpty(reason));
        }
    }
}
