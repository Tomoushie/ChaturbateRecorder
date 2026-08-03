using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Dialogue affiché par CrashReporter après une exception non gérée.
    /// Volontairement sans dépendance à ThemeManager (rendu simple et
    /// statique, plus fiable qu'un rendu thémé quand l'état de l'app peut
    /// être corrompu) — mais utilise Localization (24.0) : la langue est
    /// relue depuis les paramètres persistés par CrashReporter, puisqu'il
    /// n'a pas de référence à l'instance MainForm en cours.
    /// </summary>
    public class CrashReportForm : Form
    {
        private readonly string? _crashFile;

        public CrashReportForm(Exception ex, string? crashFile, bool isTerminating, AppLanguage language)
        {
            _crashFile = crashFile;
            InitializeComponent(ex, crashFile, isTerminating, language);
        }

        private void InitializeComponent(Exception ex, string? crashFile, bool isTerminating, AppLanguage language)
        {
            Text = Localization.Get("crash.windowTitle", language);
            ClientSize = new Size(480, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            Font = new Font("Segoe UI", 10F);

            var titleLabel = new Label
            {
                Text = isTerminating
                    ? Localization.Get("crash.titleFatal", language)
                    : Localization.Get("crash.titleRecoverable", language),
                Location = new Point(12, 12),
                Size = new Size(456, 40),
            };

            var detailsBox = new TextBox
            {
                Location = new Point(12, 56),
                Size = new Size(456, 180),
                Multiline = true,
                ReadOnly = true,
                TabStop = false,
                ScrollBars = ScrollBars.Vertical,
                Text = crashFile != null
                    ? Localization.Format("crash.detailsWithFile", language, ex.GetType().Name, ex.Message, crashFile)
                    : Localization.Format("crash.detailsNoFile", language, ex.GetType().Name, ex.Message),
            };

            var openFolderButton = new Button
            {
                Text = Localization.Get("crash.openFolder", language),
                Location = new Point(12, 246),
                Size = new Size(180, 30),
                Enabled = crashFile != null,
            };
            openFolderButton.Click += (s, e) => OnOpenFolderClick();

            var restartButton = new Button
            {
                Text = Localization.Get("crash.restart", language),
                Location = new Point(200, 246),
                Size = new Size(120, 30),
            };
            restartButton.Click += (s, e) => CrashReporter.RestartApplication();

            var closeButton = new Button
            {
                Text = isTerminating ? Localization.Get("button.close", language) : Localization.Get("crash.continue", language),
                Location = new Point(368, 246),
                Size = new Size(100, 30),
            };
            closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { titleLabel, detailsBox, openFolderButton, restartButton, closeButton });
        }

        private void OnOpenFolderClick()
        {
            if (_crashFile == null) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{_crashFile}\"",
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Best effort : si l'explorateur ne peut pas être lancé, on ne
                // bloque pas davantage un dialogue déjà affiché suite à un crash.
            }
        }
    }
}
