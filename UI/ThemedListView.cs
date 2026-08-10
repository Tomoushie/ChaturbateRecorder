using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Rend une <see cref="ListView"/> conforme au thème (39.0).
    ///
    /// **Pourquoi c'est nécessaire** : l'en-tête de colonnes d'une ListView est
    /// peint par le système et ignore <c>BackColor</c>. En thème sombre, la
    /// bande « Fichier / Taille / Durée / Date » restait donc claire au-dessus
    /// d'une liste sombre — le défaut le plus voyant de la fenêtre. Les lignes,
    /// elles, se contentaient de la sélection bleue système, sans rapport avec
    /// la couleur d'accent de l'application.
    ///
    /// Le dessin passe donc par <c>OwnerDraw</c>. Les gestionnaires ne sont
    /// câblés qu'UNE fois par contrôle (la palette, elle, est réactualisée à
    /// chaque passage) : <c>ThemeManager.ApplyPalette</c> est rappelé des
    /// dizaines de fois par seconde pendant la transition clair/sombre, et
    /// s'abonner à chaque fois multiplierait les dessins jusqu'au clignotement.
    /// </summary>
    internal static class ThemedListView
    {
        private sealed class State
        {
            public ThemeManager.Palette Palette;
            public bool Wired;
        }

        private static readonly ConditionalWeakTable<ListView, State> _states = new();

        /// <summary>
        /// Ascenseurs sombres : délégué à <see cref="NativeScrollBars"/> depuis
        /// 114.0. Le P/Invoke vivait ici, et c'est précisément pour ça que
        /// SEULES les ListView en bénéficiaient — Favoris, Logs et la fenêtre
        /// principale gardaient des barres blanches. Un seul exemplaire du
        /// code, appelé depuis tous les cas de ThemeManager.
        ///
        /// **Contrepartie assumée, mesurée en comparant les deux rendus à
        /// l'écran** : ce thème fait aussi dessiner par Windows les séparateurs
        /// de colonnes dans la zone VIDE sous les lignes. On l'accepte — une
        /// barre blanche vive sur fond sombre se remarque bien davantage qu'un
        /// filet gris, qui est de surcroît le rendu même de l'Explorateur. La
        /// classe "ItemsView" a été essayée : mêmes séparateurs.
        /// </summary>
        public static void Attach(ListView list, ThemeManager.Palette palette)
        {
            var state = _states.GetValue(list, _ => new State());
            state.Palette = palette;

            NativeScrollBars.Apply(list, palette);

            if (!state.Wired)
            {
                state.Wired = true;
                list.OwnerDraw = true;
                list.DrawColumnHeader += (s, e) => DrawHeader(e, state.Palette);
                list.DrawItem += (s, e) => DrawRow(e, state.Palette);
                list.DrawSubItem += (s, e) => DrawCell(e, state.Palette, list);
                list.Resize += (s, e) => StretchLastColumn(list);
            }

            StretchLastColumn(list);
            list.Invalidate();
        }

        /// <summary>
        /// Étire la dernière colonne jusqu'au bord de la liste.
        ///
        /// **Ce n'est pas une coquetterie de mise en page** : la zone d'en-tête
        /// située à droite de la dernière colonne n'est traversée par AUCUN
        /// évènement de dessin — le contrôle d'en-tête natif la remplit avec sa
        /// propre couleur, restée claire. En thème sombre, ça donnait un bloc
        /// blanc au bout de chaque barre de titres. Supprimer la zone est le
        /// seul moyen de la thématiser sans sous-classer l'en-tête Win32.
        ///
        /// Appelé aussi après remplissage (voir <see cref="Refresh"/>) : c'est
        /// l'apparition de l'ascenseur vertical qui réduit la largeur utile.
        /// </summary>
        public static void StretchLastColumn(ListView list)
        {
            if (list.View != View.Details || list.Columns.Count == 0) return;

            var used = 0;
            for (var i = 0; i < list.Columns.Count - 1; i++)
                used += list.Columns[i].Width;

            var last = list.Columns[^1];
            var target = list.ClientSize.Width - used;
            // Plancher : mieux vaut un reste de zone claire qu'une colonne
            // écrasée dont l'intitulé deviendrait illisible.
            if (target < 40) return;
            if (last.Width != target) last.Width = target;
        }

        /// <summary>
        /// À rappeler après avoir ajouté ou retiré des lignes : l'ascenseur
        /// vertical apparaît alors sans que le contrôle change de taille, donc
        /// sans lever <c>Resize</c>.
        ///
        /// **Différé par BeginInvoke** : au retour de <c>Items.Add</c>, la
        /// ListView n'a pas encore décidé si elle a besoin d'un ascenseur
        /// vertical, donc <c>ClientSize</c> vaut encore la largeur SANS
        /// ascenseur. Étirer à ce moment-là donne une dernière colonne trop
        /// large de ~17 px, et fait apparaître un ascenseur HORIZONTAL — vu à
        /// la capture, sur une liste de quatre lignes.
        /// </summary>
        public static void Refresh(ListView list)
        {
            if (list.IsHandleCreated)
                list.BeginInvoke(() => { if (!list.IsDisposed) StretchLastColumn(list); });
            else
                StretchLastColumn(list);
        }

        private static void DrawHeader(DrawListViewColumnHeaderEventArgs e, ThemeManager.Palette p)
        {
            var bg = ThemeManager.Lerp(p.Input, p.Fg, 0.04f);
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);

            // Seul un filet sous l'en-tête, pas de grille : c'est ce qui
            // sépare une liste moderne d'un tableau des années 2000.
            using (var pen = new Pen(p.Border))
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            var textRect = Rectangle.Inflate(e.Bounds, -8, 0);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? string.Empty, e.Font ?? SystemFonts.DefaultFont,
                textRect, p.FgMuted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        /// <summary>
        /// En vue Details, cette méthode ne dessine RIEN : chaque cellule peint
        /// son propre fond dans <see cref="DrawCell"/>.
        ///
        /// **C'est un correctif, pas un choix esthétique.** WinForms ne
        /// redessine que les cellules qui croisent la région invalidée, mais il
        /// lève `DrawItem` pour la ligne ENTIÈRE. Remplir tout `e.Bounds` ici
        /// effaçait donc des cellules que la même passe n'allait pas repeindre :
        /// après un rafraîchissement de l'historique, taille, durée et date
        /// disparaissaient, et un clic — qui invalide tout — les faisait
        /// revenir. Signalé en utilisation réelle (109.0).
        /// </summary>
        private static void DrawRow(DrawListViewItemEventArgs e, ThemeManager.Palette p)
        {
            if (e.Item?.ListView?.View == View.Details) return;

            // Vue autre que Details : DrawSubItem n'est jamais levé, le fond et
            // le texte doivent être dessinés ici, sinon la liste paraît vide.
            var fill = e.Item?.Selected == true ? ThemeManager.Lerp(p.Input, p.Accent, 0.20f) : p.Input;

            using (var brush = new SolidBrush(fill))
                e.Graphics.FillRectangle(brush, e.Bounds);

            TextRenderer.DrawText(e.Graphics, e.Item?.Text ?? string.Empty, e.Item?.Font ?? SystemFonts.DefaultFont,
                Rectangle.Inflate(e.Bounds, -6, 0), p.Fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private static void DrawCell(DrawListViewSubItemEventArgs e, ThemeManager.Palette p, ListView list)
        {
            if (e.Item == null) return;

            var bounds = e.Bounds;

            // Chaque cellule peint SON fond : c'est ce qui rend le dessin
            // correct quand WinForms ne redessine qu'une partie de la ligne
            // (voir DrawRow). `e.Item.Selected` et non `e.ItemState`, pour la
            // même raison de fiabilité qu'ailleurs.
            var fill = e.Item.Selected ? ThemeManager.Lerp(p.Input, p.Accent, 0.20f) : p.Input;
            using (var brush = new SolidBrush(fill))
                e.Graphics.FillRectangle(brush, bounds);

            var x = bounds.Left + 6;

            // Miniature (v1.29.0) : en dessin par l'application, l'ImageList
            // n'est plus posée automatiquement — elle continue en revanche de
            // fixer la hauteur de ligne, ce qui reste le comportement voulu.
            //
            // **ImageList.Draw et NON Images[i]** : l'indexeur de la collection
            // CONSTRUIT un Bitmap neuf à chaque appel. Dans une boucle de
            // dessin, ça épuise les handles GDI du processus en quelques
            // secondes — et le symptôme ne ressemble en rien à la cause :
            // BeginPaint finit par rendre un DC nul, et c'est le premier
            // contrôle peint ensuite qui lève ArgumentNullException('dc').
            // Constaté ici même, et invisible tant que l'historique n'a pas de
            // vraies miniatures.
            if (e.ColumnIndex == 0 && list.SmallImageList != null && e.Item.ImageIndex >= 0
                && e.Item.ImageIndex < list.SmallImageList.Images.Count)
            {
                var size = list.SmallImageList.ImageSize;
                var y = bounds.Top + (bounds.Height - size.Height) / 2;
                list.SmallImageList.Draw(e.Graphics, x, y, e.Item.ImageIndex);
                x += size.Width + 8;
            }

            var textRect = new Rectangle(x, bounds.Top, Math.Max(0, bounds.Right - x - 4), bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? string.Empty, e.Item.Font ?? SystemFonts.DefaultFont,
                textRect, p.Fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }
}
