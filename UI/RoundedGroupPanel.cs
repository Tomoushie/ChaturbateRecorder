using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Remplace GroupBox (8.1) : panneau à bordure arrondie avec titre dessiné
    /// à la main (façon carte "Fluent"), plus une ombre légère (8.2). Le
    /// contenu se place dedans exactement comme dans un GroupBox classique —
    /// mêmes coordonnées d'enfants, la bordure du haut reste fine pour ne pas
    /// entrer en collision avec des contrôles placés près du sommet (ex :
    /// grpDonate, qui a un contrôle dès y=12).
    /// </summary>
    public class RoundedGroupPanel : Panel
    {
        private const int TopOffset = 8;
        private const int CornerRadius = 8;
        private const int ShadowSize = 3;

        // DesignerSerializationVisibility.Hidden (WFO1000) : ce contrôle n'est
        // jamais posé via le concepteur WinForms (formulaire construit à la
        // main dans MainForm.InitializeComponent), ces propriétés n'ont donc
        // rien à sérialiser dans un .Designer.cs.
        private string _title = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = Color.FromArgb(0xD9, 0xD9, 0xD9);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TitleColor { get; set; } = Color.FromArgb(0x1A, 0x1A, 0x1A);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ShadowColor { get; set; } = Color.FromArgb(28, 0, 0, 0);

        public RoundedGroupPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bodyRect = new Rectangle(0, TopOffset, Width - 1 - ShadowSize, Height - 1 - TopOffset - ShadowSize);
            if (bodyRect.Width > 0 && bodyRect.Height > 0)
            {
                var shadowRect = new Rectangle(bodyRect.X + ShadowSize, bodyRect.Y + ShadowSize, bodyRect.Width, bodyRect.Height);
                using (var shadowPath = RoundedRect(shadowRect, CornerRadius))
                using (var shadowBrush = new SolidBrush(ShadowColor))
                    g.FillPath(shadowBrush, shadowPath);

                using (var path = RoundedRect(bodyRect, CornerRadius))
                {
                    using var backBrush = new SolidBrush(BackColor);
                    g.FillPath(backBrush, path);
                    using var pen = new Pen(BorderColor);
                    g.DrawPath(pen, path);
                }
            }

            if (!string.IsNullOrEmpty(_title))
            {
                using var titleFont = new Font(Font, FontStyle.Bold);
                var titleSize = g.MeasureString(_title, titleFont);

                // Efface le morceau de bordure sous le titre, comme un GroupBox
                // classique dont le texte "coupe" la ligne du haut.
                using (var eraseBrush = new SolidBrush(BackColor))
                    g.FillRectangle(eraseBrush, 6, 0, titleSize.Width + 8, TopOffset + 2);

                using var titleBrush = new SolidBrush(TitleColor);
                g.DrawString(_title, titleFont, titleBrush, new PointF(10, -1));
            }

            base.OnPaint(e);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
