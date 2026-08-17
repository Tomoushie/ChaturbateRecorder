using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Les gabarits du thème, lus comme du XML.
    ///
    /// **UN `Setter` DE STYLE NE TRAVERSE PAS LE `ControlTemplate`.** Poser
    /// `BorderBrush` sur le `Button` ne peint rien si le `Border` du gabarit ne
    /// le reprend pas : la propriété du contrôle et celle de l'élément dessiné
    /// sont deux propriétés différentes. C'est la cinquième occurrence de la
    /// famille « ce que le compilateur ne voit pas » dans ce projet, et celle-ci
    /// a été trouvée sur une CAPTURE D'ÉCRAN, pas par un test :
    ///
    /// - les trois gabarits de bouton réservaient `BorderThickness="1"` sans
    ///   jamais lier `BorderBrush` — un trait d'un pixel peint avec rien. Sur le
    ///   bouton secondaire, dont le fond est proche de celui de la carte, il ne
    ///   restait aucune affordance : « Parcourir... » et « Diagnostic... »
    ///   s'affichaient en texte nu ;
    /// - le gabarit du champ de saisie faisait l'inverse, liant `BorderBrush`
    ///   sans jamais poser `BorderThickness`, dont le défaut est 0. Les champs
    ///   n'avaient donc aucun cadre.
    ///
    /// Le build était vert dans les deux cas, et le restera. D'où ce test, qui
    /// ne vérifie pas quatre corrections mais l'INVARIANT : dans un gabarit, les
    /// deux moitiés d'une bordure se déclarent ensemble ou pas du tout.
    /// </summary>
    public class ThemeTemplateTests
    {
        private const string Thickness = "BorderThickness";
        private const string Brush = "BorderBrush";

        public static IEnumerable<object[]> Dictionnaires =>
            FichiersDeTheme().Select(f => new object[] { Path.GetFileName(f) });

        [Theory]
        [MemberData(nameof(Dictionnaires))]
        public void UneBordureDeGabaritDeclareSesDeuxMoities(string nom)
        {
            var chemin = FichiersDeTheme().Single(f => Path.GetFileName(f) == nom);
            var doc = XDocument.Load(chemin, LoadOptions.SetLineInfo);

            var manquantes = new List<string>();

            foreach (var border in doc.Descendants().Where(e => e.Name.LocalName == "Border"))
            {
                var epaisseur = border.Attribute(Thickness)?.Value;
                var pinceau = border.Attribute(Brush)?.Value;

                // Ni l'un ni l'autre : aucune bordure voulue, rien à dire.
                if (epaisseur is null && pinceau is null) continue;

                // Une épaisseur explicitement nulle est un choix, pas un oubli.
                if (epaisseur is not null && EstNulle(epaisseur)) continue;

                var ligne = ((System.Xml.IXmlLineInfo)border).LineNumber;
                if (epaisseur is null)
                    manquantes.Add($"ligne {ligne} : {Brush} posé, {Thickness} absent (défaut 0, donc rien ne se dessine)");
                else if (pinceau is null)
                    manquantes.Add($"ligne {ligne} : {Thickness}=\"{epaisseur}\" réservé, {Brush} absent (trait peint avec rien)");
            }

            Assert.True(manquantes.Count == 0,
                $"{nom} — une bordure de gabarit ne déclare qu'une de ses deux moitiés. " +
                "Poser l'autre sur le Style ne suffit PAS : un Setter ne traverse pas le " +
                "ControlTemplate.\n  " + string.Join("\n  ", manquantes));
        }

        /// <summary>
        /// Le test ne vaut que s'il a vu les fichiers. Sans cette garde, un
        /// chemin cassé le rendrait vert en n'examinant rien — précisément le
        /// genre de faux vert que ce projet vient de payer une session entière.
        /// </summary>
        [Fact]
        public void LesDictionnairesDeThemeSontBienTrouves()
        {
            var fichiers = FichiersDeTheme();
            Assert.NotEmpty(fichiers);
            Assert.Contains(fichiers, f => Path.GetFileName(f) == "Controls.xaml");
            Assert.All(fichiers, f => Assert.NotEmpty(XDocument.Load(f).Descendants().ToList()));
        }

        private static bool EstNulle(string epaisseur) =>
            epaisseur.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                     .All(p => double.TryParse(p, System.Globalization.NumberStyles.Any,
                                               System.Globalization.CultureInfo.InvariantCulture, out var v) && v == 0);

        /// <summary>
        /// Les `.xaml` sont compilés en ressources et non copiés à côté du
        /// binaire : on remonte donc jusqu'au dossier `Themes\` des SOURCES.
        /// </summary>
        private static IReadOnlyList<string> FichiersDeTheme()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d is not null)
            {
                var themes = Path.Combine(d.FullName, "Themes");
                if (Directory.Exists(themes))
                {
                    var fichiers = Directory.GetFiles(themes, "*.xaml");
                    if (fichiers.Length > 0) return fichiers;
                }
                d = d.Parent;
            }
            return Array.Empty<string>();
        }
    }
}
