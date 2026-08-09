using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Carte de contenu — remplace GroupBox depuis 8.1, refondue en 39.0.
    ///
    /// **Ce qui change en 39.0, et pourquoi** :
    /// - La carte est une vraie SURFACE : elle se peint en <c>Card</c> sur un
    ///   fond de fenêtre <c>Bg</c> plus sombre. Avant, elle prenait la couleur
    ///   du fond et n'existait que par un liseré de 1 px — c'est la cause
    ///   principale de l'impression « tout à plat ».
    /// - L'ombre passe d'un rectangle plein décalé de 3 px (une barre grise en
    ///   bas à droite, très visible à la capture) à un halo dégradé.
    /// - Le titre ne COUPE plus la bordure : cet idiome vient du GroupBox de
    ///   Windows 95. Il est dessiné à l'intérieur de la carte, ce qui ne coûte
    ///   aucun déplacement des enfants — le corps commence désormais en haut du
    ///   contrôle au lieu de 8 px plus bas, et récupère exactement la place que
    ///   le titre occupait à cheval sur la bordure.
    /// - La carte encadre elle-même ses champs et ses listes (voir
    ///   <see cref="FrameColor"/>) : une ListView ou un TextBox ne sait pas
    ///   dessiner une bordure suivant le thème, la sienne est peinte par le
    ///   système et restait blanche en thème sombre.
    /// </summary>
    public class RoundedGroupPanel : Panel
    {
        private const int CornerRadius = 8;
        private const int ShadowSize = 4;
        private const int TitleX = 12;
        private const int TitleY = 3;

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
        public Color BorderColor { get; set; } = Color.FromArgb(0xE0, 0xE0, 0xE0);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TitleColor { get; set; } = Color.FromArgb(0x1A, 0x1A, 0x1A);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ShadowColor { get; set; } = Color.FromArgb(24, 0, 0, 0);

        /// <summary>Fond de la fenêtre derrière la carte : les coins arrondis et
        /// le halo d'ombre le laissent voir.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SurfaceColor { get; set; } = Color.FromArgb(0xEF, 0xEF, 0xEF);

        /// <summary>Couleur des cadres dessinés autour des champs et des listes
        /// que porte la carte.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FrameColor { get; set; } = Color.FromArgb(0xE0, 0xE0, 0xE0);

        public RoundedGroupPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var surfaceBrush = new SolidBrush(SurfaceColor))
                g.FillRectangle(surfaceBrush, ClientRectangle);

            // Marges asymétriques : 1 px en haut (le titre doit tenir au-dessus
            // d'enfants placés dès y=22), ShadowSize en bas pour loger le halo.
            var bodyRect = new Rectangle(2, 1, Width - 5, Height - 2 - ShadowSize);
            if (bodyRect.Width <= 0 || bodyRect.Height <= 0)
            {
                base.OnPaint(e);
                return;
            }

            DrawShadow(g, bodyRect);

            using (var path = RoundedRect(bodyRect, CornerRadius))
            {
                using var backBrush = new SolidBrush(BackColor);
                g.FillPath(backBrush, path);
                using var pen = new Pen(BorderColor);
                g.DrawPath(pen, path);
            }

            InputFrame.DrawAll(g, this, FrameColor);

            if (!string.IsNullOrEmpty(_title))
            {
                using var titleFont = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);
                TextRenderer.DrawText(g, _title, titleFont, new Point(TitleX, TitleY), TitleColor,
                    TextFormatFlags.NoPadding);
            }

            base.OnPaint(e);
        }

        /// <summary>
        /// Halo dégradé plutôt qu'un rectangle plein décalé : quelques contours
        /// concentriques d'opacité décroissante, légèrement descendus pour
        /// suggérer une lumière venant du haut. Des CONTOURS et non des surfaces
        /// pleines — empilées, celles-ci s'additionneraient en une masse opaque
        /// au lieu d'un dégradé.
        /// </summary>
        private void DrawShadow(Graphics g, Rectangle bodyRect)
        {
            for (var i = 1; i <= ShadowSize; i++)
            {
                var falloff = 1f - (i - 1) / (float)ShadowSize;
                var alpha = Math.Max(1, (int)(ShadowColor.A * falloff * 0.55f));
                var rect = new Rectangle(bodyRect.X - i, bodyRect.Y - i + 2,
                                         bodyRect.Width + 2 * i, bodyRect.Height + 2 * i);
                if (rect.Width <= 0 || rect.Height <= 0) continue;

                using var pen = new Pen(Color.FromArgb(alpha, ShadowColor.R, ShadowColor.G, ShadowColor.B));
                using var path = RoundedRect(rect, CornerRadius + i);
                g.DrawPath(pen, path);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var d = radius * 2;
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
    }
}
