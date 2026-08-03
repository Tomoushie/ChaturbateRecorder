using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Guide de démarrage pas-à-pas : affiché automatiquement au tout premier
    /// lancement (voir MainForm.ShowFirstRunDialogs) et réutilisable à tout
    /// moment via le bouton "Guide de démarrage".
    /// </summary>
    public class TutorialForm : Form
    {
        /// <summary>
        /// Clés de traduction (24.0) plutôt que le texte lui-même : le contenu
        /// est résolu dans RenderStep, donc au moment de l'affichage. Résoudre
        /// ici, à l'initialisation statique, figerait le guide dans la langue
        /// active au démarrage et ignorerait un changement fait entre-temps
        /// dans la fenêtre Paramètres.
        /// </summary>
        internal static readonly (string TitleKey, string BodyKey)[] Steps =
        {
            ("tutorial.welcome.title",  "tutorial.welcome.body"),
            ("tutorial.start.title",    "tutorial.start.body"),
            ("tutorial.quality.title",  "tutorial.quality.body"),
            ("tutorial.saveDir.title",  "tutorial.saveDir.body"),
            ("tutorial.privacy.title",  "tutorial.privacy.body"),
            ("tutorial.tracking.title", "tutorial.tracking.body"),
            ("tutorial.security.title", "tutorial.security.body"),
        };

        private int _stepIndex;
        private Label titleLabel = null!;
        private Label bodyLabel = null!;
        private Label progressLabel = null!;
        private Button backButton = null!;
        private Button nextButton = null!;

        public TutorialForm()
        {
            InitializeComponent();
            RenderStep();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Même clé que le bouton qui ouvre cette fenêtre : le titre et le
            // bouton doivent dire la même chose, y compris s'il évolue.
            Text = Localization.Get("button.tutorial");
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

            backButton = new Button { Text = Localization.Get("tutorial.back"), Location = new Point(220, 264), Size = new Size(110, 28) };
            nextButton = new Button { Text = Localization.Get("tutorial.next"), Location = new Point(336, 264), Size = new Size(124, 28) };

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
            titleLabel.Text = Localization.Get(titleKey);
            bodyLabel.Text = Localization.Get(bodyKey);
            progressLabel.Text = Localization.Format("tutorial.stepProgress", _stepIndex + 1, Steps.Length);
            backButton.Enabled = _stepIndex > 0;
            nextButton.Text = Localization.Get(_stepIndex == Steps.Length - 1 ? "tutorial.finish" : "tutorial.next");
        }
    }
}
