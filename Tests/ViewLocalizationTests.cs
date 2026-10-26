using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Que les vues passent bien par la table de traduction.
    ///
    /// Le portage WinForms → WPF avait perdu ce câblage sur SOIXANTE ET UN
    /// libellés, répartis sur neuf vues. Rien ne le signalait : le build était
    /// vert, l'application s'affichait correctement, et le défaut ne se voyait
    /// qu'en cochant « Français » — c'est-à-dire jamais, l'application
    /// démarrant en français. Il a fallu une capture d'écran envoyée par le
    /// mainteneur pour que quatre accents manquants attirent l'œil sur le reste.
    ///
    /// Ces deux tests sont le filet. Le premier empêche qu'un libellé reparte
    /// en dur, le second qu'une clé mal orthographiée passe inaperçue.
    /// </summary>
    public class ViewLocalizationTests
    {
        /// <summary>
        /// `Content="Fermer"` ou `Text="Journaux"` : une valeur littérale
        /// commençant par une lettre. Les valeurs entre accolades sont des
        /// liaisons ou des extensions et sont donc ignorées.
        /// </summary>
        private static readonly Regex Litteral =
            new(@"(?:Content|Text)=""(?<v>[\p{L}][^""{]*)""", RegexOptions.Compiled);

        private static readonly Regex Cle =
            new(@"\{ui:Str\s+(?<k>[A-Za-z0-9_.]+)\s*\}", RegexOptions.Compiled);

        public static IEnumerable<object[]> Vues =>
            FichiersDeVue().Select(f => new object[] { Path.GetFileName(f) });

        /// <summary>
        /// Aucun libellé écrit en dur. Un texte visible qui ne passe pas par la
        /// table ne se traduira jamais — et, comme on vient de le voir, il peut
        /// aussi perdre ses accents sans que personne ne le remarque.
        /// </summary>
        [Theory]
        [MemberData(nameof(Vues))]
        public void AucunLibelleNEstEcritEnDur(string nom)
        {
            var contenu = File.ReadAllText(Chemin(nom));
            var trouves = Litteral.Matches(contenu)
                .Select(m => m.Groups["v"].Value)
                .ToList();

            Assert.True(trouves.Count == 0,
                $"{nom} — {trouves.Count} libellé(s) hors de la table de traduction : " +
                string.Join(" | ", trouves.Take(8)) +
                "\nUtiliser {ui:Str cle} et déclarer la chaîne dans UI/Localization.cs.");
        }

        /// <summary>
        /// Toute clé citée par le XAML existe. `Localization.Get` rend la CLÉ
        /// quand elle est inconnue : une faute de frappe n'échoue donc pas, elle
        /// affiche « settings.captureFoldr » à l'utilisateur. Visible, mais
        /// seulement si quelqu'un ouvre l'écran concerné.
        /// </summary>
        [Theory]
        [MemberData(nameof(Vues))]
        public void ToutesLesClesCiteesExistentDansLaTable(string nom)
        {
            var contenu = File.ReadAllText(Chemin(nom));
            var inconnues = Cle.Matches(contenu)
                .Select(m => m.Groups["k"].Value)
                .Distinct()
                .Where(k => !Localization.AllStrings.ContainsKey(k))
                .ToList();

            Assert.True(inconnues.Count == 0,
                $"{nom} — clé(s) absente(s) de la table : {string.Join(", ", inconnues)}");
        }

        /// <summary>
        /// Une vue qui cite des clés doit déclarer l'espace de noms qui les
        /// résout. L'oubli casse au CHARGEMENT de la vue, pas au build.
        /// </summary>
        [Theory]
        [MemberData(nameof(Vues))]
        public void UneVueQuiCiteDesClesDeclareLEspaceDeNoms(string nom)
        {
            var contenu = File.ReadAllText(Chemin(nom));
            if (!Cle.IsMatch(contenu)) return;

            Assert.Contains("clr-namespace:ChaturbateRecorderApp.UI", contenu);
        }

        /// <summary>
        /// Sans cette garde, un chemin cassé rendrait les trois précédents verts
        /// en n'examinant aucun fichier.
        /// </summary>
        [Fact]
        public void LesVuesSontBienTrouvees()
        {
            var vues = FichiersDeVue();
            Assert.True(vues.Count >= 9, $"{vues.Count} vue(s) trouvée(s), 9 attendues au moins.");
            Assert.Contains(vues, v => Path.GetFileName(v) == "SettingsView.xaml");
        }

        private static string Chemin(string nom) =>
            FichiersDeVue().Single(f => Path.GetFileName(f) == nom);

        private static IReadOnlyList<string> FichiersDeVue()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d is not null)
            {
                var vues = Path.Combine(d.FullName, "Views");
                if (Directory.Exists(vues))
                {
                    var fichiers = Directory.GetFiles(vues, "*.xaml");
                    if (fichiers.Length > 0) return fichiers;
                }
                d = d.Parent;
            }
            return Array.Empty<string>();
        }
    }
}
