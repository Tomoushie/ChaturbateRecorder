using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Remerciements aux donateurs (104.0), ouverte depuis le panneau
    /// "Soutenir le projet".
    ///
    /// **Aucun montant n'est affiché, ni rien qui en tienne lieu** : la liste
    /// est triée alphabétiquement par <see cref="SupportersProvider"/>, donc
    /// l'ordre ne peut pas se lire comme un classement. C'est la contrainte
    /// posée par la demande, et elle porte sur l'affichage entier, pas
    /// seulement sur l'absence de chiffres.
    ///
    /// **La liste s'affiche avant le réseau** : la version embarquée est
    /// posée dès la construction, puis remplacée si le site répond. Une
    /// fenêtre vide pendant deux secondes donnerait à croire que personne n'a
    /// jamais donné.
    /// </summary>
    public class SupportersForm : Form
    {
        private readonly AppLanguage _language;

        private Label introLabel = null!;
        private TextBox namesTextBox = null!;
        private Label statusLabel = null!;
        private Label consentLabel = null!;
        private ThemedButton closeButton = null!;

        public SupportersForm(AppTheme theme, AppLanguage language)
        {
            _language = language;
            InitializeComponent(language);
            Render(SupportersProvider.FromEmbedded(), refreshing: true);
            ThemeManager.Apply(this, theme);
        }

        private string L(string key) => Localization.Get(key, _language);

        private void InitializeComponent(AppLanguage language)
        {
            SuspendLayout();

            Text = Localization.Get("window.thanks", language);
            ClientSize = new Size(520, 470);
            MinimumSize = new Size(440, 380);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10F);
            MaximizeBox = false;
            ShowInTaskbar = false;

            // 72 px et non 56 : à 56 la troisième ligne du texte français
            // était coupée net, et l'anglais en fait autant. Mesuré à la
            // capture, invisible autrement — un Label ne signale pas qu'il
            // tronque.
            introLabel = new Label
            {
                Location = new Point(12, 12),
                Size = new Size(496, 72),
                Text = Localization.Get("thanks.intro", language),
            };

            // Même choix que LegalForm : un TextBox en lecture seule plutôt
            // qu'une ListBox. Une liste sélectionnable inviterait à cliquer sur
            // des noms qui ne mènent nulle part, alors qu'un texte se copie —
            // ce qui est exactement ce qu'on peut vouloir faire d'une liste de
            // remerciements.
            namesTextBox = new TextBox
            {
                Location = new Point(12, 92),
                Size = new Size(496, 262),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                TabStop = false,
            };

            statusLabel = new Label
            {
                Location = new Point(12, 360),
                Size = new Size(496, 20),
            };

            // Largeur 380 et non 496 : à pleine largeur cette mention passait
            // SOUS le bouton Fermer, qui en masquait la fin. Elle s'arrête donc
            // avant lui plutôt que de lui disputer la place.
            consentLabel = new Label
            {
                Location = new Point(12, 386),
                Size = new Size(380, 40),
                Text = Localization.Get("thanks.consent", language),
            };

            closeButton = new ThemedButton
            {
                Text = Localization.Get("button.close", language),
                Location = new Point(408, 430),
                Size = new Size(100, 28),
            };
            closeButton.Role = ButtonRole.Primary; // seul accent de la fenêtre (39.0)
            closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { introLabel, namesTextBox, statusLabel, consentLabel, closeButton });

            // Ancrage APRÈS Controls.Add — piège documenté en bas de CLAUDE.md.
            introLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            namesTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // Pas de Right : sinon un agrandissement la ramènerait sous le
            // bouton Fermer, qui est ancré à droite lui aussi.
            consentLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // Les deux lignes du bas commentent la liste sans être la liste.
            ThemeManager.SetTextRole(statusLabel, TextRole.Caption);
            ThemeManager.SetTextRole(consentLabel, TextRole.Caption);

            AcceptButton = closeButton;
            CancelButton = closeButton;

            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// L'actualisation part à l'affichage et non dans le constructeur : la
        /// fenêtre est alors déjà visible, donc l'attente éventuelle se voit
        /// (ligne d'état) au lieu de retarder son ouverture.
        /// </summary>
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // LoadAsync ne lève jamais (voir SupportersProvider) : pas de
            // try/catch ici, qui ne pourrait de toute façon rien rattraper dans
            // un async void.
            var list = await SupportersProvider.LoadAsync();
            if (IsDisposed || Disposing) return;
            Render(list, refreshing: false);
        }

        private void Render(SupportersList list, bool refreshing)
        {
            if (list.Names.Count == 0)
            {
                namesTextBox.Text = L("thanks.empty").Replace("\n", Environment.NewLine);
            }
            else
            {
                // .Lines et non une chaîne jointe par "\n" : un TextBox
                // multiligne WinForms n'interprète pas le saut de ligne seul et
                // collerait tous les noms sur une ligne (piège payé en v1.26.0
                // sur la note de légalité). .Lines fait la jointure lui-même.
                namesTextBox.Lines = list.Names.Select(n => "•  " + n).ToArray();
            }

            statusLabel.Text = refreshing
                ? L("thanks.refreshing")
                : list.Origin == SupportersOrigin.Refreshed
                    ? L("thanks.upToDate")
                    : L("thanks.offline");
        }
    }
}
