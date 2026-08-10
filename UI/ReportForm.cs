using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Signalement envoyé depuis l'application (102.0), sans compte GitHub.
    ///
    /// **Le vrai sujet de cette fenêtre est le consentement, pas le
    /// formulaire.** Ce qui part d'ici devient une issue PUBLIQUE, et un
    /// rapport sur un enregistreur de cams peut contenir un nom de salon ou un
    /// chemin de fichier. Trois conséquences de conception :
    /// <list type="bullet">
    /// <item>l'avertissement est visible AVANT le bouton d'envoi, pas après ;</item>
    /// <item>la ligne de contexte jointe est AFFICHÉE, pas seulement décrite —
    /// on ne demande pas de faire confiance sur parole ;</item>
    /// <item>aucun champ de contact n'existe : le suivi passe par l'URL de
    /// l'issue, montrée après l'envoi.</item>
    /// </list>
    ///
    /// **Le chemin GitHub reste offert**, et pas en repli honteux : quelqu'un
    /// qui a un compte a tout intérêt à l'utiliser (il recevra les réponses).
    /// Il devient le seul chemin si aucun relais n'est configuré.
    /// </summary>
    public class ReportForm : Form
    {
        private readonly AppLanguage _language;
        private readonly string _version;
        private readonly string _context;
        private readonly Action _openGitHub;

        private Label introLabel = null!;
        private Label kindLabel = null!;
        private ThemedComboBox kindCombo = null!;
        private Label titleLabel = null!;
        private TextBox titleBox = null!;
        private Label bodyLabel = null!;
        private TextBox bodyBox = null!;
        private Label contextCaption = null!;
        private TextBox contextBox = null!;
        private Label warningLabel = null!;
        private Label statusLabel = null!;
        private ThemedButton sendButton = null!;
        private ThemedButton githubButton = null!;
        private ThemedButton closeButton = null!;

        public ReportForm(AppTheme theme, AppLanguage language, string version, bool advancedMode, Action openGitHub)
        {
            _language = language;
            _version = version;
            _context = ReportSender.BuildContext(advancedMode, language);
            _openGitHub = openGitHub;

            InitializeComponent();
            ThemeManager.Apply(this, theme);
        }

        private string L(string key) => Localization.Get(key, _language);

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = L("window.report");
            ClientSize = new Size(560, 620);
            MinimumSize = new Size(480, 540);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10F);
            MaximizeBox = false;
            ShowInTaskbar = false;

            introLabel = new Label { Location = new Point(12, 12), Size = new Size(536, 38), Text = L("report.intro") };

            // ESPACEMENT : 8 px entre le bas d'un intitulé et le haut de son
            // champ, jamais moins. InputFrame dessine le cadre 3 px AU-DESSUS
            // du contrôle ; à 2 px d'écart, le trait passait donc sous
            // l'intitulé et ressortait à sa droite, là où le Label ne le
            // recouvrait plus. Vu à la capture, invisible en lisant le code.
            kindLabel = new Label { Location = new Point(12, 58), Size = new Size(160, 20), Text = L("report.kind") };
            kindCombo = new ThemedComboBox
            {
                Location = new Point(12, 84),
                Size = new Size(280, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            kindCombo.Items.AddRange(new object[] { L("report.kind.bug"), L("report.kind.feature"), L("report.kind.feedback") });
            kindCombo.SelectedIndex = 0;

            titleLabel = new Label { Location = new Point(12, 118), Size = new Size(300, 20), Text = L("report.title") };
            titleBox = new TextBox { Location = new Point(12, 146), Size = new Size(536, 26), MaxLength = ReportSender.TitleMax };

            bodyLabel = new Label { Location = new Point(12, 180), Size = new Size(420, 20), Text = L("report.body") };
            bodyBox = new TextBox
            {
                Location = new Point(12, 208),
                Size = new Size(536, 180),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = ReportSender.BodyMax,
                BorderStyle = BorderStyle.None,
            };

            // Le contexte est MONTRÉ et non résumé : c'est la seule chose qui
            // parte sans avoir été tapée par l'utilisateur, donc la seule sur
            // laquelle il aurait à nous croire.
            contextCaption = new Label { Location = new Point(12, 398), Size = new Size(536, 20), Text = L("report.contextCaption") };
            contextBox = new TextBox
            {
                Location = new Point(12, 426),
                Size = new Size(536, 26),
                ReadOnly = true,
                TabStop = false,
                BorderStyle = BorderStyle.None,
                Text = $"v{_version} · {_context}",
            };

            warningLabel = new Label { Location = new Point(12, 462), Size = new Size(536, 56), Text = L("report.publicWarning") };

            statusLabel = new Label { Location = new Point(12, 522), Size = new Size(536, 40), Text = "" };

            sendButton = new ThemedButton { Location = new Point(12, 570), Size = new Size(200, 30), Text = L("report.send") };
            sendButton.Role = ButtonRole.Primary;
            sendButton.Click += async (s, e) => await SendAsync();

            githubButton = new ThemedButton { Location = new Point(224, 570), Size = new Size(210, 30), Text = L("report.viaGitHub") };
            githubButton.Click += (s, e) => { _openGitHub(); Close(); };

            closeButton = new ThemedButton { Location = new Point(448, 570), Size = new Size(100, 30), Text = L("button.close") };
            closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                introLabel, kindLabel, kindCombo, titleLabel, titleBox, bodyLabel, bodyBox,
                contextCaption, contextBox, warningLabel, statusLabel,
                sendButton, githubButton, closeButton,
            });

            // Ancrages APRÈS Controls.Add — piège documenté en bas de CLAUDE.md.
            introLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            titleBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            bodyBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            contextCaption.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            contextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            warningLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            sendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            githubButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            ThemeManager.SetTextRole(kindLabel, TextRole.Caption);
            ThemeManager.SetTextRole(titleLabel, TextRole.Caption);
            ThemeManager.SetTextRole(bodyLabel, TextRole.Caption);
            ThemeManager.SetTextRole(contextCaption, TextRole.Caption);
            ThemeManager.SetTextRole(contextBox, TextRole.Caption);
            // L'avertissement reste en contraste PLEIN, contrairement aux
            // autres intitulés : c'est la seule chose de cette fenêtre que
            // l'utilisateur ne peut pas deviner, et elle est irréversible une
            // fois l'envoi parti. L'atténuer en ferait la ligne la moins lue.

            CancelButton = closeButton;

            // Sans relais configuré, l'envoi depuis l'application n'existe pas :
            // le bouton disparaît au lieu de rester là à échouer, et GitHub
            // devient l'action principale.
            if (!ReportSender.IsConfigured)
            {
                sendButton.Visible = false;
                githubButton.Location = new Point(12, 560);
                githubButton.Role = ButtonRole.Primary;
                warningLabel.Text = L("report.noRelay");
            }

            ResumeLayout(false);
            PerformLayout();
        }

        private ReportKind SelectedKind => kindCombo.SelectedIndex switch
        {
            1 => ReportKind.Feature,
            2 => ReportKind.Feedback,
            _ => ReportKind.Bug,
        };

        private async System.Threading.Tasks.Task SendAsync()
        {
            var invalid = ReportSender.Validate(titleBox.Text, bodyBox.Text);
            if (invalid != null)
            {
                SetStatus(L(invalid));
                return;
            }

            SetBusy(true);
            SetStatus(L("report.sending"));

            var result = await ReportSender.SendAsync(
                SelectedKind, titleBox.Text, bodyBox.Text, _version, _context);

            if (IsDisposed || Disposing) return;
            SetBusy(false);

            if (!result.Success)
            {
                // La fenêtre RESTE ouverte et le texte saisi intact : perdre ce
                // que quelqu'un vient d'écrire parce que le réseau a hoqueté
                // serait la pire façon de traiter un signalement.
                SetStatus(L(ReportSender.MessageKey(result.ErrorCode)));
                return;
            }

            ShowSuccess(result.IssueUrl);
        }

        private void SetBusy(bool busy)
        {
            sendButton.Enabled = !busy;
            titleBox.ReadOnly = busy;
            bodyBox.ReadOnly = busy;
            kindCombo.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void SetStatus(string message) => statusLabel.Text = message;

        /// <summary>
        /// L'URL de l'issue est le SEUL moyen pour l'utilisateur de retrouver
        /// sa demande : aucune adresse ne lui a été demandée, donc personne ne
        /// peut le prévenir. Elle est donc proposée à l'ouverture, et le
        /// message dit de la garder.
        /// </summary>
        private void ShowSuccess(string issueUrl)
        {
            SetStatus(L("report.sent"));

            var answer = MessageBox.Show(this,
                Localization.Get("report.sentBody", _language).Replace("\n", Environment.NewLine) +
                Environment.NewLine + Environment.NewLine + issueUrl,
                L("window.report"), MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (answer == DialogResult.Yes)
            {
                try { Process.Start(new ProcessStartInfo(issueUrl) { UseShellExecute = true }); }
                catch (Exception ex) { Logger.Log($"Ouverture de l'issue impossible : {ex.Message}", LogLevel.WARN); }
            }

            Close();
        }
    }
}
