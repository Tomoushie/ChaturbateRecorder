using System.Drawing;
using System.Windows.Forms;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le texte d'annonce du complément payant tient-il dans son cadre ?
    ///
    /// **Un Label ne signale jamais qu'il tronque.** Il coupe, et seule une
    /// capture prise dans la bonne langue le montre. Le premier jet de ce
    /// panneau réservait 76 px : l'anglais y tenait tout juste, le FRANÇAIS en
    /// demandait 95 et se faisait couper — c'est-à-dire que le défaut visait la
    /// langue principale, et qu'une capture en anglais l'aurait déclaré bon.
    ///
    /// Ce test mesure les DEUX langues. Il échouera si quelqu'un rallonge le
    /// texte sans agrandir le cadre, ce qui est exactement le moment où le
    /// défaut reviendrait.
    /// </summary>
    public class ProPanelTextTests
    {
        // Doivent suivre MainForm : proLabel = 636 x 110.
        private const int LargeurUtile = 636;
        private const int HauteurDisponible = 110;

        [Theory]
        [InlineData("pro.body", AppLanguage.French)]
        [InlineData("pro.body", AppLanguage.English)]
        [InlineData("pro.owned", AppLanguage.French)]
        [InlineData("pro.owned", AppLanguage.English)]
        public void TheAnnouncementFitsItsLabelInBothLanguages(string cle, AppLanguage langue)
        {
            var texte = Localization.Get(cle, langue);
            using var police = new Font("Segoe UI", 10F);

            var hauteur = TextRenderer.MeasureText(
                texte, police, new Size(LargeurUtile, 0), TextFormatFlags.WordBreak).Height;

            Assert.True(hauteur <= HauteurDisponible,
                $"« {cle} » en {langue} demande {hauteur} px pour {HauteurDisponible} disponibles : il sera tronqué sans le dire.");
        }

        /// <summary>
        /// L'annonce ne doit jamais promettre de date. Le mainteneur l'a tranché
        /// — le chantier est long, et une date manquée coûte plus cher que pas
        /// de date du tout. Un test plutôt qu'un commentaire, parce que c'est le
        /// genre de promesse qu'on ajoute sans y penser en reformulant.
        /// </summary>
        [Theory]
        [InlineData(AppLanguage.French)]
        [InlineData(AppLanguage.English)]
        public void TheAnnouncementPromisesNoDate(AppLanguage langue)
        {
            var texte = Localization.Get("pro.body", langue).ToLowerInvariant();

            foreach (var promesse in new[]
            {
                "2026", "2027", "bientôt", "soon", "prochainement",
                "semaine", "week", "mois prochain", "next month",
            })
            {
                Assert.DoesNotContain(promesse, texte);
            }
        }
    }
}
