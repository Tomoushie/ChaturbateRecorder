using System.Windows.Forms;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Mise en page de la fenêtre « Remerciements » selon la présence d'une
    /// licence premium (97.0).
    ///
    /// **Pourquoi tester une mise en page** : la ligne de licence n'apparaît que
    /// pour les acheteurs, et le champ des noms se place différemment selon
    /// qu'elle est là ou non. Un décalage se verrait sur une capture — mais
    /// seulement sur celle du cas qu'on a pensé à capturer, et le développeur
    /// n'a jamais les deux états sous la main en même temps. Ici les deux sont
    /// mesurés côte à côte.
    ///
    /// La contrainte qui compte n'est pas la position exacte, c'est que le champ
    /// des noms se termine TOUJOURS avant la ligne d'état, dans les deux cas.
    /// </summary>
    public class SupportersLayoutTests
    {
        private static (Control Licence, Control Noms, Control Etat) Trouver(SupportersForm form)
        {
            Control? licence = null, noms = null, etat = null;
            foreach (Control c in form.Controls)
            {
                if (c is TextBox) noms = c;
                else if (c is Label l)
                {
                    // La ligne de licence est la seule à 22 px de haut ; l'état
                    // à 20. L'intro et le consentement sont bien plus hauts.
                    if (l.Height == 22) licence = l;
                    else if (l.Height == 20) etat = l;
                }
            }
            Assert.NotNull(licence);
            Assert.NotNull(noms);
            Assert.NotNull(etat);
            return (licence!, noms!, etat!);
        }

        // NE PAS tester `Control.Visible` ici : WinForms y renvoie la visibilité
        // EFFECTIVE, qui est fausse tant que la fenêtre parente n'est pas
        // affichée — donc toujours fausse dans un test. Le premier jet de ces
        // tests l'a appris à ses dépens : le cas « sans licence » passait au
        // vert et le cas « avec licence » échouait, alors que les deux
        // mesuraient la même chose, c'est-à-dire rien. Le texte, lui, est
        // observable : vide = rien d'affiché.

        [Fact]
        public void WithoutALicenceTheLineIsEmptyAndTakesNoRoom()
        {
            using var form = new SupportersForm(AppTheme.Light, AppLanguage.French);
            var (licence, noms, etat) = Trouver(form);

            Assert.Equal("", licence.Text);
            // Le champ des noms remonte à la place qu'aurait prise la ligne :
            // réserver ce vide chez la quasi-totalité des utilisateurs, qui
            // n'ont pas de licence, serait un défaut servi au plus grand nombre.
            Assert.Equal(92, noms.Top);
            Assert.True(noms.Bottom <= etat.Top,
                $"le champ des noms ({noms.Bottom}) déborde sur la ligne d'état ({etat.Top})");
        }

        [Fact]
        public void WithALicenceTheBuyerIsNamedAndNothingOverlaps()
        {
            using var form = new SupportersForm(AppTheme.Light, AppLanguage.French, "Jane Doe");
            var (licence, noms, etat) = Trouver(form);

            Assert.Contains("Jane Doe", licence.Text);

            Assert.True(licence.Bottom <= noms.Top,
                $"la ligne de licence ({licence.Bottom}) recouvre le champ des noms ({noms.Top})");
            Assert.True(noms.Bottom <= etat.Top,
                $"le champ des noms ({noms.Bottom}) déborde sur la ligne d'état ({etat.Top})");
        }

        /// <summary>
        /// L'invariant qui tient les deux cas ensemble : quelle que soit la
        /// licence, le bas du champ des noms ne bouge pas. C'est lui qui garantit
        /// que rien ne peut glisser sous la ligne d'état en ajoutant une ligne.
        /// </summary>
        [Fact]
        public void TheNamesFieldEndsAtTheSamePlaceInBothCases()
        {
            using var sans = new SupportersForm(AppTheme.Light, AppLanguage.French);
            using var avec = new SupportersForm(AppTheme.Light, AppLanguage.French, "Jane Doe");

            Assert.Equal(Trouver(sans).Noms.Bottom, Trouver(avec).Noms.Bottom);
        }

        /// <summary>
        /// Le nom vient d'un fichier signé, mais reste du texte extérieur : il
        /// ne doit ni casser le formatage ni faire lever la fenêtre.
        /// </summary>
        [Fact]
        public void AnUnusualNameDoesNotBreakTheWindow()
        {
            using var form = new SupportersForm(AppTheme.Light, AppLanguage.English,
                "Amélie {0} Réaumur & Co. <script>");
            var (licence, _, _) = Trouver(form);

            Assert.Contains("Réaumur", licence.Text);
            // Le « {0} » du nom ne doit pas être réinterprété comme un trou de
            // formatage : Format() est appelé UNE fois, avec le nom en argument.
            Assert.Contains("{0}", licence.Text);
        }
    }
}
