using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Garde-fous de la prise en charge multi-plateformes (40.0). Tout ce qui
    /// est vérifié ici l'a été d'abord sur le vrai yt-dlp : les chaînes de
    /// RoomStatusCheckerTests sont ses messages réels, pas des messages
    /// plausibles.
    /// </summary>
    public class PlatformsTests
    {
        [Theory]
        [InlineData("https://chaturbate.com/lea_martin/", StreamPlatform.Chaturbate)]
        [InlineData("https://fr.chaturbate.com/lea_martin/", StreamPlatform.Chaturbate)]
        [InlineData("https://www.twitch.tv/somestreamer", StreamPlatform.Twitch)]
        [InlineData("https://m.twitch.tv/somestreamer", StreamPlatform.Twitch)]
        [InlineData("https://www.youtube.com/watch?v=abc123", StreamPlatform.YouTube)]
        [InlineData("https://youtu.be/abc123", StreamPlatform.YouTube)]
        [InlineData("https://www.tiktok.com/@someone/live", StreamPlatform.TikTok)]
        [InlineData("https://example.org/whatever", StreamPlatform.Unknown)]
        [InlineData("pas une url", StreamPlatform.Unknown)]
        public void Detect_RecognisesEachPlatform(string url, StreamPlatform expected)
        {
            Assert.Equal(expected, Platforms.Detect(url));
        }

        [Fact]
        public void Detect_IsNotFooledByALookalikeDomain()
        {
            // "twitch.tv.attaquant.com" se termine par ".com", pas par
            // ".twitch.tv" : la comparaison porte sur le SUFFIXE de domaine,
            // jamais sur une sous-chaîne.
            Assert.Equal(StreamPlatform.Unknown, Platforms.Detect("https://twitch.tv.attaquant.com/piege"));
            Assert.Equal(StreamPlatform.Unknown, Platforms.Detect("https://fauxtwitch.tv/piege"));
        }

        [Theory]
        [InlineData("https://chaturbate.com/lea_martin/", "lea_martin")]
        [InlineData("https://www.twitch.tv/somestreamer", "somestreamer")]
        [InlineData("https://www.tiktok.com/@some.account/live", "some.account")]
        [InlineData("https://www.youtube.com/@NASA/live", "NASA")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
        public void DisplayName_ExtractsAReadableName(string url, string expected)
        {
            Assert.Equal(expected, Platforms.DisplayName(url));
        }

        [Fact]
        public void DisplayName_DoesNotNameEveryYouTubeRecordingWatch()
        {
            // Le défaut que ce test existe pour empêcher : le premier segment
            // de "youtube.com/watch?v=ID" vaut "watch", donc TOUS les
            // enregistrements YouTube auraient porté le même nom de fichier.
            var name = Platforms.DisplayName("https://www.youtube.com/watch?v=M3HKLzjvKPc");
            Assert.Equal("M3HKLzjvKPc", name);
            Assert.NotEqual("watch", name);
        }

        [Theory]
        [InlineData("https://chaturbate.com/")]
        [InlineData("https://www.youtube.com/")]
        public void DisplayName_FallsBackToTheHostRatherThanEmpty(string url)
        {
            // Le nom sert de base au nom de FICHIER : une chaîne vide
            // produirait un fichier nommé "-2026-08-09_12-00-00.mp4".
            Assert.False(string.IsNullOrWhiteSpace(Platforms.DisplayName(url)));
        }

        [Fact]
        public void DisplayName_StripsWhatCannotGoInAFileName()
        {
            // Le nom vient d'une URL, donc de l'extérieur. PathValidator
            // refuserait le chemin de sortie plutôt que de le corriger : mieux
            // vaut un nom nettoyé qu'un enregistrement refusé.
            var name = Platforms.DisplayName("https://www.twitch.tv/nom%20avec%20espaces");
            Assert.DoesNotContain(" ", name);
            Assert.DoesNotContain("%", name);
            Assert.DoesNotContain("/", name);
        }

        [Fact]
        public void AllowedDomains_CoverEveryDeclaredPlatform()
        {
            // Garde-fou d'ajout : une plateforme reconnue par Detect mais
            // absente de la liste blanche serait détectée puis refusée par le
            // bac à sable d'URL — un échec incompréhensible côté utilisateur.
            foreach (var url in new[]
            {
                "https://chaturbate.com/x/",
                "https://www.twitch.tv/x",
                "https://www.youtube.com/@x/live",
                "https://youtu.be/x",
                "https://www.tiktok.com/@x/live",
            })
            {
                Assert.NotEqual(StreamPlatform.Unknown, Platforms.Detect(url));
                Assert.True(SentinelGuard.UrlValidator.IsSafeUrl(
                        url, Platforms.AllowedDomains, Config.AppConfig.Blacklist, out var reason),
                    $"{url} refusé par le bac à sable : {reason}");
            }
        }
    }
}
