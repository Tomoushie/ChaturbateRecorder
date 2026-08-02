using System.Drawing;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    public enum AppTheme { Light, Dark }

    /// <summary>
    /// Applique un thème clair/sombre en parcourant récursivement tous les
    /// contrôles du formulaire, y compris ceux imbriqués dans des GroupBox.
    /// Équivalent de Apply-Theme côté PowerShell.
    /// </summary>
    public static class ThemeManager
    {
        public static void Apply(Control root, AppTheme theme)
        {
            Color bg, panel, fg, btn;

            if (theme == AppTheme.Dark)
            {
                bg    = Color.FromArgb(32, 32, 34);
                panel = Color.FromArgb(48, 48, 51);
                fg    = Color.WhiteSmoke;
                btn   = Color.FromArgb(66, 66, 70);
            }
            else
            {
                bg    = Color.FromArgb(240, 240, 240);
                panel = Color.White;
                fg    = Color.Black;
                btn   = Color.FromArgb(225, 225, 225);
            }

            root.BackColor = bg;
            root.ForeColor = fg;

            ApplyRecursive(root, bg, panel, fg, btn);
        }

        private static void ApplyRecursive(Control control, Color bg, Color panel, Color fg, Color btn)
        {
            switch (control)
            {
                case GroupBox gb:
                    gb.ForeColor = fg;
                    gb.BackColor = bg;
                    break;
                case Button b:
                    b.BackColor = btn;
                    b.ForeColor = fg;
                    b.FlatStyle = FlatStyle.Flat;
                    break;
                case TextBox tb:
                    tb.BackColor = panel;
                    tb.ForeColor = fg;
                    break;
                case ListBox lb:
                    lb.BackColor = panel;
                    lb.ForeColor = fg;
                    break;
                case ComboBox cb:
                    cb.BackColor = panel;
                    cb.ForeColor = fg;
                    break;
                case PictureBox pb:
                    pb.BackColor = panel;
                    break;
                case Label lbl:
                    lbl.ForeColor = fg;
                    break;
            }

            foreach (Control child in control.Controls)
                ApplyRecursive(child, bg, panel, fg, btn);
        }
    }
}
