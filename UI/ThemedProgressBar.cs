using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Barre de progression dessinée à la main, en remplacement de la ProgressBar
    /// native. Deux défauts du contrôle natif la rendaient nécessaire :
    ///
    /// 1. La couleur dynamique selon l'état (3.4) ne fonctionnait pas.
    ///    PBM_SETBARCOLOR est purement et simplement ignoré par comctl32 v6 dès
    ///    que les styles visuels sont actifs (Program.Main appelle
    ///    Application.EnableVisualStyles), qui impose alors son vert dégradé :
    ///    "en cours", "terminé", "erreur" et "arrêté" s'affichaient donc tous
    ///    de la même couleur. Le seul moyen de faire reprendre effet au message
    ///    aurait été SetWindowTheme(handle, "", "") — mais cela retire le style
    ///    visuel du contrôle, qui retombe alors sur son rendu "classique" plat
    ///    d'avant Windows XP, sans rapport avec le reste de l'interface.
    ///
    /// 2. La piste du contrôle natif ignore le thème de l'application : elle
    ///    reste blanche sur fond sombre, exactement comme l'ascenseur natif qui
    ///    a motivé <see cref="ThemedScrollBar"/>.
    ///
    /// Ne dépend que de GDI+, comme <see cref="RoundedGroupPanel"/> et
    /// <see cref="ThemedScrollBar"/>. Reprend volontairement les noms de
    /// propriétés du contrôle natif (Minimum/Maximum/Value/Style/
    /// MarqueeAnimationSpeed) et son enum <see cref="ProgressBarStyle"/>.
    /// </summary>
    internal sealed class ThemedProgressBar : Control
    {
        /// <summary>Part de la piste occupée par le segment en mode Marquee.</summary>
        private const double MarqueeSegmentRatio = 0.35;

        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private ProgressBarStyle _style = ProgressBarStyle.Blocks;
        private int _marqueeAnimationSpeed = 30;
        private double _marqueePhase;
        private System.Windows.Forms.Timer? _marqueeTimer;

        public ThemedProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            TabStop = false;
        }

        // Comme ThemedScrollBar : ce contrôle est toujours posé en code (jamais
        // dans le concepteur WinForms), ces propriétés n'ont donc rien à
        // sérialiser dans un .Designer.cs qui n'existera jamais (WFO1000).

        // Ces trois couleurs se repeignent à l'affectation, comme Value et
        // Style : BarColor change à chaque tick de l'effet de pulsation
        // (MainForm.PulseProgressBar) et les deux autres à chaque image d'une
        // transition de thème animée, donc laisser l'Invalidate à l'appelant
        // reviendrait à ne jamais rien voir bouger le jour où l'un l'oublie.
        private Color _barColor = Color.FromArgb(0x00, 0x78, 0xD4);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color BarColor
        {
            get => _barColor;
            set { if (_barColor == value) return; _barColor = value; Invalidate(); }
        }

        private Color _trackColor = Color.FromArgb(0xE6, 0xE6, 0xE6);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color TrackColor
        {
            get => _trackColor;
            set { if (_trackColor == value) return; _trackColor = value; Invalidate(); }
        }

        private Color _borderColor = Color.FromArgb(0xD9, 0xD9, 0xD9);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color BorderColor
        {
            get => _borderColor;
            set { if (_borderColor == value) return; _borderColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int Minimum
        {
            get => _minimum;
            set { _minimum = value; if (_maximum < _minimum) _maximum = _minimum; Value = _value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(_minimum, value); Value = _value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int Value
        {
            get => _value;
            set
            {
                var clamped = Math.Clamp(value, _minimum, _maximum);
                if (clamped == _value) return;
                _value = clamped;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal ProgressBarStyle Style
        {
            get => _style;
            set { _style = value; UpdateMarqueeTimer(); Invalidate(); }
        }

        /// <summary>
        /// Période du rafraîchissement du défilement en mode Marquee, en
        /// millisecondes — 0 fige l'animation, comme sur le contrôle natif.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int MarqueeAnimationSpeed
        {
            get => _marqueeAnimationSpeed;
            set { _marqueeAnimationSpeed = Math.Max(0, value); UpdateMarqueeTimer(); }
        }

        /// <summary>
        /// Largeur remplie, isolée du dessin pour être testable sans instancier
        /// de contrôle (voir ThemedProgressBarTests) — c'est la seule partie du
        /// rendu qui puisse être fausse sans se voir : une barre qui n'atteint
        /// jamais tout à fait le bout, ou qui déborde d'un pixel, ne se remarque
        /// qu'aux valeurs extrêmes.
        /// </summary>
        internal static int ComputeFillWidth(int trackWidth, int minimum, int maximum, int value)
        {
            if (trackWidth <= 0) return 0;

            var span = maximum - minimum;
            if (span <= 0) return 0;

            var clamped = Math.Clamp(value, minimum, maximum);
            return (int)Math.Round((double)trackWidth * (clamped - minimum) / span);
        }

        /// <summary>
        /// Position et largeur du segment en mode Marquee pour une phase donnée
        /// (0 -> 1, un cycle complet). Le segment entre par la gauche et sort
        /// par la droite : la plage parcourue va donc de -largeur à la largeur
        /// totale, sinon il apparaîtrait et disparaîtrait d'un coup aux
        /// extrémités au lieu de glisser.
        /// </summary>
        internal static (int Left, int Width) ComputeMarqueeSegment(int trackWidth, double phase)
        {
            if (trackWidth <= 0) return (0, 0);

            var width = Math.Max(1, (int)(trackWidth * MarqueeSegmentRatio));
            var wrapped = phase - Math.Floor(phase);
            var left = (int)Math.Round(wrapped * (trackWidth + width)) - width;

            // Rogné à la piste : le dessin travaille en coordonnées visibles,
            // et un segment qui dépasse fausserait le rayon des coins arrondis.
            var right = Math.Min(trackWidth, left + width);
            left = Math.Max(0, left);
            return (left, Math.Max(0, right - left));
        }

        private void UpdateMarqueeTimer()
        {
            var shouldRun = _style == ProgressBarStyle.Marquee && _marqueeAnimationSpeed > 0;

            if (!shouldRun)
            {
                _marqueeTimer?.Stop();
                _marqueeTimer?.Dispose();
                _marqueeTimer = null;
                return;
            }

            if (_marqueeTimer == null)
            {
                _marqueeTimer = new System.Windows.Forms.Timer();
                _marqueeTimer.Tick += (s, e) =>
                {
                    // Une phase par cycle : la vitesse de défilement ne dépend
                    // donc pas de la largeur du contrôle.
                    _marqueePhase += _marqueeAnimationSpeed / 1400.0;
                    Invalidate();
                };
            }

            _marqueeTimer.Interval = _marqueeAnimationSpeed;
            _marqueeTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Le parent peint le fond : sans ça, les coins arrondis laisseraient
            // voir la couleur par défaut du contrôle au lieu du panneau derrière.
            g.Clear(Parent?.BackColor ?? BackColor);

            // Le rectangle s'arrête un pixel avant le bord (la bordure est
            // tracée dessus), et le rayon se calcule sur SA hauteur, pas sur
            // celle du contrôle : un rayon de Height/2 donnerait un diamètre
            // plus grand que le rectangle et RoundedPath retomberait sur des
            // coins carrés.
            var track = new Rectangle(0, 0, Width - 1, Height - 1);
            var radius = track.Height / 2;

            using (var path = RoundedPath(track, radius))
            {
                using var trackBrush = new SolidBrush(_trackColor);
                g.FillPath(trackBrush, path);
                using var pen = new Pen(_borderColor);
                g.DrawPath(pen, path);
            }

            var (left, width) = _style == ProgressBarStyle.Marquee
                ? ComputeMarqueeSegment(track.Width, _marqueePhase)
                : (0, ComputeFillWidth(track.Width, _minimum, _maximum, _value));

            if (width <= 0) return;

            var fill = new Rectangle(track.X + left, track.Y, width, track.Height);
            using (var fillPath = RoundedPath(fill, Math.Min(radius, width / 2)))
            {
                using var barBrush = new SolidBrush(_barColor);
                g.FillPath(barBrush, fillPath);
            }
        }

        private static GraphicsPath RoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var d = Math.Max(1, radius * 2);

            // Un rayon nul (barre de quelques pixels) dégénérerait en arcs
            // vides : on retombe alors sur un simple rectangle. Comparaison
            // stricte : un diamètre EGAL au côté est la capsule parfaite (les
            // deux arcs se rejoignent pile), pas un cas dégénéré — c'est le cas
            // d'une progression de quelques pour cent, qui doit rester arrondie.
            if (d > rect.Width || d > rect.Height)
            {
                if (rect.Width <= 0 || rect.Height <= 0) { path.AddRectangle(new Rectangle(rect.X, rect.Y, 1, 1)); return path; }
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _marqueeTimer?.Stop();
                _marqueeTimer?.Dispose();
                _marqueeTimer = null;
            }

            base.Dispose(disposing);
        }
    }
}
