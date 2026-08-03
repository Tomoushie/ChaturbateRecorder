using System;
using System.Linq;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.UI;
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

        /// <summary>
        /// Délègue à la comparaison de production plutôt que d'en garder une
        /// deuxième ici : deux implémentations d'un même ordre finissent
        /// toujours par diverger. Elle est elle-même couverte par
        /// VersionsAreComparedNumericallyNotAlphabetically.
        /// </summary>
        private static bool IsAtLeast(string version, string floor) =>
            Changelog.CompareVersions(version, floor) >= 0;

        private static string[] AnnouncedVersions(string? since, string upTo, bool english = false) =>
            Changelog.GetChangesSince(since, upTo, english).Select(e => e.Version).ToArray();

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

        /// <summary>
        /// Le piège de toute comparaison de versions faite sur des chaînes :
        /// "1.10.0" est alphabétiquement AVANT "1.9.0". Une plage calculée
        /// ainsi annoncerait les mauvaises versions sans rien casser d'autre.
        /// </summary>
        [Fact]
        public void VersionsAreComparedNumericallyNotAlphabetically()
        {
            Assert.True(Changelog.CompareVersions("1.10.0", "1.9.0") > 0);
            Assert.True(Changelog.CompareVersions("1.9.0", "1.10.0") < 0);
            Assert.True(Changelog.CompareVersions("1.15.1", "1.15.0") > 0);
            Assert.Equal(0, Changelog.CompareVersions("1.17.0", "1.17.0"));

            // Une version illisible ne doit pas faire lever la comparaison.
            Assert.True(Changelog.CompareVersions("pas une version", "1.0.0") < 0);
            Assert.True(Changelog.CompareVersions("1.0.0", null) > 0);
            Assert.Equal(0, Changelog.CompareVersions(null, "n'importe quoi"));
        }

        /// <summary>
        /// CompareVersions classe une version illisible tout en bas : une
        /// entrée mal orthographiée serait donc annoncée dans le désordre au
        /// lieu de faire lever quoi que ce soit. Vérifié ici plutôt que
        /// découvert dans un dialogue "Nouveautés".
        /// </summary>
        [Fact]
        public void EveryEntryVersionIsParsable()
        {
            foreach (var entry in Changelog.Entries)
            {
                Assert.True(System.Version.TryParse(entry.Version, out _),
                    $"'{entry.Version}' n'est pas un numéro de version analysable.");
            }
        }

        /// <summary>
        /// Le cas qui a motivé GetChangesSince : un utilisateur resté en 1.14.0
        /// qui installe la 1.17.0 doit voir les quatre versions intermédiaires.
        /// N'annoncer que la 1.17.0 lui cachait le Crash Reporter (1.15.0) et
        /// le mode Diagnostic (1.16.0), pourtant déjà installés.
        /// </summary>
        [Fact]
        public void UpdatingAcrossSeveralVersionsAnnouncesThemAllNewestFirst()
        {
            Assert.Equal(
                new[] { "1.17.0", "1.16.0", "1.15.1", "1.15.0", "1.14.1" },
                AnnouncedVersions("1.14.0", "1.17.0"));
        }

        [Fact]
        public void TheLastSeenVersionIsExcludedAndTheNewOneIncluded()
        {
            var announced = AnnouncedVersions("1.16.0", "1.17.0");

            Assert.Equal(new[] { "1.17.0" }, announced);
        }

        /// <summary>
        /// L'ordre décroissant doit être numérique jusque dans le résultat :
        /// une implémentation par comparaison de chaînes intercalerait 1.10.0
        /// entre 1.1.0 et 1.2.0.
        /// </summary>
        [Fact]
        public void AnnouncedVersionsAreSortedNumerically()
        {
            Assert.Equal(
                new[] { "1.11.0", "1.10.0", "1.9.0" },
                AnnouncedVersions("1.8.0", "1.11.0"));
        }

        [Fact]
        public void EachAnnouncedVersionCarriesItsOwnChanges()
        {
            foreach (var (version, changes) in Changelog.GetChangesSince("1.14.0", "1.17.0", english: false))
            {
                Assert.Equal(Changelog.GetChanges(version, english: false), changes);
                Assert.NotEmpty(changes);
            }
        }

        /// <summary>
        /// Une plage à cheval sur 1.16.0 mélange forcément les langues : c'est
        /// le repli voulu (voir le commentaire de classe de Changelog), pas un
        /// bug à corriger en traduisant l'historique ancien.
        /// </summary>
        [Fact]
        public void AnEnglishAnnouncementFallsBackToFrenchForOldEntriesOnly()
        {
            var announced = Changelog.GetChangesSince("1.15.0", "1.17.0", english: true);

            Assert.Equal(new[] { "1.17.0", "1.16.0", "1.15.1" }, announced.Select(e => e.Version).ToArray());

            string[] ChangesFor(string version) => announced.First(e => e.Version == version).Changes;

            // 1.16.0+ : réellement traduites.
            Assert.NotEqual(Changelog.GetChanges("1.17.0", english: false), ChangesFor("1.17.0"));
            Assert.NotEqual(Changelog.GetChanges("1.16.0", english: false), ChangesFor("1.16.0"));

            // 1.15.1 : antérieure à la traduction, donc le texte français.
            Assert.Equal(Changelog.GetChanges("1.15.1", english: false), ChangesFor("1.15.1"));
        }

        /// <summary>
        /// Borne basse inutilisable (null jamais atteint en pratique — le
        /// premier lancement affiche le tutoriel — mais aussi settings.json
        /// édité à la main) : montrer la seule version courante, surtout pas
        /// les dix-huit entrées de l'historique dans une MessageBox.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("pas une version")]
        public void AnUnusableLastSeenVersionFallsBackToTheCurrentVersionAlone(string? lastSeen)
        {
            Assert.Equal(new[] { CurrentVersion }, AnnouncedVersions(lastSeen, CurrentVersion));
        }

        /// <summary>
        /// Retour à une version antérieure (réinstallation d'une release plus
        /// ancienne) : la plage est vide, mais le dialogue doit quand même
        /// annoncer la version installée — comportement d'avant ce changement.
        /// </summary>
        [Fact]
        public void DowngradingStillAnnouncesTheInstalledVersion()
        {
            Assert.Equal(new[] { "1.14.0" }, AnnouncedVersions("1.17.0", "1.14.0"));
        }

        [Fact]
        public void ReinstallingTheSameVersionAnnouncesItOnce()
        {
            Assert.Equal(new[] { "1.16.0" }, AnnouncedVersions("1.16.0", "1.16.0"));
        }

        /// <summary>
        /// Version installée sans entrée de changelog (bump oublié dans un
        /// build de développement) : ce qui la précède reste légitimement neuf
        /// pour l'utilisateur et doit être annoncé. Ce n'est que privé aussi de
        /// borne basse exploitable qu'il ne reste rien à dire — et le dialogue
        /// affiche alors "aucun détail" plutôt qu'un corps vide.
        /// </summary>
        [Fact]
        public void AnUnknownCurrentVersionStillAnnouncesWhatCameBeforeIt()
        {
            Assert.Equal(
                new[] { "1.16.0", "1.15.1" },
                AnnouncedVersions("1.15.0", "1.16.5"));

            Assert.Empty(Changelog.GetChangesSince(null, "42.0.0", english: true));
        }

        /// <summary>
        /// Garde-fou sur le cas réel du dialogue : l'annonce ne doit jamais
        /// remonter au-delà de la version installée, même si l'historique
        /// contient des entrées postérieures (release préparée en avance).
        /// </summary>
        [Fact]
        public void NothingNewerThanTheInstalledVersionIsAnnounced()
        {
            foreach (var version in AnnouncedVersions("1.0.0", "1.13.0"))
            {
                Assert.True(Changelog.CompareVersions(version, "1.13.0") <= 0,
                    $"'{version}' est postérieure à la version installée et n'aurait pas dû être annoncée.");
            }
        }

        // --- Mise en forme du dialogue (ChangelogForm.BuildLines) ---
        //
        // Assertions volontairement structurelles (en-têtes, puces, ordre) et
        // jamais sur du texte traduit : Localization.Current est un état
        // statique global qu'un autre test peut basculer en parallèle.

        /// <summary>
        /// Cas courant (mise à jour d'une version à la suivante) : une simple
        /// liste de puces. Un en-tête ne ferait que répéter le numéro de
        /// version déjà présent dans le titre de la fenêtre.
        /// </summary>
        [Fact]
        public void ASingleAnnouncedVersionIsRenderedWithoutAVersionHeader()
        {
            var lines = ChangelogForm.BuildLines(new[]
            {
                ("1.17.0", new[] { "Première nouveauté.", "Seconde nouveauté." }),
            });

            Assert.Equal(
                new[] { ("• Première nouveauté.", false), ("• Seconde nouveauté.", false) },
                lines);
        }

        /// <summary>
        /// Plusieurs versions : chaque groupe est précédé de son numéro, en
        /// gras, sinon l'utilisateur voit une liste de puces sans savoir
        /// laquelle vient de quelle version.
        /// </summary>
        [Fact]
        public void SeveralAnnouncedVersionsAreGroupedUnderTheirVersionHeader()
        {
            var lines = ChangelogForm.BuildLines(new[]
            {
                ("1.17.0", new[] { "Nouveauté récente." }),
                ("1.16.0", new[] { "Nouveauté intermédiaire." }),
            });

            Assert.Equal(
                new[]
                {
                    (Localization.Format("changelog.versionHeader", "1.17.0"), true),
                    ("• Nouveauté récente.", false),
                    (Localization.Format("changelog.versionHeader", "1.16.0"), true),
                    ("• Nouveauté intermédiaire.", false),
                },
                lines);
        }

        /// <summary>
        /// L'ordre du rendu suit celui de GetChangesSince (plus récent en
        /// premier) : c'est ce que voit l'utilisateur, et l'inverser mettrait
        /// les nouveautés les plus anciennes en tête de la fenêtre.
        /// </summary>
        [Fact]
        public void TheRenderedLinesKeepTheNewestVersionFirst()
        {
            var headers = ChangelogForm
                .BuildLines(Changelog.GetChangesSince("1.14.0", "1.17.0", english: false))
                .Where(l => l.IsHeader)
                .Select(l => l.Text)
                .ToArray();

            Assert.Equal(
                new[] { "1.17.0", "1.16.0", "1.15.1", "1.15.0", "1.14.1" }
                    .Select(v => Localization.Format("changelog.versionHeader", v))
                    .ToArray(),
                headers);
        }

        /// <summary>
        /// Rendu anglais d'une plage à cheval sur 1.16.0 : les entrées non
        /// traduites doivent apparaître telles quelles (en français) au lieu de
        /// laisser un groupe vide sous son en-tête.
        /// </summary>
        [Fact]
        public void AMixedLanguageAnnouncementRendersEveryVersionWithItsBullets()
        {
            var announced = Changelog.GetChangesSince("1.14.0", "1.17.0", english: true);
            var lines = ChangelogForm.BuildLines(announced);

            foreach (var (version, changes) in announced)
            {
                Assert.Contains((Localization.Format("changelog.versionHeader", version), true), lines);
                foreach (var change in changes)
                    Assert.Contains(("• " + change, false), lines);
            }

            // Aucune puce en trop ni en moins : une ligne par changement, plus
            // une par en-tête de version.
            Assert.Equal(
                announced.Sum(e => e.Changes.Length) + announced.Length,
                lines.Length);
        }

        [Fact]
        public void AnEmptyAnnouncementRendersTheNoDetailsMessageInsteadOfNothing()
        {
            var lines = ChangelogForm.BuildLines(Array.Empty<(string, string[])>());

            var line = Assert.Single(lines);
            Assert.False(line.IsHeader);
            Assert.False(string.IsNullOrWhiteSpace(line.Text));
            Assert.DoesNotContain("•", line.Text);
        }

        /// <summary>
        /// Une version sans aucune puce (entrée ajoutée au changelog mais
        /// laissée vide) ne doit pas produire un en-tête suivi de rien.
        /// </summary>
        [Fact]
        public void AVersionWithoutBulletsStillSaysSomethingUnderItsHeader()
        {
            var lines = ChangelogForm.BuildLines(new[]
            {
                ("1.17.0", new[] { "Une nouveauté." }),
                ("1.16.0", Array.Empty<string>()),
            });

            Assert.Equal(4, lines.Length);
            Assert.True(lines[2].IsHeader);
            Assert.False(lines[3].IsHeader);
            Assert.False(string.IsNullOrWhiteSpace(lines[3].Text));
        }
    }
}
