using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Note de légalité (98.0), accessible depuis la fenêtre principale.
    ///
    /// **Pourquoi dans l'application et pas seulement sur le site** : la
    /// confusion que ce texte dissipe — enregistrer n'est pas diffuser, et
    /// violer des conditions d'utilisation n'est pas commettre une infraction —
    /// concerne l'utilisateur au moment où il enregistre, pas au moment où il
    /// visite le dépôt. Un texte qui n'existe que sur GitHub ne sera lu que par
    /// ceux qui n'en ont pas besoin.
    ///
    /// Le bouton vit sur la première rangée de la barre du haut, donc visible
    /// en mode simple ET en mode avancé, contrairement à Diagnostic ou au
    /// guide : c'est une information dont personne ne doit être privé.
    /// </summary>
    public class LegalForm : Form
    {
        private TextBox bodyTextBox = null!;
        private Button closeButton = null!;

        public LegalForm(AppTheme theme, AppLanguage language)
        {
            InitializeComponent(language);
            // ThemeManager traite déjà le cas TextBox (fond de panneau, texte du
            // thème). Une premiere version reposait ces deux couleurs depuis
            // celles du formulaire APRES Apply : le texte s'affichait alors dans
            // une couleur sans rapport, constate a la capture. Ne rien
            // surcharger ici.
            ThemeManager.Apply(this, theme);
        }

        private void InitializeComponent(AppLanguage language)
        {
            SuspendLayout();

            string L(string key) => Localization.Get(key, language);

            Text = L("window.legal");
            ClientSize = new Size(640, 460);
            MinimumSize = new Size(480, 320);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10F);
            MaximizeBox = false;
            ShowInTaskbar = false;

            // TextBox en lecture seule plutôt qu'un Label : le texte est long,
            // il doit pouvoir défiler, et l'utilisateur doit pouvoir le
            // sélectionner pour le copier — c'est une note juridique, on ne le
            // force pas à la recopier à la main.
            bodyTextBox = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(616, 396),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                TabStop = false,
                // Environment.NewLine, pas le saut de ligne seul : un TextBox
                // multiligne WinForms ne l'interprète pas, et les paragraphes
                // se collaient les uns aux autres — constaté à la capture,
                // invisible autrement.
                Text = L("legal.body").Replace("\n", Environment.NewLine),
            };

            closeButton = new Button
            {
                Text = L("button.close"),
                Location = new Point(528, 420),
                Size = new Size(100, 28),
            };
            closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { bodyTextBox, closeButton });

            // Ancrage APRÈS Controls.Add — piège documenté en bas de CLAUDE.md.
            bodyTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            AcceptButton = closeButton;
            CancelButton = closeButton;

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
