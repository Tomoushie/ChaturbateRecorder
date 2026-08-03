using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    public enum AppTheme { Light, Dark }

    /// <summary>
    /// Applique un thème clair/sombre en parcourant récursivement tous les
    /// contrôles du formulaire, y compris ceux imbriqués dans des
    /// RoundedGroupPanel (8.1, remplace les anciens GroupBox). Palette
    /// "Windows 11" (7.5) : boutons en couleur d'accent, texte clair dessus
    /// dans les deux thèmes. Boutons sans bordure, coins arrondis, états
    /// survol/appui animés en douceur (7.1/9.2) façon "Fluent".
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Jeu de couleurs d'un thème — exposé séparément de <see cref="Apply"/>
        /// pour permettre à MainForm d'interpoler entre deux palettes lors d'une
        /// transition animée clair/sombre (9.2), sans dupliquer les valeurs.
        /// </summary>
        public readonly record struct Palette(Color Bg, Color Panel, Color Fg, Color Btn, Color BtnFg, Color Border, Color Shadow);

        public static Palette GetPalette(AppTheme theme) => theme == AppTheme.Dark
            ? new Palette(
                Bg: Color.FromArgb(0x1E, 0x1E, 0x1E),
                Panel: Color.FromArgb(0x2D, 0x2D, 0x2D),
                Fg: Color.FromArgb(0xE6, 0xE6, 0xE6),
                Btn: Color.FromArgb(0x3A, 0x96, 0xDD),
                // Les boutons sont en couleur d'accent (bleu) dans les deux thèmes :
                // leur texte doit donc toujours être clair pour rester lisible,
                // indépendamment du texte général (Fg) qui lui varie clair/sombre.
                BtnFg: Color.White,
                Border: Color.FromArgb(0x45, 0x45, 0x45),
                Shadow: Color.FromArgb(60, 0, 0, 0))
            : new Palette(
                Bg: Color.FromArgb(0xF3, 0xF3, 0xF3),
                Panel: Color.White,
                Fg: Color.FromArgb(0x1A, 0x1A, 0x1A),
                Btn: Color.FromArgb(0x00, 0x78, 0xD4),
                BtnFg: Color.White,
                Border: Color.FromArgb(0xD9, 0xD9, 0xD9),
                Shadow: Color.FromArgb(28, 0, 0, 0));

        public static void Apply(Control root, AppTheme theme) => ApplyPalette(root, GetPalette(theme));

        public static void ApplyPalette(Control root, Palette p)
        {
            root.BackColor = p.Bg;
            root.ForeColor = p.Fg;
            ApplyRecursive(root, p);
        }

        /// <summary>
        /// Interpole deux palettes (9.2) : sert de base à MainForm pour animer
        /// la transition clair/sombre au lieu d'un saut instantané de couleurs.
        /// </summary>
        public static Palette LerpPalette(Palette a, Palette b, float t) => new(
            Lerp(a.Bg, b.Bg, t), Lerp(a.Panel, b.Panel, t), Lerp(a.Fg, b.Fg, t), Lerp(a.Btn, b.Btn, t),
            Lerp(a.BtnFg, b.BtnFg, t), Lerp(a.Border, b.Border, t), Lerp(a.Shadow, b.Shadow, t));

        private static void ApplyRecursive(Control control, Palette p)
        {
            switch (control)
            {
                case RoundedGroupPanel rgp:
                    rgp.ForeColor = p.Fg;
                    rgp.BackColor = p.Bg;
                    rgp.TitleColor = p.Fg;
                    rgp.BorderColor = p.Border;
                    rgp.ShadowColor = p.Shadow;
                    rgp.Font = new Font(rgp.Font.FontFamily, rgp.Font.Size, FontStyle.Bold);
                    rgp.Invalidate();
                    break;
                case Panel pnl:
                    // Panels génériques (contentPanel, advancedOptionsPanel, lignes
                    // de job) : même fond que le reste, sinon leur BackColor par
                    // défaut (gris clair système) reste visible même en thème sombre.
                    pnl.BackColor = p.Bg;
                    break;
                case Button b:
                    b.ForeColor = p.BtnFg;
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderSize = 0;
                    b.Padding = new Padding(8, 0, 8, 0);
                    b.Cursor = Cursors.Hand;
                    ApplyRoundedRegion(b, 6);
                    // États survol/appui animés (9.2) : transition douce de couleur
                    // au lieu du changement instantané natif de FlatAppearance.
                    ConfigureButtonColors(b, p.Btn, Lighten(p.Btn, 24), Darken(p.Btn, 24));
                    break;
                case TextBox tb:
                    tb.BackColor = p.Panel;
                    tb.ForeColor = p.Fg;
                    break;
                // RichTextBox ne dérive pas de TextBox (tous deux dérivent de
                // TextBoxBase) : sans ce cas, le corps du dialogue "Nouveautés"
                // resterait blanc sur blanc en thème sombre.
                case RichTextBox rtb:
                    rtb.BackColor = p.Panel;
                    rtb.ForeColor = p.Fg;
                    break;
                case ListBox lb:
                    lb.BackColor = p.Panel;
                    lb.ForeColor = p.Fg;
                    break;
                case ListView lv:
                    lv.BackColor = p.Panel;
                    lv.ForeColor = p.Fg;
                    break;
                case ComboBox cb:
                    cb.BackColor = p.Panel;
                    cb.ForeColor = p.Fg;
                    break;
                case CheckBox cbx:
                    cbx.ForeColor = p.Fg;
                    break;
                case PictureBox pb:
                    pb.BackColor = p.Panel;
                    break;
                // Couleurs dérivées de la palette plutôt qu'ajoutées à Palette :
                // un curseur d'ascenseur n'est ni du texte ni un fond, c'est un
                // intermédiaire entre les deux, et l'interpolation le donne
                // juste dans les deux thèmes sans deux champs de plus à animer
                // pendant la transition clair/sombre.
                case ThemedScrollBar tsb:
                    tsb.TrackColor = p.Panel;
                    tsb.ThumbColor = Lerp(p.Panel, p.Fg, 0.35f);
                    tsb.ThumbHoverColor = Lerp(p.Panel, p.Fg, 0.55f);
                    tsb.Invalidate();
                    break;
                case Label lbl:
                    lbl.ForeColor = p.Fg;
                    break;
            }

            foreach (Control child in control.Controls)
                ApplyRecursive(child, p);
        }

        private sealed class ButtonColorState
        {
            public Color Base, Hover, Pressed;
            public bool Hovering;
            public bool Pressing;
            public System.Windows.Forms.Timer? Timer;
        }

        // ConditionalWeakTable plutôt qu'un Dictionary classique : les lignes
        // d'enregistrement (BuildJobRow dans MainForm) créent et détruisent des
        // boutons dynamiquement — ça évite une fuite mémoire en laissant le GC
        // récupérer l'entrée en même temps que le bouton.
        private static readonly ConditionalWeakTable<Button, ButtonColorState> _buttonColors = new();

        /// <summary>
        /// (Ré)enregistre les couleurs cibles d'un bouton pour l'animation
        /// survol/appui, et câble les gestionnaires de souris une seule fois
        /// par bouton (rappelé à chaque Apply/ApplyPalette, y compris pendant
        /// une transition de thème animée — donc jusqu'à plusieurs fois par
        /// seconde, d'où l'utilisation d'un Timer par état plutôt qu'un cablage
        /// répété des événements).
        /// </summary>
        private static void ConfigureButtonColors(Button b, Color baseColor, Color hoverColor, Color pressedColor)
        {
            if (!_buttonColors.TryGetValue(b, out var state))
            {
                state = new ButtonColorState();
                _buttonColors.Add(b, state);

                b.MouseEnter += (s, e) => { state.Hovering = true; AnimateButtonColor(b, state, state.Pressing ? state.Pressed : state.Hover); };
                b.MouseLeave += (s, e) => { state.Hovering = false; AnimateButtonColor(b, state, state.Base); };
                b.MouseDown += (s, e) => { state.Pressing = true; AnimateButtonColor(b, state, state.Pressed); };
                b.MouseUp += (s, e) => { state.Pressing = false; AnimateButtonColor(b, state, state.Hovering ? state.Hover : state.Base); };
            }

            state.Base = baseColor;
            state.Hover = hoverColor;
            state.Pressed = pressedColor;

            // En dehors de toute interaction en cours, le repos suit directement
            // la couleur de thème — c'est ce qui produit la transition fluide
            // quand cette méthode est rappelée en boucle par une animation de
            // changement de thème (MainForm.AnimateThemeTransition).
            if (!state.Hovering && !state.Pressing && state.Timer == null)
                b.BackColor = baseColor;
        }

        private static void AnimateButtonColor(Button b, ButtonColorState state, Color target)
        {
            state.Timer?.Stop();
            state.Timer?.Dispose();

            var start = b.BackColor;
            var sw = Stopwatch.StartNew();
            const int durationMs = 120;
            var timer = new System.Windows.Forms.Timer { Interval = 15 };
            state.Timer = timer;
            timer.Tick += (s, e) =>
            {
                if (b.IsDisposed) { timer.Stop(); timer.Dispose(); state.Timer = null; return; }

                var t = Math.Min(1f, (float)sw.ElapsedMilliseconds / durationMs);
                b.BackColor = Lerp(start, target, t);
                if (t >= 1f)
                {
                    timer.Stop();
                    timer.Dispose();
                    state.Timer = null;
                }
            };
            timer.Start();
        }

        private static Color Lerp(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));

        private static Color Lighten(Color c, int amount) => Color.FromArgb(
            Math.Min(255, c.R + amount), Math.Min(255, c.G + amount), Math.Min(255, c.B + amount));

        private static Color Darken(Color c, int amount) => Color.FromArgb(
            Math.Max(0, c.R - amount), Math.Max(0, c.G - amount), Math.Max(0, c.B - amount));

        /// <summary>
        /// Coins arrondis (7.1) via une Region calculée sur la taille actuelle
        /// du contrôle — appelée à chaque Apply (démarrage + changement de
        /// thème), donc toujours à jour même si la taille du bouton a changé.
        /// </summary>
        private static void ApplyRoundedRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;

            var d = radius * 2;
            var rect = new Rectangle(0, 0, control.Width, control.Height);
            using var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            control.Region?.Dispose();
            control.Region = new Region(path);
        }
    }
}
