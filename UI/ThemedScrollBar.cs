using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Ascenseur vertical dessiné à la main. L'ascenseur natif d'un contrôle
    /// Windows ignore le thème de l'application : il reste clair sur un fond
    /// sombre. Le noircir passerait par AllowDarkModeForWindow /
    /// SetPreferredAppMode d'uxtheme.dll, exportées par ordinal, non
    /// documentées et déplacées d'une version de Windows à l'autre — d'où ce
    /// contrôle, qui ne dépend que de GDI+.
    ///
    /// Unité : le pixel de contenu (pas la ligne). <see cref="Maximum"/> est la
    /// hauteur totale du contenu et <see cref="LargeChange"/> la hauteur
    /// visible, ce qui permet de piloter directement le décalage d'un contrôle
    /// dans son conteneur (voir ChangelogForm).
    /// </summary>
    internal sealed class ThemedScrollBar : Control
    {
        internal const int Thickness = 12;
        private const int ThumbWidth = 6;
        private const int ThumbHoverWidth = 10;
        private const int MinThumbHeight = 24;

        private int _maximum = 1;
        private int _largeChange = 1;
        private int _value;
        private bool _hovering;
        private bool _dragging;
        private int _dragOffset;

        // Membres internal et non public : ce contrôle n'est jamais posé dans
        // le concepteur WinForms, et une propriété publique sur un Control
        // devrait sinon déclarer sa sérialisation de concepteur (WFO1000).
        internal event EventHandler? ValueChanged;

        public ThemedScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Width = Thickness;
            TabStop = false;
        }

        // Toutes ces propriétés sont posées en code (par ThemeManager et par
        // ChangelogForm), jamais dans le concepteur : sans cet attribut,
        // l'analyseur WinForms (WFO1000) exige de savoir comment les
        // sérialiser dans un fichier .Designer.cs qui n'existera jamais.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color TrackColor { get; set; } = Color.Transparent;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color ThumbColor { get; set; } = Color.Gray;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color ThumbHoverColor { get; set; } = Color.DimGray;

        /// <summary>Hauteur totale du contenu, en pixels.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(1, value); SetValue(_value); Invalidate(); }
        }

        /// <summary>Hauteur visible — une "page" de défilement, en pixels.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int LargeChange
        {
            get => _largeChange;
            set { _largeChange = Math.Max(1, value); SetValue(_value); Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int Value
        {
            get => _value;
            set => SetValue(value);
        }

        /// <summary>Valeur maximale atteignable : au-delà, on verrait du vide.</summary>
        internal int MaximumValue => Math.Max(0, _maximum - _largeChange);

        private void SetValue(int value)
        {
            var clamped = Math.Clamp(value, 0, MaximumValue);
            if (clamped == _value) return;

            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Géométrie du curseur, isolée du dessin et des événements souris pour
        /// être testable sans instancier de contrôle (voir ThemedScrollBarTests).
        /// Un curseur proportionnel au contenu deviendrait insaisissable sur un
        /// contenu très long, d'où la hauteur plancher.
        /// </summary>
        internal static (int Top, int Height) ComputeThumb(
            int trackHeight, int maximum, int largeChange, int value)
        {
            if (trackHeight <= 0) return (0, 0);
            if (maximum <= largeChange) return (0, trackHeight);

            var height = (int)((long)trackHeight * largeChange / maximum);
            height = Math.Clamp(height, Math.Min(MinThumbHeight, trackHeight), trackHeight);

            var scrollable = maximum - largeChange;
            var travel = trackHeight - height;
            var top = travel <= 0
                ? 0
                : (int)((long)travel * Math.Clamp(value, 0, scrollable) / scrollable);

            return (top, height);
        }

        /// <summary>
        /// Inverse de <see cref="ComputeThumb"/> : position de curseur (pendant
        /// un glisser) vers valeur de défilement.
        /// </summary>
        internal static int ValueFromThumbTop(
            int trackHeight, int maximum, int largeChange, int thumbTop)
        {
            var scrollable = Math.Max(0, maximum - largeChange);
            var travel = trackHeight - ComputeThumb(trackHeight, maximum, largeChange, 0).Height;
            if (travel <= 0 || scrollable <= 0) return 0;

            return (int)Math.Clamp((long)thumbTop * scrollable / travel, 0, scrollable);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (TrackColor != Color.Transparent)
                e.Graphics.Clear(TrackColor);

            if (_maximum <= _largeChange) return;

            var (top, height) = ComputeThumb(Height, _maximum, _largeChange, _value);
            var active = _hovering || _dragging;
            var width = active ? ThumbHoverWidth : ThumbWidth;
            var rect = new Rectangle((Width - width) / 2, top, width, height);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(active ? ThumbHoverColor : ThumbColor);
            using var path = RoundedPath(rect, width / 2);
            e.Graphics.FillPath(brush, path);
        }

        private static GraphicsPath RoundedPath(Rectangle rect, int radius)
        {
            var d = Math.Max(1, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_maximum <= _largeChange) return;

            var (top, height) = ComputeThumb(Height, _maximum, _largeChange, _value);
            if (e.Y >= top && e.Y < top + height)
            {
                _dragging = true;
                _dragOffset = e.Y - top;
                Capture = true;
                Invalidate();
            }
            else
            {
                // Clic dans la piste : page vers le clic, comme un ascenseur natif.
                SetValue(_value + (e.Y < top ? -_largeChange : _largeChange));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
                SetValue(ValueFromThumbTop(Height, _maximum, _largeChange, e.Y - _dragOffset));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_dragging) return;

            _dragging = false;
            Capture = false;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovering = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovering = false;
            // Pas de repaint pendant un glisser : la souris sort régulièrement
            // du contrôle sans que le curseur doive rétrécir pour autant.
            if (!_dragging) Invalidate();
        }
    }
}
