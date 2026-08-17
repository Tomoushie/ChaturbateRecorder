using System;
using System.Collections.Generic;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le thème WPF, éprouvé sur ce qui a été DÉCIDÉ PAR MESURE.
    ///
    /// Les 26 couleurs de la palette ne sont pas des goûts : leurs contrastes
    /// ont été mesurés au seuil WCAG de 4,5 pendant la refonte 97.0, et deux
    /// candidates évidentes du thème clair y étaient tombées à 3,97 et 4,17.
    /// Sans ces tests, un ajustement « à l'œil » les repasserait sous le seuil
    /// sans que rien ne le signale — un texte illisible ne fait pas échouer un
    /// build.
    ///
    /// Ils ne touchent NI WPF NI l'application : `ThemeManager.GetPalette` et
    /// `ResolveButtonColors` sont des fonctions pures. C'est exactement pour
    /// cela qu'elles sont restées en C# au lieu de partir en déclencheurs XAML.
    /// </summary>
    public class ThemeWpfTests
    {
        public static IEnumerable<object[]> Themes =>
            new[] { new object[] { AppTheme.Light }, new object[] { AppTheme.Dark } };

        /// <summary>
        /// Contraste WCAG 2.x. Recopié du test WinForms plutôt qu'importé : le
        /// type couleur diffère (Media.Color et non Drawing.Color), et ajouter
        /// une dépendance pour six lignes d'arithmétique serait absurde.
        /// </summary>
        private static double Contraste(Color a, Color b)
        {
            static double Canal(int v)
            {
                var c = v / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            static double Luminance(Color c) =>
                0.2126 * Canal(c.R) + 0.7152 * Canal(c.G) + 0.0722 * Canal(c.B);

            var (l1, l2) = (Luminance(a), Luminance(b));
            if (l1 < l2) (l1, l2) = (l2, l1);
            return (l1 + 0.05) / (l2 + 0.05);
        }

        // --- Lisibilité, le seul critère qui compte vraiment ---

        [Theory]
        [MemberData(nameof(Themes))]
        public void LeTexteEstLisibleSurUneCarte(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);

            Assert.True(Contraste(p.Fg, p.Card) >= 4.5,
                $"Texte principal illisible en thème {theme} : {Contraste(p.Fg, p.Card):0.00}");
            Assert.True(Contraste(p.FgMuted, p.Card) >= 4.5,
                $"Libellé secondaire illisible en thème {theme} : {Contraste(p.FgMuted, p.Card):0.00}");
        }

        /// <summary>
        /// Success et Warning sont les deux couleurs ajoutées en 97.0, et les
        /// seules choisies PAR MESURE plutôt qu'à l'œil : les candidates
        /// évidentes du thème clair tombaient à 3,97 et 4,17.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LesCouleursDEtatSontLisiblesSurUneCarte(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);

            Assert.True(Contraste(p.Success, p.Card) >= 4.5,
                $"« En ligne » illisible en thème {theme} : {Contraste(p.Success, p.Card):0.00}");
            Assert.True(Contraste(p.Warning, p.Card) >= 4.5,
                $"« Reconnexion » illisible en thème {theme} : {Contraste(p.Warning, p.Card):0.00}");
            Assert.True(Contraste(p.Danger, p.Card) >= 4.5,
                $"« Échec » illisible en thème {theme} : {Contraste(p.Danger, p.Card):0.00}");
        }

        /// <summary>
        /// Le texte d'un bouton doit être lisible sur SON PROPRE fond, dans les
        /// trois rôles. C'est ici qu'un défaut réel a été trouvé côté WinForms :
        /// du blanc sur l'accent du thème sombre ne donnait que 3,2.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void ChaqueRoleDeBoutonResteLisible(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);

            foreach (var role in new[] { ButtonRole.Primary, ButtonRole.Secondary, ButtonRole.Danger })
            {
                var (fill, hover, pressed, _, fg) = ThemeManager.ResolveButtonColors(role, p);

                foreach (var (fond, etat) in new[] { (fill, "repos"), (hover, "survol"), (pressed, "appui") })
                {
                    var ratio = Contraste(fg, fond);
                    Assert.True(ratio >= 4.5,
                        $"Bouton {role} illisible au {etat} en thème {theme} : {ratio:0.00}");
                }
            }
        }

        // --- Les règles DÉRIVÉES, celles qu'un XAML aurait figées ---

        /// <summary>
        /// Survol et appui s'ÉLOIGNENT de la couleur du texte au lieu de
        /// s'éclaircir systématiquement. Sur l'accent clair du thème sombre,
        /// éclaircir encore effacerait le libellé : le contraste doit donc
        /// CROÎTRE d'un état à l'autre, jamais décroître.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LeContrasteNePeutQueSAmeliorerAuSurvolPuisALAppui(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);
            var (fill, hover, pressed, _, fg) = ThemeManager.ResolveButtonColors(ButtonRole.Primary, p);

            Assert.True(Contraste(fg, hover) >= Contraste(fg, fill),
                $"Le survol dégrade la lisibilité en thème {theme}.");
            Assert.True(Contraste(fg, pressed) >= Contraste(fg, hover),
                $"L'appui dégrade la lisibilité en thème {theme}.");
        }

        /// <summary>
        /// Danger ne REMPLIT PAS le bouton de rouge : « Stop », « Tout
        /// arrêter », « Supprimer favori » et « Ne plus surveiller » sont
        /// visibles en même temps, et quatre aplats rouges crieraient à
        /// l'écran. Le rouge ne porte que le texte et la bordure.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LeBoutonDangerNEstPasUnAplatRouge(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);
            var (fill, _, _, _, fg) = ThemeManager.ResolveButtonColors(ButtonRole.Danger, p);

            Assert.Equal(p.Neutral, fill);
            Assert.Equal(p.Danger, fg);
        }

        // --- L'interpolation, et son invariant oublié une fois ---

        /// <summary>
        /// **LES TREIZE COULEURS DOIVENT TOUTES ÊTRE INTERPOLÉES.** Success et
        /// Warning avaient été oubliées à leur ajout en 97.0, et elles
        /// SAUTAIENT au milieu du fondu au lieu de le suivre. Ce test compare
        /// chaque champ : en ajouter un à la palette sans l'ajouter à
        /// `LerpPalette` le fera échouer.
        /// </summary>
        [Fact]
        public void LInterpolationTraiteLesTreizeCouleurs()
        {
            var clair = ThemeManager.GetPalette(AppTheme.Light);
            var sombre = ThemeManager.GetPalette(AppTheme.Dark);
            var milieu = ThemeManager.LerpPalette(clair, sombre, 0.5f);

            var champs = typeof(ThemeManager.Palette).GetProperties();
            Assert.Equal(13, champs.Length);

            foreach (var champ in champs)
            {
                var a = (Color)champ.GetValue(clair)!;
                var b = (Color)champ.GetValue(sombre)!;
                var m = (Color)champ.GetValue(milieu)!;

                if (a == b) continue;   // rien à interpoler, le fondu ne peut pas s'y voir

                Assert.True(m != a && m != b,
                    $"« {champ.Name} » n'est pas interpolée : elle saute au milieu du fondu.");
            }
        }

        [Fact]
        public void LesBornesDeLInterpolationRendentLesPalettesExactes()
        {
            var clair = ThemeManager.GetPalette(AppTheme.Light);
            var sombre = ThemeManager.GetPalette(AppTheme.Dark);

            Assert.Equal(clair, ThemeManager.LerpPalette(clair, sombre, 0f));
            Assert.Equal(sombre, ThemeManager.LerpPalette(clair, sombre, 1f));
        }

        /// <summary>
        /// Les deux thèmes doivent VRAIMENT différer. Un copier-coller malheureux
        /// entre les deux branches de `GetPalette` donnerait une application dont
        /// le bouton « Thème sombre » ne fait rien — défaut qui s'est produit
        /// pendant cette migration, pour une autre raison, et qui a mis trois
        /// tentatives à être compris.
        /// </summary>
        [Fact]
        public void LesDeuxThemesSontReellementDifferents()
        {
            var clair = ThemeManager.GetPalette(AppTheme.Light);
            var sombre = ThemeManager.GetPalette(AppTheme.Dark);

            Assert.NotEqual(clair, sombre);
            Assert.NotEqual(clair.Bg, sombre.Bg);
            Assert.NotEqual(clair.Card, sombre.Card);
            Assert.NotEqual(clair.Fg, sombre.Fg);
        }
    }
}
