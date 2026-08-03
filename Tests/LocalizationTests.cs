using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Cohérence du dictionnaire de traduction (24.0). Le risque principal
    /// n'est pas une traduction maladroite (visible à l'œil) mais un trou de
    /// formatage désaccordé entre les deux langues : Format() lèverait alors
    /// une FormatException dans une seule des deux, cas qu'un test manuel en
    /// français ne révèle jamais.
    /// </summary>
    public class LocalizationTests
    {
        private static readonly Regex PlaceholderRegex = new(@"\{(\d+)\}", RegexOptions.Compiled);

        private static HashSet<int> Placeholders(string text) =>
            PlaceholderRegex.Matches(text).Select(m => int.Parse(m.Groups[1].Value)).ToHashSet();

        [Fact]
        public void FrenchAndEnglishUseTheSamePlaceholders()
        {
            foreach (var (key, pair) in Localization.AllStrings)
            {
                Assert.True(
                    Placeholders(pair.Fr).SetEquals(Placeholders(pair.En)),
                    $"Trous de formatage désaccordés pour la clé '{key}' : " +
                    $"FR={string.Join(",", Placeholders(pair.Fr).OrderBy(i => i))} / " +
                    $"EN={string.Join(",", Placeholders(pair.En).OrderBy(i => i))}");
            }
        }

        /// <summary>
        /// Les trous doivent être numérotés sans trou dans la séquence (0,1,2...) :
        /// un "{2}" sans "{1}" signifie qu'un argument attendu par string.Format
        /// ne sera jamais fourni par l'appelant.
        /// </summary>
        [Fact]
        public void PlaceholderIndicesAreContiguousFromZero()
        {
            foreach (var (key, pair) in Localization.AllStrings)
            {
                var indices = Placeholders(pair.Fr);
                if (indices.Count == 0) continue;

                Assert.True(
                    indices.SetEquals(Enumerable.Range(0, indices.Count)),
                    $"Indices non contigus pour la clé '{key}' : {string.Join(",", indices.OrderBy(i => i))}");
            }
        }

        [Fact]
        public void NoTranslationIsEmpty()
        {
            foreach (var (key, pair) in Localization.AllStrings)
            {
                Assert.False(string.IsNullOrWhiteSpace(pair.Fr), $"Traduction FR vide pour '{key}'.");
                Assert.False(string.IsNullOrWhiteSpace(pair.En), $"Traduction EN vide pour '{key}'.");
            }
        }

        [Fact]
        public void EveryKeyResolvesInBothLanguages()
        {
            foreach (var key in Localization.AllStrings.Keys)
            {
                Assert.NotEqual(key, Localization.Get(key, AppLanguage.French));
                Assert.NotEqual(key, Localization.Get(key, AppLanguage.English));
            }
        }

        [Fact]
        public void UnknownKeyFallsBackToTheKeyItself()
        {
            const string missing = "cette.cle.n.existe.pas";
            Assert.Equal(missing, Localization.Get(missing, AppLanguage.French));
            Assert.Equal(missing, Localization.Get(missing, AppLanguage.English));
        }

        [Fact]
        public void FormatUsesTheCurrentLanguage()
        {
            var previous = Localization.Current;
            try
            {
                Localization.Current = AppLanguage.French;
                Assert.Equal("Chemin de log invalide.", Localization.Get("error.invalidLogPath"));

                Localization.Current = AppLanguage.English;
                Assert.Equal("Invalid log path.", Localization.Get("error.invalidLogPath"));

                Assert.Equal("Cannot open the page: boom", Localization.Format("error.cannotOpenPage", "boom"));
            }
            finally
            {
                Localization.Current = previous;
            }
        }

        /// <summary>
        /// Toutes les chaînes du dictionnaire doivent survivre à string.Format
        /// avec le bon nombre d'arguments — attrape une accolade littérale non
        /// échappée ("{" seul), qui ferait échouer l'appel à l'exécution.
        /// </summary>
        [Fact]
        public void EveryStringSurvivesFormatting()
        {
            foreach (var (key, pair) in Localization.AllStrings)
            {
                var argCount = Placeholders(pair.Fr).Count;
                var args = Enumerable.Range(0, argCount).Select(i => (object)$"arg{i}").ToArray();

                var fr = Record.Exception(() => string.Format(pair.Fr, args));
                var en = Record.Exception(() => string.Format(pair.En, args));

                Assert.True(fr == null, $"Formatage FR impossible pour '{key}' : {fr?.Message}");
                Assert.True(en == null, $"Formatage EN impossible pour '{key}' : {en?.Message}");
            }
        }
    }
}
