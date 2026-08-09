using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Dialogue "Nouveautés". Une MessageBox suffisait tant qu'une seule
    /// version était annoncée ; depuis qu'une mise à jour peut en franchir
    /// plusieurs d'un coup (Changelog.GetChangesSince), le corps dépasse la
    /// hauteur d'écran — et une MessageBox ne défile pas, elle tronque
    /// silencieusement. D'où cette fenêtre : même contenu, mais qui défile et
    /// se redimensionne.
    ///
    /// RichTextBox plutôt qu'un empilement de Labels dans un Panel AutoScroll :
    /// le retour à la ligne et les en-têtes en gras sont natifs, là où des
    /// Labels demanderaient de mesurer la hauteur de chaque puce à la main (et
    /// de la remesurer à chaque redimensionnement).
    ///
    /// Le défilement, lui, n'est PAS celui du RichTextBox : le contrôle est
    /// dimensionné à la hauteur totale de son contenu, à l'intérieur d'un
    /// panneau qui le rogne, et défiler revient à décaler son Top. C'est le
    /// mécanisme qu'emploie Panel.AutoScroll, et il permet d'utiliser
    /// ThemedScrollBar — l'ascenseur natif, lui, resterait clair en thème
    /// sombre.
    /// </summary>
    public class ChangelogForm : Form
    {
        private readonly (string Text, bool IsHeader)[] _lines;
        private Panel _viewport = null!;
        private RichTextBox _body = null!;
        private ThemedScrollBar _scrollBar = null!;
        private ThemedButton _closeButton = null!;
        private Font? _headerFont;
        private int _contentHeight;
        private bool _updatingExtent;
        private bool _ready;

        public ChangelogForm((string Version, string[] Changes)[] announced, string version, AppTheme theme)
        {
            _lines = BuildLines(announced);

            InitializeComponent(version);
            ThemeManager.Apply(this, theme);
            // Le panneau ne sert que de fenêtre de rognage : il doit se
            // confondre avec le corps, pas avec le fond du formulaire (ce que
            // lui donne le cas Panel de ThemeManager), sinon le couloir de
            // l'ascenseur trancherait sur le reste.
            _viewport.BackColor = _body.BackColor;
        }

        /// <summary>
        /// Mise en forme du contenu, séparée de la fenêtre pour être testable
        /// sans instancier de contrôle WinForms (voir ChangelogTests).
        ///
        /// Une seule version annoncée : pas d'en-tête, il ne ferait que répéter
        /// le titre de la fenêtre. Plusieurs : un en-tête par version, sinon
        /// rien ne dit laquelle apporte quoi.
        /// </summary>
        internal static (string Text, bool IsHeader)[] BuildLines(
            (string Version, string[] Changes)[] announced)
        {
            if (announced.Length == 0)
                return new[] { (Localization.Get("changelog.noDetails"), false) };

            var withHeaders = announced.Length > 1;
            var lines = new List<(string Text, bool IsHeader)>();

            foreach (var (version, changes) in announced)
            {
                if (withHeaders)
                    lines.Add((Localization.Format("changelog.versionHeader", version), true));

                if (changes.Length == 0)
                    lines.Add((Localization.Get("changelog.noDetails"), false));
                else
                    lines.AddRange(changes.Select(change => ("• " + change, false)));
            }

            return lines.ToArray();
        }

        private void InitializeComponent(string version)
        {
            SuspendLayout();

            Text = Localization.Format("changelog.title", version);
            ClientSize = new Size(560, 420);
            MinimumSize = new Size(420, 200);
            StartPosition = FormStartPosition.CenterParent;
            // Sizable (contrairement aux autres dialogues du projet, tous en
            // FixedDialog) : la longueur du contenu varie du simple correctif à
            // une dizaine de versions cumulées.
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 10F);

            _viewport = new Panel { Location = new Point(16, 16), Size = new Size(528, 348) };

            _body = new RichTextBox
            {
                Location = new Point(0, 0),
                Width = _viewport.Width - ThemedScrollBar.Thickness,
                Height = _viewport.Height,
                ReadOnly = true,
                TabStop = false,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
            };
            // Seule façon fiable de connaître la hauteur réelle du contenu :
            // GetPositionFromCharIndex ne renseigne que sur la partie visible.
            _body.ContentsResized += (s, e) =>
            {
                _contentHeight = e.NewRectangle.Height;
                UpdateScrollExtent();
            };
            _body.MouseWheel += (s, e) =>
            {
                ScrollByWheel(e.Delta);
                // Indispensable, pas cosmétique : sans Handled, WinForms laisse
                // DefWindowProc relayer la molette au parent, donc à
                // OnMouseWheel ci-dessous — et le contenu défilerait deux fois
                // par cran dès que le corps a le focus.
                if (e is HandledMouseEventArgs handled) handled.Handled = true;
            };

            _scrollBar = new ThemedScrollBar
            {
                Location = new Point(_viewport.Width - ThemedScrollBar.Thickness, 0),
                Size = new Size(ThemedScrollBar.Thickness, _viewport.Height),
                Visible = false,
            };
            _scrollBar.ValueChanged += (s, e) => _body.Top = -_scrollBar.Value;

            _body.SelectionChanged += (s, e) => EnsureCaretVisible();

            _viewport.Controls.AddRange(new Control[] { _body, _scrollBar });
            _viewport.Resize += (s, e) => UpdateScrollExtent();

            _closeButton = new ThemedButton
            {
                Text = Localization.Get("button.close"),
                Location = new Point(434, 372),
                Size = new Size(110, 32),
            };
            _closeButton.Role = ButtonRole.Primary; // seul accent de la fenêtre (39.0)
            _closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { _viewport, _closeButton });

            // Anchor posé APRÈS Controls.Add : avant, la marge est calculée sur
            // un Parent encore null et le contrôle part hors de la fenêtre au
            // premier redimensionnement.
            _viewport.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _body.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _scrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;

            AcceptButton = _closeButton;
            CancelButton = _closeButton;

            ResumeLayout(false);
        }

        /// <summary>
        /// Remplissage à l'affichage plutôt que dans le constructeur : écrire
        /// dans un RichTextBox force la création de son handle, et le faire
        /// avant celui du formulaire parent fait recréer le contrôle au moment
        /// où il est réellement parenté.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _headerFont ??= new Font(_body.Font, FontStyle.Bold);

            foreach (var (text, isHeader) in _lines)
            {
                // Ligne vide de séparation avant chaque groupe sauf le premier.
                if (isHeader && _body.TextLength > 0)
                    _body.AppendText(Environment.NewLine);

                // Aucune SelectionColor n'est posée : le texte garde la couleur
                // par défaut du contrôle, donc celle du thème appliqué par
                // ThemeManager — sinon le gras resterait noir en thème sombre.
                _body.SelectionFont = isHeader ? _headerFont : _body.Font;
                _body.AppendText(text + Environment.NewLine);
            }

            _body.SelectionStart = 0;
            ShrinkToContent();
            UpdateScrollExtent();
            _ready = true;
        }

        /// <summary>
        /// Le corps prend le focus dès qu'on clique dedans, et les flèches ou
        /// PagePrécédente/Suivante y déplacent alors le caret. Le RichTextBox
        /// étant dimensionné à la hauteur totale de son contenu, il n'a rien à
        /// faire défiler de son côté : sans ce rattrapage, le caret sortirait
        /// de la partie visible sans que rien ne bouge à l'écran.
        /// </summary>
        private void EnsureCaretVisible()
        {
            if (!_ready || !_scrollBar.Visible) return;

            // Coordonnée dans le contenu, le contrôle ne défilant jamais
            // lui-même.
            var caretTop = _body.GetPositionFromCharIndex(_body.SelectionStart).Y;
            var caretBottom = caretTop + _body.Font.Height;

            if (caretTop < _scrollBar.Value)
                _scrollBar.Value = caretTop;
            else if (caretBottom > _scrollBar.Value + _viewport.Height)
                _scrollBar.Value = caretBottom - _viewport.Height;
        }

        /// <summary>
        /// La MessageBox remplacée s'ajustait à son contenu ; à taille fixe, le
        /// cas le plus fréquent (une version, deux lignes de correctif)
        /// laisserait un grand vide sous le texte. On réduit donc la hauteur au
        /// contenu réel — jamais au-delà de la taille par défaut, un contenu
        /// plus long relevant du défilement, ni sous MinimumSize (que WinForms
        /// n'applique pas de lui-même à ce stade, la fenêtre n'étant pas encore
        /// affichée : d'où le Math.Max explicite).
        /// </summary>
        private void ShrinkToContent()
        {
            var unused = _viewport.Height - (_contentHeight + _body.Font.Height);
            if (unused <= 0) return;

            Height = Math.Max(MinimumSize.Height, Height - unused);
            // La position CenterParent a été calculée sur la hauteur d'avant.
            CenterToParent();
        }

        /// <summary>
        /// Accorde l'ascenseur et la hauteur du RichTextBox au contenu. Le
        /// couloir de l'ascenseur est réservé en permanence (le RichTextBox est
        /// toujours plus étroit que le panneau, même sans défilement) : le
        /// rendre au contenu quand l'ascenseur disparaît élargirait le texte,
        /// donc changerait sa hauteur, donc pourrait refaire apparaître
        /// l'ascenseur — un aller-retour sans fin.
        /// </summary>
        private void UpdateScrollExtent()
        {
            // Réentrance : redimensionner le corps peut relancer la mise en
            // page du RichTextBox, donc ContentsResized, donc cette méthode.
            if (_contentHeight <= 0 || _updatingExtent) return;
            _updatingExtent = true;

            try { ApplyScrollExtent(); }
            finally { _updatingExtent = false; }
        }

        private void ApplyScrollExtent()
        {
            var visibleHeight = _viewport.Height;
            var totalHeight = _contentHeight + _body.Font.Height;

            _body.Height = Math.Max(totalHeight, visibleHeight);
            _scrollBar.LargeChange = visibleHeight;
            _scrollBar.Maximum = totalHeight;
            _scrollBar.Visible = totalHeight > visibleHeight;

            if (!_scrollBar.Visible) _scrollBar.Value = 0;
            _body.Top = -_scrollBar.Value;
        }

        private void ScrollByWheel(int delta)
        {
            var lines = SystemInformation.MouseWheelScrollLines;
            // -1 signifie "une page par cran" (réglage système).
            var step = lines < 0 ? _viewport.Height : lines * _body.Font.Height;
            _scrollBar.Value -= delta / SystemInformation.MouseWheelScrollDelta * step;
        }

        /// <summary>
        /// Le message de molette part au contrôle qui a le focus — le bouton
        /// Fermer la plupart du temps — et remonte jusqu'ici. Le traiter au
        /// niveau du formulaire couvre donc tous les cas d'un coup.
        /// </summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollByWheel(e.Delta);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _headerFont?.Dispose();
            base.Dispose(disposing);
        }
    }
}
