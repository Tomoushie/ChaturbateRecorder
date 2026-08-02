using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChaturbateRecorderApp.Services
{
    public class UpdateInfo
    {
        public required string Version { get; init; }
        public required string DownloadUrl { get; init; }
        public required string ReleaseNotesUrl { get; init; }
    }

    /// <summary>
    /// Interroge l'API publique GitHub Releases (aucune authentification requise
    /// pour un dépôt public) pour savoir si une version plus récente existe.
    /// </summary>
    public static class UpdateChecker
    {
        private const string ApiUrl = "https://api.github.com/repos/Tomoushie/ChaturbateRecorder/releases/latest";

        public static async Task<UpdateInfo?> CheckForUpdateAsync(string currentVersion)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ChaturbateRecorder-UpdateChecker");
            http.Timeout = TimeSpan.FromSeconds(10);

            var json = await http.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            var latestVersion = tagName.TrimStart('v', 'V');

            if (!IsNewer(latestVersion, currentVersion)) return null;

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(downloadUrl)) return null;

            return new UpdateInfo
            {
                Version = latestVersion,
                DownloadUrl = downloadUrl,
                ReleaseNotesUrl = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "",
            };
        }

        private static bool IsNewer(string latest, string current)
        {
            if (Version.TryParse(latest, out var lv) && Version.TryParse(current, out var cv))
                return lv > cv;
            return string.CompareOrdinal(latest, current) > 0;
        }
    }
}
