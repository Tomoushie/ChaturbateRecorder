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
    /// Volontairement sans dépendance à ThemeManager/Localization : au moment
    /// où ce dialogue s'affiche, l'état de l'application peut être corrompu —
    /// un rendu simple et statique est plus fiable qu'un rendu thémé.
    /// </summary>
    public class CrashReportForm : Form
    {
        private readonly string? _crashFile;

        public CrashReportForm(Exception ex, string? crashFile, bool isTerminating)
        {
            _crashFile = crashFile;
            InitializeComponent(ex, crashFile, isTerminating);
        }

        private void InitializeComponent(Exception ex, string? crashFile, bool isTerminating)
        {
            Text = "Erreur inattendue";
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
                    ? "⚠️ Chaturbate Recorder a rencontré une erreur fatale et doit fermer."
                    : "⚠️ Chaturbate Recorder a rencontré une erreur inattendue.",
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
                Text = $"{ex.GetType().Name} : {ex.Message}\r\n\r\n" +
                       (crashFile != null
                           ? $"Rapport complet enregistré dans :\r\n{crashFile}"
                           : "Le rapport détaillé n'a pas pu être enregistré sur disque."),
            };

            var openFolderButton = new Button
            {
                Text = "Ouvrir le dossier des logs",
                Location = new Point(12, 246),
                Size = new Size(180, 30),
                Enabled = crashFile != null,
            };
            openFolderButton.Click += (s, e) => OnOpenFolderClick();

            var restartButton = new Button
            {
                Text = "Redémarrer",
                Location = new Point(200, 246),
                Size = new Size(120, 30),
            };
            restartButton.Click += (s, e) => CrashReporter.RestartApplication();

            var closeButton = new Button
            {
                Text = isTerminating ? "Fermer" : "Continuer",
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
