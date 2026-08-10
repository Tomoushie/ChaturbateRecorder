using System.Linq;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Assainissement de la liste des donateurs (104.0).
    ///
    /// Ce qui est éprouvé ici n'est pas « la liste s'affiche » mais ce qui
    /// arrive quand le fichier du site contient autre chose que ce qu'on
    /// attend : c'est le seul texte de l'application qui vienne du réseau, et
    /// il est affiché tel quel dans une fenêtre.
    /// </summary>
    public class SupportersTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\r\n")]
        public void EmptyOrBlankNamesAreDropped(string? raw)
        {
            Assert.Null(SupportersProvider.Clean(raw));
        }

        [Fact]
        public void SurroundingAndRepeatedWhitespaceIsCollapsed()
        {
            Assert.Equal("Jean Dupont", SupportersProvider.Clean("   Jean     Dupont  "));
        }

        /// <summary>
        /// Un saut de ligne dans un nom fabriquerait deux entrées visuelles à
        /// partir d'une seule : le TextBox de la fenêtre affiche une ligne par
        /// nom, il n'a aucun moyen de distinguer les deux cas.
        /// </summary>
        [Fact]
        public void ControlCharactersCannotForgeExtraEntries()
        {
            Assert.Equal("Alice Bob", SupportersProvider.Clean("Alice\r\nBob"));
            Assert.Equal("Alice", SupportersProvider.Clean("Ali\0ce"));
        }

        /// <summary>
        /// U+202E (RIGHT-TO-LEFT OVERRIDE) inverse le rendu de ce qui suit :
        /// un nom peut donc s'afficher tout autrement qu'il n'est écrit. Même
        /// famille pour les espaces de largeur nulle, invisibles mais qui
        /// déjouent la déduplication.
        /// </summary>
        [Fact]
        public void DirectionAndZeroWidthMarksAreStripped()
        {
            // Points de code écrits en clair et non collés tels quels : ces
            // caractères sont invisibles dans un éditeur, donc un copier-coller
            // malheureux les ferait disparaître du test sans le faire échouer —
            // il ne prouverait alors plus rien.
            var rlo = ((char)0x202E).ToString();  // RIGHT-TO-LEFT OVERRIDE
            var zwsp = ((char)0x200B).ToString(); // ZERO WIDTH SPACE
            var bom = ((char)0xFEFF).ToString();  // ZERO WIDTH NO-BREAK SPACE

            Assert.Equal("Alice", SupportersProvider.Clean("Ali" + rlo + "ce"));
            Assert.Equal("Alice", SupportersProvider.Clean(zwsp + "Alice" + zwsp));
            Assert.Equal("Alice", SupportersProvider.Clean("Ali" + bom + "ce"));
        }

        [Fact]
        public void EmojiSurvive()
        {
            // Une paire de substituts ne doit être ni coupée en deux ni prise
            // pour un caractère de catégorie Surrogate à supprimer.
            Assert.Equal("Alice \U0001F600", SupportersProvider.Clean("Alice \U0001F600"));
        }

        [Fact]
        public void OverlongNamesAreTruncatedNotDropped()
        {
            var cleaned = SupportersProvider.Clean(new string('a', 200));
            Assert.NotNull(cleaned);
            Assert.Equal(SupportersProvider.MaxNameLength, cleaned!.Length);
        }

        [Fact]
        public void DuplicatesAreRemovedIgnoringCase()
        {
            var names = SupportersProvider.Clean(new[] { "Alice", "ALICE", "alice " });
            Assert.Equal(new[] { "Alice" }, names);
        }

        /// <summary>
        /// Le tri n'est pas cosmétique : sans lui, l'ordre du fichier finirait
        /// par refléter la chronologie ou les montants, ce que la demande
        /// exclut explicitement.
        /// </summary>
        [Fact]
        public void NamesAreSortedAlphabetically()
        {
            var names = SupportersProvider.Clean(new[] { "Zoé", "alice", "Bob" });
            Assert.Equal(new[] { "alice", "Bob", "Zoé" }, names);
        }

        [Fact]
        public void ListIsCappedAtMaxNames()
        {
            var raw = Enumerable.Range(0, SupportersProvider.MaxNames + 50).Select(i => $"donateur{i:D4}");
            Assert.Equal(SupportersProvider.MaxNames, SupportersProvider.Clean(raw).Count);
        }

        [Fact]
        public void ParsesTheObjectFormUsedBySite()
        {
            var names = SupportersProvider.ParseJson("""{"updated":"2026-08-10","supporters":["Alice","Bob"]}""");
            Assert.Equal(new[] { "Alice", "Bob" }, names);
        }

        [Fact]
        public void ParsesABareArray()
        {
            Assert.Equal(new[] { "Alice" }, SupportersProvider.ParseJson("""["Alice"]"""));
        }

        [Fact]
        public void NonStringEntriesAreIgnoredRatherThanFailingTheWholeFile()
        {
            var names = SupportersProvider.ParseJson("""{"supporters":["Alice",42,null,{"nom":"Bob"}]}""");
            Assert.Equal(new[] { "Alice" }, names);
        }

        /// <summary>
        /// null et non une liste vide : un fichier illisible ou d'une forme
        /// inattendue ne doit pas EFFACER la liste embarquée, seulement laisser
        /// l'application s'en tenir à elle.
        /// </summary>
        [Theory]
        [InlineData("pas du json")]
        [InlineData("{}")]
        [InlineData("""{"supporters":"Alice"}""")]
        [InlineData("42")]
        public void MalformedOrUnexpectedDocumentsYieldNull(string json)
        {
            Assert.Null(SupportersProvider.ParseJson(json));
        }

        /// <summary>
        /// Le fichier du dépôt doit rester lisible par le code qui le
        /// consomme : c'est le seul lien entre les deux, et il n'est vérifié
        /// nulle part ailleurs.
        /// </summary>
        [Fact]
        public void RepositoryJsonFileIsParseable()
        {
            var path = System.IO.Path.Combine(TestPaths.RepoRoot, "docs", "supporters.json");
            Assert.True(System.IO.File.Exists(path), $"Fichier introuvable : {path}");
            Assert.NotNull(SupportersProvider.ParseJson(System.IO.File.ReadAllText(path)));
        }
    }

    internal static class TestPaths
    {
        /// <summary>
        /// Remonte depuis le dossier de sortie des tests
        /// (Tests/bin/Debug/net10.0-windows) jusqu'à la racine du dépôt.
        /// Cherché plutôt que compté : le nombre de niveaux change avec la
        /// configuration et le TFM.
        /// </summary>
        internal static string RepoRoot
        {
            get
            {
                var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
                while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "ChaturbateRecorderApp.csproj")))
                    dir = dir.Parent;
                return dir?.FullName ?? System.AppContext.BaseDirectory;
            }
        }
    }
}
