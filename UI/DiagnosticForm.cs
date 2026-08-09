using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using SentinelGuard;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Panneau de diagnostic (2.3) : réutilise directement les validateurs de
    /// SentinelGuard (hash des binaires, ACL, dossier d'exécution) plutôt que de
    /// dupliquer leur logique, et ajoute les informations non déjà exposées
    /// ailleurs (versions .NET/app/yt-dlp/ffmpeg, joignabilité réseau).
    ///
    /// Volontairement NON traduit (décision de périmètre en 24.0) : ce rapport
    /// est fait pour être collé dans un ticket GitHub. Le traduire ferait
    /// arriver des rapports en deux langues au mainteneur, qui aurait à
    /// deviner à quel champ français correspond chaque libellé anglais. Même
    /// raisonnement que pour les logs et CrashReportForm.
    /// </summary>
    public class DiagnosticForm : Form
    {
        private TextBox _reportBox = null!;
        private ThemedButton _refreshButton = null!;

        public DiagnosticForm()
        {
            InitializeComponent();
            _ = RefreshReportAsync();
        }

        private void InitializeComponent()
        {
            Text = "Diagnostic";
            ClientSize = new Size(520, 460);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10F);

            _reportBox = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(496, 396),
                Multiline = true,
                ReadOnly = true,
                TabStop = false,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
            };

            var copyButton = new ThemedButton { Text = "Copier", Location = new Point(12, 418), Size = new Size(100, 30) };
            copyButton.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_reportBox.Text)) Clipboard.SetText(_reportBox.Text);
            };

            _refreshButton = new ThemedButton { Text = "Actualiser", Location = new Point(120, 418), Size = new Size(100, 30) };
            _refreshButton.Click += (s, e) => _ = RefreshReportAsync();

            var closeButton = new ThemedButton { Text = "Fermer", Location = new Point(408, 418), Size = new Size(100, 30) };
            closeButton.Role = ButtonRole.Primary; // seul accent de la fenêtre (39.0)
            closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { _reportBox, copyButton, _refreshButton, closeButton });
        }

        private async Task RefreshReportAsync()
        {
            _refreshButton.Enabled = false;
            _reportBox.Text = BuildStaticReport() +
                "\r\n--- Binaires (versions) ---\r\n" +
                "yt-dlp.exe : (vérification...)\r\n" +
                "ffmpeg.exe : (vérification...)\r\n" +
                "\r\n--- Réseau ---\r\n" +
                "chaturbate.com : (vérification...)\r\n" +
                "api.github.com : (vérification...)\r\n";

            var ytDlpVersion = await GetBinaryVersionAsync(AppConfig.YtDlpPath, "--version");
            var ffmpegVersion = await GetBinaryVersionAsync(AppConfig.FFmpegPath, "-version");
            var chaturbateReachable = await CheckReachableAsync("https://chaturbate.com");
            var githubReachable = await CheckReachableAsync("https://api.github.com");

            var sb = new StringBuilder();
            sb.Append(BuildStaticReport());
            sb.AppendLine("--- Binaires (versions) ---");
            sb.AppendLine($"yt-dlp.exe : {ytDlpVersion}");
            sb.AppendLine($"ffmpeg.exe : {ffmpegVersion}");
            sb.AppendLine();
            sb.AppendLine("--- Réseau ---");
            sb.AppendLine($"chaturbate.com : {(chaturbateReachable ? "joignable" : "injoignable")}");
            sb.AppendLine($"api.github.com : {(githubReachable ? "joignable" : "injoignable")}");

            if (!IsDisposed)
            {
                _reportBox.Text = sb.ToString();
                _refreshButton.Enabled = true;
            }
        }

        private static string BuildStaticReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Application : v{typeof(DiagnosticForm).Assembly.GetName().Version?.ToString(3)}");
            sb.AppendLine($".NET : {RuntimeInformation.FrameworkDescription}");
            sb.AppendLine($"Système : {Environment.OSVersion} ({(Environment.Is64BitProcess ? "64" : "32")} bits)");
            sb.AppendLine();

            sb.AppendLine("--- Intégrité des binaires (hash SHA256) ---");
            sb.AppendLine($"yt-dlp.exe : {DescribeHash("yt-dlp", AppConfig.YtDlpPath, AppConfig.YtDlpExpectedSha256)}");
            sb.AppendLine($"ffmpeg.exe : {DescribeHash("ffmpeg", AppConfig.FFmpegPath, AppConfig.FfmpegExpectedSha256)}");
            sb.AppendLine();

            sb.AppendLine("--- Dossier d'exécution ---");
            sb.AppendLine($"Emplacement autorisé : {(WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir) ? "oui" : "non")}");
            sb.AppendLine();

            sb.AppendLine("--- ACL (droits d'écriture élargis détectés ?) ---");
            sb.AppendLine(DescribeAcl("Dossier d'exécution", AppConfig.AppDir));
            sb.AppendLine(DescribeAcl("Dossier de capture", AppConfig.CaptureDir));
            sb.AppendLine(DescribeAcl("Dossier de logs", AppConfig.LogDir));
            sb.AppendLine();

            sb.AppendLine($"Proxy configuré : {(string.IsNullOrWhiteSpace(AppConfig.ProxyUrl) ? "aucun" : AppConfig.ProxyUrl)}");
            sb.AppendLine();

            return sb.ToString();
        }

        private static string DescribeHash(string binaryKey, string path, string expectedHash)
        {
            if (!File.Exists(path)) return "introuvable";
            if (BinaryVerifier.VerifyFileHash(path, expectedHash)) return "OK (version testée par le mainteneur)";

            var actualHash = BinaryVerifier.ComputeSha256(path);
            var trustedHash = Services.TrustedBinaryStore.GetTrustedHash(binaryKey);
            if (actualHash != null && string.Equals(actualHash, trustedHash, StringComparison.OrdinalIgnoreCase))
                return "OK (approuvé manuellement)";

            return "ÉCHEC (hash inattendu — pas encore approuvé)";
        }

        private static string DescribeAcl(string label, string path)
        {
            return AclValidator.TryFindBroadWriteAccess(path, out var details)
                ? $"{label} : permissive — {details}"
                : $"{label} : OK";
        }

        private static async Task<string> GetBinaryVersionAsync(string path, string arguments)
        {
            if (!File.Exists(path)) return "introuvable";

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                process.Start();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var output = await process.StandardOutput.ReadToEndAsync(cts.Token);
                try { await process.WaitForExitAsync(cts.Token); } catch (OperationCanceledException) { }

                var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                return string.IsNullOrEmpty(firstLine) ? "version inconnue" : firstLine;
            }
            catch (Exception ex)
            {
                return $"erreur ({ex.Message})";
            }
        }

        private static async Task<bool> CheckReachableAsync(string url)
        {
            try
            {
                var handler = new HttpClientHandler();
                if (!string.IsNullOrWhiteSpace(AppConfig.ProxyUrl))
                {
                    handler.Proxy = new System.Net.WebProxy(AppConfig.ProxyUrl);
                    handler.UseProxy = true;
                }

                using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(4) };
                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
