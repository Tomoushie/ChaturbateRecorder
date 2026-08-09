using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Liste déroulante conforme au thème (39.0).
    ///
    /// **Ce que le contrôle natif ne sait pas faire** : sa bordure et son bouton
    /// de déroulement sont peints par Windows à partir des couleurs SYSTÈME.
    /// En thème sombre, une bordure claire encadrait donc chaque champ, et le
    /// menu déroulant s'ouvrait en blanc. `FlatStyle.Flat` ne change que la
    /// forme du bouton, pas ses couleurs.
    ///
    /// **Stratégie, volontairement minimale** : on ne remplace pas le contrôle,
    /// on repeint par-dessus. `DrawMode.OwnerDrawFixed` couvre le texte fermé
    /// ET les éléments de la liste déroulante (WinForms lève <c>DrawItem</c>
    /// pour les deux, distingués par <see cref="DrawItemState.ComboBoxEdit"/>),
    /// et un passage après <c>WM_PAINT</c> redessine la bordure et la flèche.
    /// Le comportement natif — clavier, ouverture, défilement — reste intact,
    /// ce qu'une réécriture complète aurait mis en jeu pour un gain nul.
    /// </summary>
    public class ThemedComboBox : ComboBox
    {
        private const int WM_PAINT = 0x000F;
        // Envoyé par DrawToBitmap — c'est-à-dire par le seul moyen de vérifier
        // ce rendu en capture. Sans ce cas, la capture montrait le rendu NATIF
        // (bordure blanche comprise) et donnait à croire que le repeint ne
        // fonctionnait pas du tout.
        private const int WM_PRINTCLIENT = 0x0318;
        // DrawToBitmap envoie WM_PRINT avec PRF_CHILDREN : c'est ce message-là
        // que reçoit un contrôle natif, et lui seul dans certains cas — le
        // WM_PRINTCLIENT que la procédure par défaut est censée se renvoyer
        // n'arrive pas toujours jusqu'ici.
        private const int WM_PRINT = 0x0317;
        private const int ArrowZoneWidth = 20;
        private const int CornerRadius = 4;

        public ThemedComboBox()
        {
            FlatStyle = FlatStyle.Flat;
            DrawMode = DrawMode.OwnerDrawFixed;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = SystemColors.ControlDark;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ArrowColor { get; set; } = SystemColors.ControlText;

        /// <summary>Fond de la carte qui porte le champ : les coins arrondis le
        /// laissent voir, le contrôle natif ne connaissant que le rectangle.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SurfaceColor { get; set; } = SystemColors.Control;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectionColor { get; set; } = SystemColors.Highlight;

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) { base.OnDrawItem(e); return; }

            var g = e.Graphics;
            // L'élément affiché dans la zone fermée ne doit jamais paraître
            // « sélectionné » : la surbrillance n'a de sens qu'à l'intérieur du
            // menu ouvert.
            var inEditArea = (e.State & DrawItemState.ComboBoxEdit) != 0;
            var highlighted = !inEditArea && (e.State & DrawItemState.Selected) != 0;

            using (var brush = new SolidBrush(highlighted ? SelectionColor : BackColor))
                g.FillRectangle(brush, e.Bounds);

            var text = GetItemText(Items[e.Index]);
            var textRect = new Rectangle(e.Bounds.X + 3, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height);
            TextRenderer.DrawText(g, text, Font, textRect, ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // Après le dessin natif, pas à sa place : la zone de texte et la
            // liste sont déjà correctes (OnDrawItem), il ne reste qu'à couvrir
            // ce que Windows peint avec ses propres couleurs.
            if (IsDisposed || !IsHandleCreated) return;

            if (m.Msg == WM_PAINT)
            {
                using var g = Graphics.FromHwnd(Handle);
                PaintChrome(g);
            }
            else if ((m.Msg == WM_PRINTCLIENT || m.Msg == WM_PRINT) && m.WParam != IntPtr.Zero)
            {
                // Le DC appartient à l'appelant : Graphics.FromHdc ne le libère
                // pas, contrairement à FromHwnd.
                using var g = Graphics.FromHdc(m.WParam);
                PaintChrome(g);
            }
        }

        private void PaintChrome(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Le rendu natif trace SA bordure à 1 px du bord, donc À
            // L'INTÉRIEUR de la nôtre : la dessiner par-dessus ne suffit pas,
            // les deux resteraient visibles (constaté en mettant la nôtre en
            // rouge — le liseré blanc apparaissait juste en dedans). On couvre
            // donc l'anneau de 2 px avant de tracer.
            using (var cover = new Pen(BackColor, 2f))
                if (Width > 4 && Height > 4)
                    g.DrawRectangle(cover, 1, 1, Width - 3, Height - 3);

            var arrowZone = new Rectangle(Width - ArrowZoneWidth - 1, 1, ArrowZoneWidth, Height - 2);
            using (var back = new SolidBrush(BackColor))
                g.FillRectangle(back, arrowZone);

            // Chevron plutôt que le triangle plein du contrôle natif : c'est le
            // signe utilisé par Windows 11 lui-même.
            var cx = arrowZone.Left + arrowZone.Width / 2;
            var cy = arrowZone.Top + arrowZone.Height / 2;
            using (var pen = new Pen(ArrowColor, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
                g.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // Les quatre coins carrés du rendu natif dépassent du tracé arrondi :
            // sans ce masque, ils restent visibles en clair sur fond sombre.
            using (var region = new Region(new Rectangle(0, 0, Width, Height)))
            using (var path = ThemedButton.RoundedRect(rect, CornerRadius))
            {
                region.Exclude(path);
                using var surface = new SolidBrush(SurfaceColor);
                g.FillRegion(surface, region);
            }

            using (var borderPath = ThemedButton.RoundedRect(rect, CornerRadius))
            using (var pen = new Pen(BorderColor))
                g.DrawPath(pen, borderPath);
        }
    }
}
