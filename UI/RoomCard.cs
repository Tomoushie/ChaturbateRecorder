using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Une carte par salon (97.0 étape 2b) : c'est elle qui remplace les trois
    /// panneaux Favoris, Surveillance et Enregistrements en cours, qui
    /// décrivaient les mêmes salons sous trois angles.
    ///
    /// **Adaptative, et c'est le compromis retenu** : compacte au repos, elle
    /// s'étend quand l'enregistrement démarre pour loger progression et durée.
    /// Des cartes toutes hautes comme celles d'Olived Pro ne laissent voir que
    /// quatre salons ; des lignes de tableau ne logent pas de barre de
    /// progression. L'extension ne coûte de la place que là où il se passe
    /// quelque chose.
    ///
    /// **La couleur porte l'état, jamais la décoration** : une pastille verte
    /// dit « en ligne », orange « reconnexion », rouge « échec ». Ajouter des
    /// couleurs qui ne signifient rien rendrait illisibles celles qui signifient
    /// quelque chose.
    /// </summary>
    internal sealed class RoomCard : Panel
    {
        internal const int CompactHeight = 60;
        internal const int ExpandedHeight = 92;

        private const int Radius = 10;
        private const int PadX = 14;
        private const int IconSize = 18;
        private const int ToggleWidth = 34;
        private const int ToggleHeight = 18;

        private RoomRowState _state = RoomRowState.Idle;
        private bool _autoRecord;
        private bool _hovering;
        private bool _toggleHovering;
        private bool _indeterminate;
        private int _pulse;
        private System.Windows.Forms.Timer? _pulseTimer;

        internal event EventHandler? AutoRecordToggled;

        public RoomCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = CompactHeight;
            Margin = new Padding(0, 0, 0, 8);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal string RoomName { get; set; } = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal string PlatformIcon { get; set; } = "camera";

        /// <summary>Ligne secondaire : durée, taille, débit. Vide au repos.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal string Detail { get; set; } = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal string StateLabel { get; set; } = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int Progress { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal RoomRowState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                Height = IsExpanded(value) ? ExpandedHeight : CompactHeight;
                SyncPulsation();
                Invalidate();
            }
        }

        /// <summary>
        /// Barre indéterminée, pour un enregistrement qui n'a pas encore annoncé
        /// de pourcentage — c'est le cas normal d'un direct, dont la durée n'est
        /// pas connue d'avance. Sans elle une capture bien vivante s'afficherait
        /// figée à 0 %, ce que l'ancienne interface évitait avec le mode Marquee
        /// de ProgressBar (que la carte, se dessinant elle-même, n'a pas).
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool Indeterminate
        {
            get => _indeterminate;
            set
            {
                if (_indeterminate == value) return;
                _indeterminate = value;
                SyncPulsation();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool AutoRecord
        {
            get => _autoRecord;
            set { if (_autoRecord == value) return; _autoRecord = value; Invalidate(); }
        }

        // Couleurs posées par ThemeManager.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal ThemeManager.Palette Palette { get; set; } = ThemeManager.GetPalette(AppTheme.Dark);

        /// <summary>
        /// Une carte s'étend seulement quand il se passe quelque chose de
        /// mesurable. « Terminé » ou « échec » n'ont pas de progression à
        /// montrer : les garder hauts gaspillerait la place au moment où on
        /// parcourt sa liste.
        /// </summary>
        internal static bool IsExpanded(RoomRowState state) =>
            state is RoomRowState.Recording or RoomRowState.Reconnecting;

        /// <summary>
        /// Couleur de la pastille d'état. Isolée et testable : c'est le seul
        /// endroit qui traduit un état en couleur, et une erreur y ferait
        /// afficher « en ligne » en rouge.
        /// </summary>
        internal static Color StateColor(RoomRowState state, ThemeManager.Palette p) => state switch
        {
            RoomRowState.Live => p.Success,
            RoomRowState.Recording => p.Accent,
            RoomRowState.Reconnecting => p.Warning,
            RoomRowState.Failed => p.Danger,
            // « Inexistant » est un échec DÉFINITIF, pas une panne passagère :
            // il mérite la couleur d'alerte, sans quoi une faute de frappe se
            // confondrait avec un salon simplement hors ligne.
            RoomRowState.NotFound => p.Danger,
            _ => p.FgMuted,
        };

        /// <summary>
        /// Rectangle de l'interrupteur « auto ». Séparé du dessin pour que le
        /// test de clic et le rendu ne puissent pas diverger — un interrupteur
        /// qui ne réagit pas là où il est dessiné est le pire des défauts, on le
        /// prend pour une panne.
        /// </summary>
        internal static Rectangle ToggleBounds(int cardWidth, int cardHeight, int actionsWidth)
        {
            var x = cardWidth - actionsWidth - PadX - ToggleWidth;
            var y = (Math.Min(cardHeight, CompactHeight) - ToggleHeight) / 2;
            return new Rectangle(x, y, ToggleWidth, ToggleHeight);
        }

        /// <summary>Largeur réservée aux boutons d'action, à droite.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int ActionsWidth { get; set; } = 200;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var surToggle = ToggleBounds(Width, Height, ActionsWidth).Contains(e.Location);
            if (surToggle == _toggleHovering && _hovering) return;
            _toggleHovering = surToggle;
            _hovering = true;
            Cursor = surToggle ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovering = false;
            _toggleHovering = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!ToggleBounds(Width, Height, ActionsWidth).Contains(e.Location)) return;
            AutoRecord = !AutoRecord;
            AutoRecordToggled?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var p = Palette;

            using (var fond = new SolidBrush(Parent?.BackColor ?? p.Bg))
                g.FillRectangle(fond, ClientRectangle);

            var corps = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var chemin = Arrondi(corps, Radius))
            {
                // Le survol éclaircit d'un cran au lieu d'ajouter une ombre :
                // une ombre portée sur une liste de vingt cartes fait vibrer
                // tout l'écran au passage de la souris.
                var surface = _hovering ? Melange(p.Card, p.Fg, 0.05f) : p.Card;
                using var fond = new SolidBrush(surface);
                g.FillPath(fond, chemin);
                using var bord = new Pen(_hovering ? Melange(p.Border, p.Fg, 0.20f) : p.Border);
                g.DrawPath(bord, chemin);
            }

            var couleurEtat = StateColor(State, p);

            // Liseré d'état sur le bord gauche : lisible en un balayage
            // vertical, alors qu'une pastille oblige à lire chaque ligne.
            using (var liseré = new SolidBrush(couleurEtat))
            using (var chemin = Arrondi(new Rectangle(0, 0, Radius * 2, Height - 1), Radius))
            {
                var ancien = g.Clip;
                g.SetClip(new Rectangle(0, 0, 4, Height));
                g.FillPath(liseré, chemin);
                g.Clip = ancien;
            }

            var x = PadX;
            try
            {
                using var icone = IconManager.Render(PlatformIcon, IconSize, p.FgMuted);
                g.DrawImage(icone, x, (Math.Min(Height, CompactHeight) - IconSize) / 2, IconSize, IconSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pictogramme '{PlatformIcon}' indisponible : {ex.Message}");
            }
            x += IconSize + 12;

            using (var texte = new SolidBrush(p.Fg))
            using (var gras = new Font(Font, FontStyle.Bold))
                g.DrawString(RoomName, gras, texte, x, 11);

            using (var texte = new SolidBrush(couleurEtat))
                g.DrawString(StateLabel, Font, texte, x, 30);

            DessineInterrupteur(g, p);

            if (!IsExpanded(State)) return;

            var piste = new Rectangle(PadX, CompactHeight + 2, Width - 2 * PadX, 6);
            using (var fond = new SolidBrush(p.Input))
            using (var chemin = Arrondi(piste, 3))
                g.FillPath(fond, chemin);

            var rempli = _indeterminate
                ? PulseBounds(piste, _pulse)
                : new Rectangle(piste.X, piste.Y, (int)(piste.Width * Math.Clamp(Progress, 0, 100) / 100.0), piste.Height);

            if (rempli.Width > 4)
            {
                using var pinceau = new SolidBrush(couleurEtat);
                using var chemin = Arrondi(rempli, 3);
                g.FillPath(pinceau, chemin);
            }

            if (Detail.Length > 0)
                using (var texte = new SolidBrush(p.FgMuted))
                    g.DrawString(Detail, Font, texte, PadX, CompactHeight + 12);
        }

        /// <summary>
        /// Position du segment qui glisse quand la progression est inconnue.
        /// Séparée du dessin et pure, comme <see cref="ToggleBounds"/> : une
        /// erreur de bornes ferait déborder le segment hors de sa piste, par
        /// dessus le bord arrondi de la carte, et seule une capture prise au bon
        /// instant le montrerait.
        /// </summary>
        internal static Rectangle PulseBounds(Rectangle piste, int phase)
        {
            var segment = Math.Max(24, piste.Width * 3 / 10);
            // Le segment entre par la gauche et sort par la droite : la course
            // vaut donc la piste PLUS sa propre largeur.
            var x = piste.X - segment + (piste.Width + segment) * Math.Clamp(phase, 0, 100) / 100;

            var gauche = Math.Max(x, piste.X);
            var droite = Math.Min(x + segment, piste.Right);
            return new Rectangle(gauche, piste.Y, Math.Max(0, droite - gauche), piste.Height);
        }

        /// <summary>
        /// Le minuteur ne tourne que si la carte est à la fois indéterminée et
        /// étendue : une carte compacte n'a pas de piste où dessiner, et faire
        /// battre un timer par carte au repos coûterait pour rien sur une liste
        /// de vingt salons.
        /// </summary>
        private void SyncPulsation()
        {
            if (_indeterminate && IsExpanded(_state))
            {
                if (_pulseTimer != null) return;
                _pulseTimer = new System.Windows.Forms.Timer { Interval = 50 };
                _pulseTimer.Tick += (s, e) =>
                {
                    _pulse = (_pulse + 3) % 101;
                    // Seule la bande basse est repeinte : réinvalider la carte
                    // entière vingt fois par seconde redessinerait le nom, l'état
                    // et le pictogramme sans qu'aucun n'ait changé.
                    Invalidate(new Rectangle(0, CompactHeight, Width, Height - CompactHeight));
                };
                _pulseTimer.Start();
                return;
            }

            if (_pulseTimer == null) return;
            _pulseTimer.Stop();
            _pulseTimer.Dispose();
            _pulseTimer = null;
            _pulse = 0;
        }

        /// <summary>
        /// Sans ça le minuteur survivrait à la carte et appellerait Invalidate
        /// sur un contrôle détruit — le piège déjà payé sur ThemedProgressBar en
        /// mode Marquee, dont le Dispose est la seule chose qui arrête le timer.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pulseTimer?.Stop();
                _pulseTimer?.Dispose();
                _pulseTimer = null;
            }
            base.Dispose(disposing);
        }

        private void DessineInterrupteur(Graphics g, ThemeManager.Palette p)
        {
            var rect = ToggleBounds(Width, Height, ActionsWidth);
            var actif = AutoRecord;

            var fond = actif ? p.Accent : Melange(p.Card, p.Fg, _toggleHovering ? 0.22f : 0.14f);
            using (var pinceau = new SolidBrush(fond))
            using (var chemin = Arrondi(rect, rect.Height / 2))
                g.FillPath(pinceau, chemin);

            var d = rect.Height - 6;
            var cx = actif ? rect.Right - d - 3 : rect.X + 3;
            using (var pastille = new SolidBrush(actif ? p.AccentFg : p.FgMuted))
                g.FillEllipse(pastille, cx, rect.Y + 3, d, d);
        }

        private static GraphicsPath Arrondi(Rectangle r, int rayon)
        {
            var chemin = new GraphicsPath();
            if (rayon <= 0 || r.Width <= 0 || r.Height <= 0) { chemin.AddRectangle(r); return chemin; }
            var d = rayon * 2;
            chemin.AddArc(r.X, r.Y, d, d, 180, 90);
            chemin.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            chemin.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            chemin.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            chemin.CloseFigure();
            return chemin;
        }

        private static Color Melange(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }
}
