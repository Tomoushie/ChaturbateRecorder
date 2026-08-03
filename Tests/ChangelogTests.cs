using System.Linq;
using ChaturbateRecorderApp.Config;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Cohérence de l'historique des versions et de sa traduction partielle
    /// (24.0) : seules les versions à partir de 1.16.0 sont traduites, les
    /// précédentes retombent volontairement sur le français.
    /// </summary>
    public class ChangelogTests
    {
        /// <summary>
        /// Première version couverte par la traduction anglaise. Sert de borne
        /// à FromTheCurrentVersionOnwardEveryEntryIsTranslated : en-dessous,
        /// l'absence de traduction est le comportement voulu.
        /// </summary>
        private const string FirstTranslatedVersion = "1.16.0";

        private static string CurrentVersion =>
            typeof(Changelog).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        private static int[] Parse(string version) => version.Split('.').Select(int.Parse).ToArray();

        private static bool IsAtLeast(string version, string floor)
        {
            var (a, b) = (Parse(version), Parse(floor));
            for (var i = 0; i < 3; i++)
            {
                if (a[i] != b[i]) return a[i] > b[i];
            }
            return true;
        }

        [Fact]
        public void EveryTranslatedVersionExistsInTheFrenchHistory()
        {
            var known = Changelog.Entries.Select(e => e.Version).ToHashSet();

            foreach (var version in Changelog.AllEnglishChanges.Keys)
            {
                Assert.True(known.Contains(version),
                    $"La version '{version}' est traduite en anglais mais absente de Changelog.Entries — " +
                    "faute de frappe ? Cette traduction ne serait jamais affichée.");
            }
        }

        /// <summary>
        /// Une traduction qui aurait perdu une puce en route ferait disparaître
        /// une fonctionnalité de l'annonce anglaise sans que rien ne le signale.
        /// </summary>
        [Fact]
        public void TranslatedVersionsKeepTheSameNumberOfBullets()
        {
            foreach (var (version, englishChanges) in Changelog.AllEnglishChanges)
            {
                var french = Changelog.Entries.First(e => e.Version == version).Changes;

                Assert.True(french.Length == englishChanges.Length,
                    $"Nombre de puces différent pour la version '{version}' : " +
                    $"FR={french.Length} / EN={englishChanges.Length}.");
            }
        }

        /// <summary>
        /// Encode la décision prise en 24.0 : l'historique ancien reste en
        /// français, mais toute version publiée à partir de 1.16.0 doit être
        /// traduite. Ce test échoue donc volontairement au prochain bump de
        /// version tant que la nouvelle entrée n'a pas sa traduction anglaise.
        /// </summary>
        [Fact]
        public void FromTheCurrentVersionOnwardEveryEntryIsTranslated()
        {
            foreach (var entry in Changelog.Entries.Where(e => IsAtLeast(e.Version, FirstTranslatedVersion)))
            {
                Assert.True(Changelog.AllEnglishChanges.ContainsKey(entry.Version),
                    $"La version '{entry.Version}' n'a pas de traduction anglaise. " +
                    "Depuis la v1.16.0, chaque nouvelle entrée de Changelog.Entries doit avoir " +
                    "son pendant dans EnglishChanges (voir Config/Changelog.cs).");
            }
        }

        [Fact]
        public void TheCurrentAssemblyVersionHasAChangelogEntry()
        {
            Assert.Contains(Changelog.Entries, e => e.Version == CurrentVersion);
        }

        [Fact]
        public void UntranslatedVersionsFallBackToFrench()
        {
            // 1.0.0 est antérieure à la traduction : demander l'anglais doit
            // rendre le texte français, pas une liste vide.
            var french = Changelog.GetChanges("1.0.0", english: false);
            var asked = Changelog.GetChanges("1.0.0", english: true);

            Assert.NotEmpty(asked);
            Assert.Equal(french, asked);
        }

        [Fact]
        public void TranslatedVersionActuallyReturnsEnglish()
        {
            var french = Changelog.GetChanges(FirstTranslatedVersion, english: false);
            var english = Changelog.GetChanges(FirstTranslatedVersion, english: true);

            Assert.NotEmpty(english);
            Assert.NotEqual(french, english);
        }

        [Fact]
        public void UnknownVersionReturnsEmptyInBothLanguages()
        {
            Assert.Empty(Changelog.GetChanges("42.0.0", english: false));
            Assert.Empty(Changelog.GetChanges("42.0.0", english: true));
        }

        [Fact]
        public void NoVersionIsListedTwice()
        {
            var versions = Changelog.Entries.Select(e => e.Version).ToList();
            Assert.Equal(versions.Count, versions.Distinct().Count());
        }
    }
}
