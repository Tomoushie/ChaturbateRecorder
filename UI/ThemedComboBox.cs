using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
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
    /// **Stratégie** : le contrôle natif est conservé — clavier, ouverture,
    /// défilement du menu restent les siens — mais l'état FERMÉ est dessiné
    /// intégralement par l'application, à la place du rendu de Windows et non
    /// par-dessus. `DrawMode.OwnerDrawFixed` couvre les éléments du menu
    /// déroulant, qui est une fenêtre à part.
    ///
    /// **Repeindre par-dessus ne suffisait pas** : c'était la première version,
    /// correcte à l'arrêt mais scintillante au survol. Chaque passage de souris
    /// fait repeindre le contrôle natif dans son état « chaud », visible une
    /// fraction de seconde avant d'être recouvert — signalé en utilisation
    /// réelle (110.0) comme des coins qui changent de thème par intermittence.
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
        private const int WM_ERASEBKGND = 0x0014;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr Hdc;
            public int Erase;
            public RECT PaintRect;
            public int Restore;
            public int IncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Reserved;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr window, ref PAINTSTRUCT paint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr window, ref PAINTSTRUCT paint);
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
            // WM_PAINT est traité À LA PLACE du rendu natif, pas après lui.
            //
            // Repeindre par-dessus fonctionnait à l'arrêt mais SCINTILLAIT au
            // survol : chaque passage de souris fait repeindre le contrôle
            // natif dans son état « chaud », visible une fraction de seconde
            // avant d'être recouvert — d'où des coins qui changent de thème
            // par intermittence, parfois plusieurs fois d'affilée (110.0,
            // signalé en utilisation réelle). La seule façon de le supprimer
            // est de ne jamais laisser Windows peindre cette zone.
            //
            // BeginPaint/EndPaint valident la région invalide : sans eux,
            // ignorer WM_PAINT relancerait indéfiniment le message.
            if (m.Msg == WM_PAINT && !IsDisposed && IsHandleCreated)
            {
                var ps = new PAINTSTRUCT();
                var hdc = BeginPaint(m.HWnd, ref ps);
                if (hdc != IntPtr.Zero)
                {
                    try
                    {
                        using var g = Graphics.FromHdc(hdc);
                        PaintClosedState(g);
                    }
                    finally
                    {
                        EndPaint(m.HWnd, ref ps);
                    }
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            // Le fond effacé par Windows produit un éclair clair avant notre
            // dessin : on le lui refuse, PaintClosedState couvre tout.
            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);

            if (IsDisposed || !IsHandleCreated) return;

            // DrawToBitmap passe par WM_PRINT/WM_PRINTCLIENT, jamais par
            // WM_PAINT : ce chemin-là dessine bien APRÈS le rendu natif, faute
            // de pouvoir l'empêcher, mais il ne concerne que les captures.
            if ((m.Msg == WM_PRINTCLIENT || m.Msg == WM_PRINT) && m.WParam != IntPtr.Zero)
            {
                // Le DC appartient à l'appelant : Graphics.FromHdc ne le libère
                // pas, contrairement à FromHwnd.
                using var g = Graphics.FromHdc(m.WParam);
                PaintChrome(g);
            }
        }

        /// <summary>
        /// Dessine le contrôle fermé DE BOUT EN BOUT : fond, texte de l'élément
        /// sélectionné, chevron, bordure. Rien du rendu natif ne subsiste, donc
        /// rien ne peut réapparaître au survol.
        /// </summary>
        private void PaintClosedState(Graphics g)
        {
            using (var back = new SolidBrush(BackColor))
                g.FillRectangle(back, ClientRectangle);

            var text = Text;
            if (!string.IsNullOrEmpty(text))
            {
                var textRect = new Rectangle(4, 0, Math.Max(0, Width - ArrowZoneWidth - 6), Height);
                TextRenderer.DrawText(g, text, Font, textRect, ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            }

            PaintChrome(g);
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
