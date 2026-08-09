using System;
using System.Drawing;
using System.Windows.Forms;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Garde-fous de la hiérarchie visuelle introduite en 39.0.
    ///
    /// Ce qui est vérifié ici n'est pas « les couleurs sont jolies » — c'est
    /// invérifiable — mais les propriétés qu'une retouche de palette pourrait
    /// casser sans que rien ne se voie à la compilation : un texte devenu
    /// illisible sur son fond, un bouton d'accent qui cesse de se distinguer
    /// des autres, ou un survol qui ne change plus rien.
    /// </summary>
    public class ThemeHierarchyTests
    {
        public static TheoryData<AppTheme> Themes => new() { AppTheme.Light, AppTheme.Dark };

        /// <summary>
        /// Rapport de contraste WCAG entre deux couleurs opaques. Recalculé ici
        /// plutôt qu'importé : la formule tient en cinq lignes et une
        /// dépendance de plus pour ça serait absurde.
        /// </summary>
        private static double Contrast(Color a, Color b)
        {
            static double Channel(int v)
            {
                var c = v / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            static double Luminance(Color c) =>
                0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);

            var (l1, l2) = (Luminance(a), Luminance(b));
            if (l1 < l2) (l1, l2) = (l2, l1);
            return (l1 + 0.05) / (l2 + 0.05);
        }

        [Theory]
        [MemberData(nameof(Themes))]
        public void PrimaryButton_UsesAccentFill(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);
            var colors = ThemeManager.ResolveButtonColors(ButtonRole.Primary, p);

            Assert.Equal(p.Accent, colors.Fill);
            Assert.Equal(p.AccentFg, colors.Fg);
            // Pas de bordure sur un aplat : elle n'ajouterait qu'un liseré plus
            // foncé sur une couleur déjà pleine.
            Assert.True(colors.Border.IsEmpty);
        }

        [Theory]
        [MemberData(nameof(Themes))]
        public void SecondaryButton_IsNeutralWithBorder(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);
            var colors = ThemeManager.ResolveButtonColors(ButtonRole.Secondary, p);

            Assert.Equal(p.Neutral, colors.Fill);
            Assert.Equal(p.Fg, colors.Fg);
            // Sans bordure, un bouton secondaire au fond presque identique à
            // celui de sa carte disparaîtrait : c'est elle qui le délimite.
            Assert.False(colors.Border.IsEmpty);
        }

        [Theory]
        [MemberData(nameof(Themes))]
        public void DangerButton_DoesNotFillWithRed(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);
            var colors = ThemeManager.ResolveButtonColors(ButtonRole.Danger, p);

            // Le garde-fou qui compte : « Stop », « Tout arrêter »,
            // « Supprimer favori » et « Ne plus surveiller » sont visibles en
            // même temps. Quatre aplats rouges donneraient une fenêtre en
            // alerte permanente alors que rien ne va mal.
            Assert.Equal(p.Neutral, colors.Fill);
            Assert.Equal(p.Danger, colors.Fg);
        }

        [Theory]
        [MemberData(nameof(Themes))]
        public void PrimaryStandsOutFromSecondary(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);
            var primary = ThemeManager.ResolveButtonColors(ButtonRole.Primary, p);
            var secondary = ThemeManager.ResolveButtonColors(ButtonRole.Secondary, p);

            // Toute la refonte tient à ça : l'action principale doit se voir du
            // premier coup d'œil parmi les autres.
            Assert.True(Contrast(primary.Fill, secondary.Fill) >= 2.0,
                $"Accent et fond neutre trop proches en thème {theme} : "
                + $"contraste {Contrast(primary.Fill, secondary.Fill):0.00}");
        }

        [Theory]
        [MemberData(nameof(Themes))]
        public void EveryRole_KeepsReadableText(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);

            foreach (var role in Enum.GetValues<ButtonRole>())
            {
                var c = ThemeManager.ResolveButtonColors(role, p);
                foreach (var (state, fill) in new[] { ("repos", c.Fill), ("survol", c.Hover), ("appui", c.Pressed) })
                {
                    var ratio = Contrast(c.Fg, fill);
                    Assert.True(ratio >= 4.5,
                        $"Texte illisible sur {role} en {state}, thème {theme} : contraste {ratio:0.00} (< 4,5)");
                }
            }
        }

        [Theory]
        [MemberData(nameof(Themes))]
        public void HoverAndPressed_AreDistinctFromRest(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);

            foreach (var role in Enum.GetValues<ButtonRole>())
            {
                var c = ThemeManager.ResolveButtonColors(role, p);
                // Un survol qui rend la même couleur que le repos supprime le
                // seul retour visuel dont dispose la souris.
                Assert.True(c.Hover != c.Fill, $"{role} : survol identique au repos en thème {theme}");
                Assert.True(c.Pressed != c.Hover, $"{role} : appui identique au survol en thème {theme}");
            }
        }

        [Theory]
        [MemberData(nameof(Themes))]
        public void CardStandsOutFromWindowBackground(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);

            // Le défaut d'origine de 39.0 : les cartes prenaient la couleur du
            // fond de fenêtre et n'existaient plus que par un liseré de 1 px,
            // d'où un écran entièrement plat.
            Assert.True(p.Card != p.Bg, $"Carte et fond identiques en thème {theme}");
        }

        [Theory]
        [MemberData(nameof(Themes))]
        public void MutedText_StaysReadableButQuieter(AppTheme theme)
        {
            var p = ThemeManager.GetPalette(theme);

            var body = Contrast(p.Fg, p.Card);
            var muted = Contrast(p.FgMuted, p.Card);

            // Atténué, pas effacé : un intitulé de champ doit rester lisible.
            Assert.True(muted >= 4.5, $"Libellé secondaire illisible en thème {theme} : {muted:0.00}");
            Assert.True(muted < body, $"Libellé secondaire pas plus discret que le texte courant en thème {theme}");
        }

        [Fact]
        public void TextRole_DefaultsToBody()
        {
            using var label = new Label();
            Assert.Equal(TextRole.Body, ThemeManager.GetTextRole(label));

            ThemeManager.SetTextRole(label, TextRole.Caption);
            Assert.Equal(TextRole.Caption, ThemeManager.GetTextRole(label));

            // Réassigner ne doit pas lever : ApplyPalette repasse sur les mêmes
            // contrôles à chaque changement de thème.
            ThemeManager.SetTextRole(label, TextRole.Body);
            Assert.Equal(TextRole.Body, ThemeManager.GetTextRole(label));
        }

        [Fact]
        public void ButtonContent_IsCenteredAsAGroup()
        {
            // 120 px de bouton, groupe de 60 : 30 px de chaque côté.
            Assert.Equal(30, ThemedButton.ContentStartX(120, 60));

            // Piège de la v1.22.1 : c'est le GROUPE qui est centré, pas le
            // texte dans sa propre zone. Ajouter une icône décale donc le
            // départ vers la gauche, il ne le laisse pas en place.
            var withoutIcon = ThemedButton.ContentStartX(120, 40);
            var withIcon = ThemedButton.ContentStartX(120, 40 + 16 + 8);
            Assert.True(withIcon < withoutIcon);
        }

        [Fact]
        public void ButtonContent_ClampsLeftWhenTooLong()
        {
            // Un groupe plus large que le bouton donnerait une abscisse
            // négative : le début du libellé sortirait par la gauche, donc le
            // texte serait rogné des DEUX côtés.
            var x = ThemedButton.ContentStartX(100, 260);
            Assert.True(x >= 0);
            Assert.Equal(ThemedButton.ContentStartX(100, 100), x);
        }

        [Fact]
        public void IconTextGap_OnlyWhenBothPresent()
        {
            Assert.Equal(0, ThemedButton.IconTextGap(0, "Démarrer"));
            Assert.Equal(0, ThemedButton.IconTextGap(16, ""));
            Assert.Equal(0, ThemedButton.IconTextGap(16, null));
            Assert.True(ThemedButton.IconTextGap(16, "Démarrer") > 0);
        }

        [Fact]
        public void LastColumn_FillsTheListWidth()
        {
            using var list = new ListView { View = View.Details, Width = 300, Height = 100 };
            list.Columns.Add("A", 100);
            list.Columns.Add("B", 50);

            ThemedListView.StretchLastColumn(list);

            // La zone d'en-tête à droite de la dernière colonne n'est traversée
            // par aucun évènement de dessin : le contrôle natif la remplit de
            // blanc, y compris en thème sombre. La supprimer est le correctif.
            Assert.Equal(list.ClientSize.Width, list.Columns[0].Width + list.Columns[1].Width);
        }

        [Fact]
        public void LastColumn_IsNotCrushedWhenSpaceIsMissing()
        {
            using var list = new ListView { View = View.Details, Width = 120, Height = 100 };
            list.Columns.Add("A", 100);
            list.Columns.Add("B", 60);

            ThemedListView.StretchLastColumn(list);

            // Mieux vaut un reste de zone claire qu'un intitulé écrasé et
            // illisible.
            Assert.Equal(60, list.Columns[1].Width);
        }

        [Fact]
        public void OnlyInputsAndListsGetAFrame()
        {
            using var text = new TextBox();
            using var list = new ListView();
            using var box = new ListBox();
            using var picture = new PictureBox();
            using var button = new ThemedButton();
            using var combo = new ThemedComboBox();

            Assert.True(InputFrame.NeedsFrame(text));
            Assert.True(InputFrame.NeedsFrame(list));
            Assert.True(InputFrame.NeedsFrame(box));
            Assert.True(InputFrame.NeedsFrame(picture));

            // Ces deux-là dessinent leur propre bordure : un cadre de plus
            // ferait un double trait.
            Assert.False(InputFrame.NeedsFrame(button));
            Assert.False(InputFrame.NeedsFrame(combo));
        }
    }
}
