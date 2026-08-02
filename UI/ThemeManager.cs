using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    public enum AppTheme { Light, Dark }

    /// <summary>
    /// Applique un thème clair/sombre en parcourant récursivement tous les
    /// contrôles du formulaire, y compris ceux imbriqués dans des
    /// RoundedGroupPanel (8.1, remplace les anciens GroupBox). Palette
    /// "Windows 11" (7.5) : boutons en couleur d'accent, texte clair dessus
    /// dans les deux thèmes. Boutons sans bordure, coins arrondis, et états
    /// survol/appui distincts façon "Fluent" (7.1/8.4).
    /// </summary>
    public static class ThemeManager
    {
        public static void Apply(Control root, AppTheme theme)
        {
            Color bg, panel, fg, btn, btnFg, border, shadow;

            if (theme == AppTheme.Dark)
            {
                bg     = Color.FromArgb(0x1E, 0x1E, 0x1E);
                panel  = Color.FromArgb(0x2D, 0x2D, 0x2D);
                fg     = Color.FromArgb(0xE6, 0xE6, 0xE6);
                btn    = Color.FromArgb(0x3A, 0x96, 0xDD);
                border = Color.FromArgb(0x45, 0x45, 0x45);
                shadow = Color.FromArgb(60, 0, 0, 0);
            }
            else
            {
                bg     = Color.FromArgb(0xF3, 0xF3, 0xF3);
                panel  = Color.White;
                fg     = Color.FromArgb(0x1A, 0x1A, 0x1A);
                btn    = Color.FromArgb(0x00, 0x78, 0xD4);
                border = Color.FromArgb(0xD9, 0xD9, 0xD9);
                shadow = Color.FromArgb(28, 0, 0, 0);
            }

            // Les boutons sont en couleur d'accent (bleu) dans les deux thèmes :
            // leur texte doit donc toujours être clair pour rester lisible,
            // indépendamment du texte général (fg) qui lui varie clair/sombre.
            btnFg = Color.White;

            root.BackColor = bg;
            root.ForeColor = fg;

            ApplyRecursive(root, bg, panel, fg, btn, btnFg, border, shadow);
        }

        private static void ApplyRecursive(Control control, Color bg, Color panel, Color fg, Color btn, Color btnFg, Color border, Color shadow)
        {
            switch (control)
            {
                case RoundedGroupPanel rgp:
                    rgp.ForeColor = fg;
                    rgp.BackColor = bg;
                    rgp.TitleColor = fg;
                    rgp.BorderColor = border;
                    rgp.ShadowColor = shadow;
                    rgp.Font = new Font(rgp.Font.FontFamily, rgp.Font.Size, FontStyle.Bold);
                    rgp.Invalidate();
                    break;
                case Panel pnl:
                    // Panels génériques (contentPanel, advancedOptionsPanel, lignes
                    // de job) : même fond que le reste, sinon leur BackColor par
                    // défaut (gris clair système) reste visible même en thème sombre.
                    pnl.BackColor = bg;
                    break;
                case Button b:
                    b.BackColor = btn;
                    b.ForeColor = btnFg;
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderSize = 0;
                    // États survol/appui (8.4) : éclairci au survol, assombri à
                    // l'appui — WinForms les gère nativement une fois définis,
                    // pas besoin de gérer MouseEnter/MouseDown à la main.
                    b.FlatAppearance.MouseOverBackColor = Lighten(btn, 24);
                    b.FlatAppearance.MouseDownBackColor = Darken(btn, 24);
                    b.Padding = new Padding(8, 0, 8, 0);
                    b.Cursor = Cursors.Hand;
                    ApplyRoundedRegion(b, 6);
                    break;
                case TextBox tb:
                    tb.BackColor = panel;
                    tb.ForeColor = fg;
                    break;
                case ListBox lb:
                    lb.BackColor = panel;
                    lb.ForeColor = fg;
                    break;
                case ListView lv:
                    lv.BackColor = panel;
                    lv.ForeColor = fg;
                    break;
                case ComboBox cb:
                    cb.BackColor = panel;
                    cb.ForeColor = fg;
                    break;
                case CheckBox cbx:
                    cbx.ForeColor = fg;
                    break;
                case PictureBox pb:
                    pb.BackColor = panel;
                    break;
                case Label lbl:
                    lbl.ForeColor = fg;
                    break;
            }

            foreach (Control child in control.Controls)
                ApplyRecursive(child, bg, panel, fg, btn, btnFg, border, shadow);
        }

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
