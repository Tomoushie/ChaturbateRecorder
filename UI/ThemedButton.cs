using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Rôle d'un bouton dans la hiérarchie visuelle (39.0). C'est ce rôle, et
    /// non une couleur écrite au point d'appel, qui décide de l'apparence :
    /// ThemeManager en dérive tout le reste, donc changer de palette ne demande
    /// de retoucher aucun formulaire.
    ///
    /// **Règle** : un seul <see cref="Primary"/> par zone de l'écran. Avant
    /// 39.0, les 18 boutons de la fenêtre étaient tous en bleu d'accent plein —
    /// « Démarrer » pesait donc visuellement autant que « Reddit », et l'œil
    /// n'avait aucun point d'entrée.
    /// </summary>
    public enum ButtonRole
    {
        /// <summary>Action principale de la zone : fond d'accent plein.</summary>
        Primary,
        /// <summary>Tout le reste : fond neutre, bordure fine, texte du thème.</summary>
        Secondary,
        /// <summary>Interrompt ou supprime : accent rouge, en aplat léger.</summary>
        Danger,
    }

    /// <summary>
    /// Bouton dessiné entièrement par l'application (39.0), dans la lignée de
    /// <see cref="ThemedProgressBar"/> et <see cref="ThemedScrollBar"/>.
    ///
    /// **Pourquoi ne pas garder le Button natif** : trois défauts qu'aucun
    /// réglage ne corrige.
    /// 1. Une bordure arrondie est impossible — `FlatAppearance.BorderColor`
    ///    dessine un rectangle droit, que la Region arrondie découpe aux coins
    ///    en laissant des amorces de traits. Or un bouton secondaire SANS
    ///    bordure ne se distingue plus de la carte qui le porte.
    /// 2. La Region ne connaît pas l'antialiasing : les coins « arrondis » de
    ///    l'ancien rendu étaient en escalier, visible à la capture.
    /// 3. `TextImageRelation` découpe le bouton en deux zones et centre le
    ///    texte dans SA zone (piège documenté en v1.22.1) — d'où des libellés
    ///    décentrés dès que l'icône est présente. Ici, icône et texte forment
    ///    un groupe centré ensemble, le problème ne peut plus se poser.
    ///
    /// L'icône est désignée par son NOM (<see cref="IconName"/>) et rendue par
    /// le contrôle lui-même à la couleur du texte courant : elle suit donc le
    /// rôle et le thème sans qu'aucun appelant ait à la redessiner.
    /// </summary>
    public class ThemedButton : Button
    {
        private const int CornerRadius = 6;
        private const int IconTextGapPx = 8;
        private const int EdgePadding = 10;

        /// <summary>
        /// Espace entre l'icône et le libellé — nul si l'un des deux manque,
        /// sans quoi un bouton sans texte verrait son icône décalée vers la
        /// gauche de la moitié du vide.
        /// </summary>
        internal static int IconTextGap(int iconWidth, string? text) =>
            iconWidth > 0 && !string.IsNullOrEmpty(text) ? IconTextGapPx : 0;

        /// <summary>
        /// Abscisse du groupe icône+texte. Centré tant qu'il tient, sinon collé
        /// à gauche avec une demi-marge — un libellé trop long est alors coupé
        /// par une ellipse au lieu de déborder symétriquement des deux côtés,
        /// ce qui masquerait aussi son DÉBUT.
        /// </summary>
        internal static int ContentStartX(int controlWidth, int groupWidth)
        {
            var x = (controlWidth - groupWidth) / 2;
            return x < EdgePadding / 2 ? EdgePadding / 2 : x;
        }

        private ButtonRole _role = ButtonRole.Secondary;
        private string? _iconName;
        private int _iconSize = 16;
        private Bitmap? _icon;
        private Color _iconColor = Color.Empty;

        // Couleur affichée à l'instant t : elle glisse vers la couleur cible
        // (repos / survol / appui) au lieu de sauter, ce qui reprend l'effet
        // introduit en 9.2 — mais porté ici, le rendu étant désormais le nôtre.
        private Color _currentFill;
        private Color _targetFill;
        private System.Windows.Forms.Timer? _fadeTimer;
        private bool _hovering;
        private bool _pressing;

        public ThemedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
        }

        /// <summary>Rôle visuel — voir <see cref="ButtonRole"/>.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ButtonRole Role
        {
            get => _role;
            set { _role = value; Invalidate(); }
        }

        /// <summary>
        /// Nom d'icône <see cref="IconManager"/>, ou null pour un bouton sans
        /// icône. Le rendu est différé et mis en cache : changer de thème ne
        /// coûte un redessin de SVG que si la couleur du texte a réellement
        /// changé.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? IconName
        {
            get => _iconName;
            set { if (_iconName != value) { _iconName = value; InvalidateIcon(); } }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int IconSize
        {
            get => _iconSize;
            set { if (_iconSize != value) { _iconSize = value; InvalidateIcon(); } }
        }

        /// <summary>
        /// Fond de la surface qui porte le bouton — les coins arrondis laissent
        /// voir cette couleur, faute de vraie transparence.
        ///
        /// **Laissée vide, elle se déduit du parent** (voir
        /// <see cref="ResolvedSurface"/>). Sans ce repli, un bouton posé dans
        /// une fenêtre qui n'appelle pas ThemeManager peignait ses coins en gris
        /// système sur un fond clair : quatre angles visibles, exactement
        /// l'aspect « bouton rogné » signalé sur le Guide de démarrage (111.0).
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SurfaceColor { get; set; } = Color.Empty;

        /// <summary>
        /// Couleur réellement utilisée pour masquer les coins : celle posée par
        /// le thème si elle existe, sinon le fond du parent, qui est toujours
        /// juste même quand personne n'a thématisé la fenêtre.
        /// </summary>
        private Color ResolvedSurface =>
            !SurfaceColor.IsEmpty ? SurfaceColor
            : Parent != null ? Parent.BackColor
            : SystemColors.Control;

        /// <summary>Fond au repos.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FillColor { get; set; } = SystemColors.Control;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverFillColor { get; set; } = SystemColors.ControlLight;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color PressedFillColor { get; set; } = SystemColors.ControlDark;

        /// <summary>
        /// Bordure, ou <see cref="Color.Empty"/> pour aucune — ce que
        /// ThemeManager pose sur les boutons pleins, où elle n'ajouterait qu'un
        /// liseré plus foncé.
        ///
        /// La valeur PAR DÉFAUT en a une, contrairement aux boutons d'accent :
        /// tant qu'aucun thème n'est appliqué, le fond du bouton est celui du
        /// système et ne se distingue pas toujours de la fenêtre. Sans bordure,
        /// le bouton disparaîtrait purement et simplement.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = SystemColors.ControlDark;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color DisabledFillColor { get; set; } = SystemColors.Control;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color DisabledForeColor { get; set; } = SystemColors.GrayText;

        /// <summary>
        /// Applique d'un coup les quatre couleurs d'un état, et repositionne le
        /// fond courant si aucune interaction n'est en cours — c'est ce qui rend
        /// la transition de thème animée (MainForm.AnimateThemeTransition, qui
        /// rappelle la palette interpolée ~60 fois par seconde) fluide sur les
        /// boutons comme sur le reste.
        /// </summary>
        public void SetColors(Color fill, Color hover, Color pressed, Color border, Color foreColor)
        {
            FillColor = fill;
            HoverFillColor = hover;
            PressedFillColor = pressed;
            BorderColor = border;
            ForeColor = foreColor;

            if (!_hovering && !_pressing && _fadeTimer == null)
                _currentFill = fill;

            Invalidate();
        }

        private void InvalidateIcon()
        {
            _icon?.Dispose();
            _icon = null;
            _iconColor = Color.Empty;
            Invalidate();
        }

        private Bitmap? GetIcon(Color color)
        {
            if (string.IsNullOrEmpty(_iconName)) return null;
            if (_icon != null && _iconColor == color) return _icon;

            _icon?.Dispose();
            _icon = IconManager.Render(_iconName, _iconSize, color);
            _iconColor = color;
            return _icon;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovering = true;
            FadeTo(_pressing ? PressedFillColor : HoverFillColor);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovering = false;
            FadeTo(FillColor);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _pressing = true;
            FadeTo(PressedFillColor);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressing = false;
            FadeTo(_hovering ? HoverFillColor : FillColor);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            // Sans ça, un bouton désactivé pendant qu'il est survolé garderait
            // sa couleur de survol jusqu'au prochain passage de la souris.
            _hovering = false;
            _pressing = false;
            Invalidate();
        }

        private void FadeTo(Color target)
        {
            if (!Enabled) return;

            _targetFill = target;
            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();

            var start = _currentFill;
            var sw = Stopwatch.StartNew();
            const int durationMs = 120;

            var timer = new System.Windows.Forms.Timer { Interval = 15 };
            _fadeTimer = timer;
            timer.Tick += (s, e) =>
            {
                if (IsDisposed)
                {
                    timer.Stop();
                    timer.Dispose();
                    if (ReferenceEquals(_fadeTimer, timer)) _fadeTimer = null;
                    return;
                }

                var t = Math.Min(1f, (float)sw.ElapsedMilliseconds / durationMs);
                _currentFill = Lerp(start, _targetFill, t);
                Invalidate();

                if (t >= 1f)
                {
                    timer.Stop();
                    timer.Dispose();
                    if (ReferenceEquals(_fadeTimer, timer)) _fadeTimer = null;
                }
            };
            timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Les coins arrondis découvrent la surface porteuse : sans ce
            // remplissage préalable, ils garderaient le contenu précédent du
            // buffer (traînées noires au redimensionnement).
            using (var surfaceBrush = new SolidBrush(ResolvedSurface))
                g.FillRectangle(surfaceBrush, ClientRectangle);

            var fill = !Enabled ? DisabledFillColor
                     : _currentFill.IsEmpty ? FillColor
                     : _currentFill;
            var textColor = Enabled ? ForeColor : DisabledForeColor;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (var path = RoundedRect(rect, CornerRadius))
            {
                using (var brush = new SolidBrush(fill))
                    g.FillPath(brush, path);

                if (!BorderColor.IsEmpty && Enabled)
                    using (var pen = new Pen(BorderColor))
                        g.DrawPath(pen, path);
            }

            // Anneau de focus clavier : dessiné à l'intérieur, pour ne pas
            // agrandir l'encombrement du bouton ni mordre sur ses voisins.
            if (Focused && ShowFocusCues)
            {
                var focusRect = new Rectangle(2, 2, Width - 5, Height - 5);
                if (focusRect.Width > 0 && focusRect.Height > 0)
                {
                    using var focusPath = RoundedRect(focusRect, Math.Max(2, CornerRadius - 2));
                    using var focusPen = new Pen(textColor) { Width = 1.4f };
                    g.DrawPath(focusPen, focusPath);
                }
            }

            DrawContent(g, textColor);
        }

        /// <summary>
        /// Icône et texte forment un GROUPE centré, mesuré puis posé d'un bloc.
        /// C'est la différence avec le Button natif, qui répartit d'abord la
        /// largeur entre deux zones puis centre le texte dans la sienne : le
        /// même libellé y paraissait centré sur un bouton large et collé à
        /// l'icône sur un bouton étroit (v1.22.1).
        /// </summary>
        private void DrawContent(Graphics g, Color textColor)
        {
            var icon = GetIcon(textColor);
            var text = Text ?? string.Empty;

            var available = Width - 2 * EdgePadding;
            if (available <= 0) available = Width;

            var textSize = string.IsNullOrEmpty(text)
                ? Size.Empty
                : TextRenderer.MeasureText(g, text, Font, new Size(available, Height), TextFormatFlags.NoPadding);

            var iconWidth = icon?.Width ?? 0;
            var gap = IconTextGap(iconWidth, text);
            var groupWidth = iconWidth + gap + textSize.Width;

            var x = ContentStartX(Width, groupWidth);

            if (icon != null)
            {
                g.DrawImage(icon, x, (Height - icon.Height) / 2, icon.Width, icon.Height);
                x += iconWidth + gap;
            }

            if (!string.IsNullOrEmpty(text))
            {
                // Largeur restante bornée : un libellé plus long que le bouton
                // se termine en « ... » au lieu de déborder sur ses voisins.
                var textRect = new Rectangle(x, 0, Math.Max(0, Width - x - EdgePadding / 2), Height);
                TextRenderer.DrawText(g, text, Font, textRect, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fadeTimer?.Stop();
                _fadeTimer?.Dispose();
                _fadeTimer = null;
                _icon?.Dispose();
                _icon = null;
            }
            base.Dispose(disposing);
        }

        internal static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var d = radius * 2;

            // Un diamètre supérieur au côté ferait retomber les arcs sur des
            // coins carrés sans lever d'erreur (piège rencontré en v1.19.1 sur
            // ThemedProgressBar) : on borne plutôt que de laisser dégénérer.
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;
            if (d <= 0)
            {
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

        private static Color Lerp(Color a, Color b, float t)
        {
            if (a.IsEmpty) return b;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}
