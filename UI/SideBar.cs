using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Barre de navigation verticale (97.0), colonne de gauche de la fenêtre.
    ///
    /// **Ce qu'elle remplace, et pourquoi** : jusqu'ici tout vivait sur un seul
    /// écran défilant — enregistrement, historique, favoris, surveillance, dons
    /// et logs empilés. C'est ce qui avait rendu nécessaire le mode
    /// simple/avancé, dont le seul rôle était de MASQUER la moitié de la page.
    /// Une navigation supprime le besoin : on ne cache plus, on range.
    ///
    /// **Dessinée à la main**, comme <see cref="ThemedButton"/> et
    /// <see cref="ThemedScrollBar"/> : aucun contrôle Windows ne donne ce
    /// rendu, et l'application sait déjà peindre ses propres contrôles.
    /// </summary>
    internal sealed class SideBar : Control
    {
        internal sealed class Entry
        {
            public required string Key { get; init; }
            public required string IconName { get; init; }
            public string Label { get; set; } = "";
        }

        internal const int DefaultWidth = 196;
        private const int ItemHeight = 44;
        private const int TopPadding = 12;
        private const int IconSize = 18;
        private const int IconLeft = 18;
        private const int TextLeft = 50;
        private const int MarkerWidth = 3;

        private readonly List<Entry> _entries = new();
        private int _selected;
        private int _hovered = -1;

        internal event EventHandler? SelectionChanged;

        public SideBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Width = DefaultWidth;
            TabStop = false;
        }

        // Posées par ThemeManager, jamais par le concepteur : sans cet attribut
        // l'analyseur WinForms (WFO1000) exige de savoir les sérialiser dans un
        // .Designer.cs qui n'existera jamais.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color SurfaceColor { get; set; } = Color.FromArgb(23, 26, 33);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color TextColor { get; set; } = Color.Gainsboro;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color MutedColor { get; set; } = Color.Gray;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color AccentColor { get; set; } = Color.DodgerBlue;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color BorderColor { get; set; } = Color.DimGray;

        internal IReadOnlyList<Entry> Entries => _entries;

        internal string SelectedKey => _entries.Count == 0 ? "" : _entries[_selected].Key;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int SelectedIndex
        {
            get => _selected;
            set
            {
                var clamped = Math.Max(0, Math.Min(_entries.Count - 1, value));
                if (clamped == _selected) return;
                _selected = clamped;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void AddEntry(string key, string iconName) =>
            _entries.Add(new Entry { Key = key, IconName = iconName });

        /// <summary>
        /// Les libellés sont posés séparément des entrées : ils changent à
        /// chaque bascule de langue, pas la structure.
        /// </summary>
        internal void SetLabel(string key, string label)
        {
            foreach (var e in _entries)
                if (e.Key == key) { e.Label = label; break; }
            Invalidate();
        }

        /// <summary>
        /// Index de l'entrée sous ce point, ou -1. Isolée et internal pour être
        /// vérifiable sans afficher de fenêtre : c'est la seule arithmétique de
        /// ce contrôle, et une erreur d'un pixel y sélectionnerait la mauvaise
        /// section près des bords.
        /// </summary>
        internal static int IndexAt(int y, int count)
        {
            if (count <= 0 || y < TopPadding) return -1;
            var index = (y - TopPadding) / ItemHeight;
            return index >= 0 && index < count ? index : -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var index = IndexAt(e.Y, _entries.Count);
            if (index == _hovered) return;
            _hovered = index;
            Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hovered == -1) return;
            _hovered = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var index = IndexAt(e.Y, _entries.Count);
            if (index >= 0) SelectedIndex = index;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var fond = new SolidBrush(SurfaceColor))
                g.FillRectangle(fond, ClientRectangle);

            // Filet de séparation à droite plutôt qu'une ombre : la zone de
            // contenu a sa propre couleur, un seul trait suffit à marquer la
            // frontière sans alourdir.
            using (var trait = new Pen(BorderColor))
                g.DrawLine(trait, Width - 1, 0, Width - 1, Height);

            for (var i = 0; i < _entries.Count; i++)
            {
                var rect = new Rectangle(0, TopPadding + i * ItemHeight, Width - 1, ItemHeight);
                var choisi = i == _selected;
                var survole = i == _hovered && !choisi;

                if (choisi)
                {
                    // Teinte d'accent très diluée : marquer la section active
                    // sans transformer la barre en aplat coloré. Le repère de
                    // gauche fait le gros du travail.
                    using var fond = new SolidBrush(Melange(SurfaceColor, AccentColor, 0.16f));
                    g.FillRectangle(fond, rect);
                    using var repere = new SolidBrush(AccentColor);
                    g.FillRectangle(repere, new Rectangle(0, rect.Y + 8, MarkerWidth, ItemHeight - 16));
                }
                else if (survole)
                {
                    using var fond = new SolidBrush(Melange(SurfaceColor, TextColor, 0.07f));
                    g.FillRectangle(fond, rect);
                }

                var couleur = choisi ? TextColor : MutedColor;

                // IconManager.Render construit un Bitmap à chaque appel : il est
                // libéré ici même. Ne PAS le mettre en cache dans une ImageList
                // sans relire le piège de 103.0 et de 39.0.
                try
                {
                    using var icone = IconManager.Render(_entries[i].IconName, IconSize, couleur);
                    g.DrawImage(icone, IconLeft, rect.Y + (ItemHeight - IconSize) / 2, IconSize, IconSize);
                }
                catch (Exception ex)
                {
                    // Un pictogramme manquant ne doit pas empêcher de naviguer.
                    System.Diagnostics.Debug.WriteLine($"Pictogramme '{_entries[i].IconName}' indisponible : {ex.Message}");
                }

                using var texte = new SolidBrush(couleur);
                using var police = new Font(Font, choisi ? FontStyle.Bold : FontStyle.Regular);
                var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(_entries[i].Label, police, texte,
                    new RectangleF(TextLeft, rect.Y, Width - TextLeft - 8, ItemHeight), format);
            }
        }

        private static Color Melange(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }
}
