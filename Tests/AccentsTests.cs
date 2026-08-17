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
    /// Le français affiché porte ses accents.
    ///
    /// Trois fois en deux jours, du texte visible est parti sans eux : quatre
    /// libellés du XAML des Réglages (« Legalite... »), un message de modèle de
    /// vue (« Reglages enregistres. »), puis la fenêtre Diagnostic ENTIÈRE —
    /// « Systeme », « Integrite », « Etat indisponible ». Aucun n'a été trouvé
    /// par un test : les deux premiers par une capture d'écran du mainteneur, le
    /// troisième par une autre capture, deux jours plus tard.
    ///
    /// Ce test ne sait pas lire le français. Il connaît une LISTE de mots dont
    /// la forme sans accent n'existe pas — « systeme », « reseau », « probleme ».
    /// C'est volontairement étroit : les formes ambiguës (« il installe », « il
    /// enregistre », « il autorise ») sont exclues, parce qu'un filet qui crie
    /// au loup finit par être désarmé.
    /// </summary>
    public class AccentsTests
    {
        /// <summary>
        /// Mots dont la version SANS accent n'est pas un mot français. Les
        /// formes verbales homographes sont écartées à dessein.
        /// </summary>
        private static readonly string[] SansAccentImpossible =
        {
            "systeme", "integrite", "execution", "reseau", "probleme",
            "demarrage", "legalite", "reglage", "joignabilite", "apercu",
            "deja", "tres", "apres", "precedent", "verifie", "verifiable",
            "elargi", "indetermine", "duree", "qualite", "securite",
            "parametre", "necessaire", "telecharge", "reussi", "echoue",
            "fenetre", "menage", "priorite", "operation", "acces",
        };

        private static readonly Regex Motif = new(
            @"\b(" + string.Join("|", SansAccentImpossible) + @")(s|e|es)?\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// La colonne FRANÇAISE de la table de traduction. C'est par elle que
        /// passe tout ce que l'application affiche depuis le câblage du 17-08.
        /// </summary>
        [Fact]
        public void LaTableDeTraductionEstAccentuee()
        {
            var fautes = Localization.AllStrings
                .Select(p => (p.Key, Faute: Motif.Match(p.Value.Fr)))
                .Where(x => x.Faute.Success)
                .Select(x => $"{x.Key} : « {x.Faute.Value} »")
                .ToList();

            Assert.True(fautes.Count == 0,
                "Des chaînes françaises sans accent :\n  " + string.Join("\n  ", fautes));
        }

        /// <summary>
        /// La fenêtre Diagnostic ne passe PAS par la table, et c'est un choix
        /// documenté : sa sortie est collée dans un ticket public, où des
        /// rapports en deux langues compliqueraient le dépouillement. Elle reste
        /// donc en français — raison de plus pour qu'il soit correct.
        ///
        /// Les motifs de refus venus de SentinelGuard restent ANGLAIS, eux aussi
        /// par décision : tout ce qui SORT de ce paquet s'écrit en anglais. Le
        /// mélange visible dans la ligne ACL n'est donc pas un défaut.
        /// </summary>
        [Fact]
        public void LeRapportDeDiagnosticEstAccentue()
        {
            var chemin = Remonter(Path.Combine("Services", "DiagnosticReport.cs"));
            var fautes = Litteraux(File.ReadAllText(chemin))
                .Select(l => (l, Faute: Motif.Match(l)))
                .Where(x => x.Faute.Success)
                .Select(x => $"« {x.Faute.Value} » dans {x.l}")
                .ToList();

            Assert.True(fautes.Count == 0,
                "DiagnosticReport.cs — français sans accent :\n  " + string.Join("\n  ", fautes));
        }

        /// <summary>
        /// Sans cette garde, les deux tests précédents passeraient en
        /// n'examinant rien — le faux vert que ce projet a déjà payé.
        /// </summary>
        [Fact]
        public void LeFiletExamineBienQuelqueChose()
        {
            Assert.True(Localization.AllStrings.Count > 100);
            Assert.NotEmpty(Litteraux(File.ReadAllText(
                Remonter(Path.Combine("Services", "DiagnosticReport.cs")))));

            // Et il DÉTECTE : un filet qu'on n'a jamais vu mordre ne prouve rien.
            Assert.Matches(Motif, "Integrite des binaires");
            Assert.Matches(Motif, "Systeme");
            // Sans crier au loup sur une forme verbale légitime.
            Assert.DoesNotMatch(Motif, "il installe le composant");
            Assert.DoesNotMatch(Motif, "il enregistre le salon");
        }

        /// <summary>Chaînes littérales d'un fichier C#, interpolations comprises.</summary>
        private static IEnumerable<string> Litteraux(string source) =>
            Regex.Matches(source, "\"([^\"\\n]{3,})\"")
                 .Select(m => m.Groups[1].Value)
                 // Les chemins, cles et arguments techniques ne sont pas du texte.
                 .Where(v => v.Contains(' ') && !v.StartsWith("--") && !v.Contains("://"));

        private static string Remonter(string relatif)
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d is not null)
            {
                var candidat = Path.Combine(d.FullName, relatif);
                if (File.Exists(candidat)) return candidat;
                d = d.Parent;
            }
            throw new FileNotFoundException(relatif);
        }
    }
}
