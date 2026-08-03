using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Guide de démarrage pas-à-pas : affiché automatiquement au tout premier
    /// lancement (voir MainForm.ShowFirstRunDialogs) et réutilisable à tout
    /// moment via le bouton "Guide de démarrage". Contenu piloté par
    /// Localization (24.0) : la langue est figée à la construction (celle
    /// active à l'ouverture), pas de retraduction à chaud si l'utilisateur
    /// change de langue pendant que ce dialogue est ouvert.
    /// </summary>
    public class TutorialForm : Form
    {
        private static readonly (string TitleKey, string BodyKey)[] Steps =
        {
            ("tutorial.step1.title", "tutorial.step1.body"),
            ("tutorial.step2.title", "tutorial.step2.body"),
            ("tutorial.step3.title", "tutorial.step3.body"),
            ("tutorial.step4.title", "tutorial.step4.body"),
            ("tutorial.step5.title", "tutorial.step5.body"),
            ("tutorial.step6.title", "tutorial.step6.body"),
            ("tutorial.step7.title", "tutorial.step7.body"),
        };

        private readonly AppLanguage _language;
        private int _stepIndex;
        private Label titleLabel = null!;
        private Label bodyLabel = null!;
        private Label progressLabel = null!;
        private Button backButton = null!;
        private Button nextButton = null!;

        public TutorialForm(AppLanguage language)
        {
            _language = language;
            InitializeComponent();
            RenderStep();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = Localization.Get("tutorial.windowTitle", _language);
            ClientSize = new Size(480, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;

            titleLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(440, 28),
                Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
            };

            bodyLabel = new Label
            {
                Location = new Point(20, 56),
                Size = new Size(440, 196),
                AutoSize = false,
            };

            progressLabel = new Label
            {
                Location = new Point(20, 270),
                AutoSize = true,
            };

            backButton = new Button { Text = Localization.Get("tutorial.back", _language), Location = new Point(220, 264), Size = new Size(110, 28) };
            nextButton = new Button { Text = Localization.Get("tutorial.next", _language), Location = new Point(336, 264), Size = new Size(124, 28) };

            backButton.Click += (s, e) => { if (_stepIndex > 0) { _stepIndex--; RenderStep(); } };
            nextButton.Click += (s, e) =>
            {
                if (_stepIndex < Steps.Length - 1) { _stepIndex++; RenderStep(); }
                else Close();
            };

            Controls.AddRange(new Control[] { titleLabel, bodyLabel, progressLabel, backButton, nextButton });

            ResumeLayout(false);
        }

        private void RenderStep()
        {
            var (titleKey, bodyKey) = Steps[_stepIndex];
            titleLabel.Text = Localization.Get(titleKey, _language);
            bodyLabel.Text = Localization.Get(bodyKey, _language);
            progressLabel.Text = Localization.Format("tutorial.progress", _language, _stepIndex + 1, Steps.Length);
            backButton.Enabled = _stepIndex > 0;
            nextButton.Text = _stepIndex == Steps.Length - 1
                ? Localization.Get("tutorial.finish", _language)
                : Localization.Get("tutorial.next", _language);
        }
    }
}
