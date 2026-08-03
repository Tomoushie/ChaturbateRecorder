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
using ChaturbateRecorderApp.Security;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Panneau de diagnostic (2.3) : réutilise directement les validateurs de
    /// Security/ (hash des binaires, ACL, dossier d'exécution) plutôt que de
    /// dupliquer leur logique, et ajoute les informations non déjà exposées
    /// ailleurs (versions .NET/app/yt-dlp/ffmpeg, joignabilité réseau).
    /// Langue figée à la construction (24.0), comme TutorialForm.
    /// </summary>
    public class DiagnosticForm : Form
    {
        private readonly AppLanguage _language;
        private TextBox _reportBox = null!;
        private Button _refreshButton = null!;

        public DiagnosticForm(AppLanguage language)
        {
            _language = language;
            InitializeComponent();
            _ = RefreshReportAsync();
        }

        private void InitializeComponent()
        {
            Text = Localization.Get("button.diagnostic", _language);
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

            var copyButton = new Button { Text = Localization.Get("diag.copy", _language), Location = new Point(12, 418), Size = new Size(100, 30) };
            copyButton.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_reportBox.Text)) Clipboard.SetText(_reportBox.Text);
            };

            _refreshButton = new Button { Text = Localization.Get("button.refresh", _language), Location = new Point(120, 418), Size = new Size(100, 30) };
            _refreshButton.Click += (s, e) => _ = RefreshReportAsync();

            var closeButton = new Button { Text = Localization.Get("button.close", _language), Location = new Point(408, 418), Size = new Size(100, 30) };
            closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { _reportBox, copyButton, _refreshButton, closeButton });
        }

        private async Task RefreshReportAsync()
        {
            _refreshButton.Enabled = false;
            var checking = Localization.Get("diag.checking", _language);
            _reportBox.Text = BuildStaticReport() +
                $"\r\n{Localization.Get("diag.sectionBinaryVersions", _language)}\r\n" +
                $"yt-dlp.exe : {checking}\r\n" +
                $"ffmpeg.exe : {checking}\r\n" +
                $"\r\n{Localization.Get("diag.sectionNetwork", _language)}\r\n" +
                $"chaturbate.com : {checking}\r\n" +
                $"api.github.com : {checking}\r\n";

            var ytDlpVersion = await GetBinaryVersionAsync(AppConfig.YtDlpPath, "--version");
            var ffmpegVersion = await GetBinaryVersionAsync(AppConfig.FFmpegPath, "-version");
            var chaturbateReachable = await CheckReachableAsync("https://chaturbate.com");
            var githubReachable = await CheckReachableAsync("https://api.github.com");

            var sb = new StringBuilder();
            sb.Append(BuildStaticReport());
            sb.AppendLine(Localization.Get("diag.sectionBinaryVersions", _language));
            sb.AppendLine($"yt-dlp.exe : {ytDlpVersion}");
            sb.AppendLine($"ffmpeg.exe : {ffmpegVersion}");
            sb.AppendLine();
            sb.AppendLine(Localization.Get("diag.sectionNetwork", _language));
            sb.AppendLine($"chaturbate.com : {(chaturbateReachable ? Localization.Get("diag.reachable", _language) : Localization.Get("diag.unreachable", _language))}");
            sb.AppendLine($"api.github.com : {(githubReachable ? Localization.Get("diag.reachable", _language) : Localization.Get("diag.unreachable", _language))}");

            if (!IsDisposed)
            {
                _reportBox.Text = sb.ToString();
                _refreshButton.Enabled = true;
            }
        }

        private string BuildStaticReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Localization.Format("diag.application", _language, typeof(DiagnosticForm).Assembly.GetName().Version?.ToString(3)));
            sb.AppendLine($".NET : {RuntimeInformation.FrameworkDescription}");
            sb.AppendLine(Localization.Format("diag.system", _language, Environment.OSVersion, Environment.Is64BitProcess ? "64" : "32"));
            sb.AppendLine();

            sb.AppendLine(Localization.Get("diag.sectionHashIntegrity", _language));
            sb.AppendLine($"yt-dlp.exe : {DescribeHash("yt-dlp", AppConfig.YtDlpPath, AppConfig.YtDlpExpectedSha256)}");
            sb.AppendLine($"ffmpeg.exe : {DescribeHash("ffmpeg", AppConfig.FFmpegPath, AppConfig.FfmpegExpectedSha256)}");
            sb.AppendLine();

            sb.AppendLine(Localization.Get("diag.sectionExecDir", _language));
            sb.AppendLine(Localization.Format("diag.authorizedLocation", _language,
                WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir) ? Localization.Get("diag.yes", _language) : Localization.Get("diag.no", _language)));
            sb.AppendLine();

            sb.AppendLine(Localization.Get("diag.sectionAcl", _language));
            sb.AppendLine(DescribeAcl(Localization.Get("diag.execDirLabel", _language), AppConfig.AppDir));
            sb.AppendLine(DescribeAcl(Localization.Get("diag.captureDirLabel", _language), AppConfig.CaptureDir));
            sb.AppendLine(DescribeAcl(Localization.Get("diag.logDirLabel", _language), AppConfig.LogDir));
            sb.AppendLine();

            sb.AppendLine(Localization.Format("diag.proxyConfigured", _language,
                string.IsNullOrWhiteSpace(AppConfig.ProxyUrl) ? Localization.Get("diag.none", _language) : AppConfig.ProxyUrl));
            sb.AppendLine();

            return sb.ToString();
        }

        private string DescribeHash(string binaryKey, string path, string expectedHash)
        {
            if (!File.Exists(path)) return Localization.Get("diag.notFound", _language);
            if (BinaryVerifier.VerifyFileHash(path, expectedHash)) return Localization.Get("diag.hashOkMaintainer", _language);

            var actualHash = BinaryVerifier.ComputeSha256(path);
            var trustedHash = Services.TrustedBinaryStore.GetTrustedHash(binaryKey);
            if (actualHash != null && string.Equals(actualHash, trustedHash, StringComparison.OrdinalIgnoreCase))
                return Localization.Get("diag.hashOkApproved", _language);

            return Localization.Get("diag.hashFailed", _language);
        }

        private string DescribeAcl(string label, string path)
        {
            return AclValidator.TryFindBroadWriteAccess(path, out var details)
                ? Localization.Format("diag.aclPermissive", _language, label, details)
                : Localization.Format("diag.aclOk", _language, label);
        }

        private async Task<string> GetBinaryVersionAsync(string path, string arguments)
        {
            if (!File.Exists(path)) return Localization.Get("diag.notFound", _language);

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
                return string.IsNullOrEmpty(firstLine) ? Localization.Get("diag.unknownVersion", _language) : firstLine;
            }
            catch (Exception ex)
            {
                return Localization.Format("diag.versionError", _language, ex.Message);
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
