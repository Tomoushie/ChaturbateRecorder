using System;
using System.IO;
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
        /// <summary>
        /// Empreinte SHA-256 publiee par GitHub pour ce fichier, en majuscules.
        /// Vide si l'API ne la fournit pas (releases anterieures a son
        /// introduction) : dans ce cas l'installation se poursuit sans
        /// verification, comme avant, plutot que de rendre les anciennes
        /// versions impossibles a mettre a jour.
        /// </summary>
        public string Sha256 { get; init; } = "";
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
            string? digest = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                // Chaque release attache deux ZIP (standard framework-dependent /
                // portable self-contained). Il faut choisir celui qui correspond à
                // l'exécutable en cours, sinon une install portable pourrait être
                // remplacée par le build standard (sans runtime .NET embarqué), et
                // inversement. Détection : le build portable (single-file) n'a pas
                // de ChaturbateRecorder.dll séparé à côté de l'exe.
                var isPortable = !File.Exists(Path.Combine(AppContext.BaseDirectory, "ChaturbateRecorder.dll"));
                string? fallbackUrl = null;
                string? fallbackDigest = null;

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;

                    var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                    var assetDigest = asset.TryGetProperty("digest", out var dg) ? dg.GetString() : null;
                    if (fallbackUrl == null)
                    {
                        fallbackUrl = url;
                        fallbackDigest = assetDigest;
                    }

                    var isPortableAsset = name.Contains("portable", StringComparison.OrdinalIgnoreCase);
                    if (isPortableAsset == isPortable)
                    {
                        downloadUrl = url;
                        digest = assetDigest;
                        break;
                    }
                }

                // Release plus ancienne ne suivant pas la convention de nommage
                // (un seul ZIP, ou noms différents) : on retombe sur le premier
                // trouvé plutôt que d'échouer silencieusement.
                if (downloadUrl == null)
                {
                    downloadUrl = fallbackUrl;
                    digest = fallbackDigest;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl)) return null;

            return new UpdateInfo
            {
                Version = latestVersion,
                DownloadUrl = downloadUrl,
                ReleaseNotesUrl = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "",
                Sha256 = ParseDigest(digest),
            };
        }

        /// <summary>
        /// Extrait l'empreinte d'un champ « digest » de l'API GitHub, de la
        /// forme « sha256:abc... ». Retourne une chaine vide si le champ est
        /// absent ou d'un algorithme inconnu — l'appelant traite ce cas comme
        /// « pas de verification possible », pas comme une erreur : les
        /// releases publiees avant l'introduction de ce champ doivent rester
        /// installables.
        /// </summary>
        internal static string ParseDigest(string? digest)
        {
            if (string.IsNullOrWhiteSpace(digest)) return "";

            const string prefix = "sha256:";
            if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return "";

            return digest[prefix.Length..].Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Vérification automatique (79.0) : une version déjà signalée ne doit
        /// pas redéclencher une notification à chaque passage horaire. On
        /// compare à la dernière version notifiée plutôt que de mémoriser un
        /// simple booléen "déjà prévenu" — sinon une release publiée après
        /// coup passerait sous silence jusqu'au redémarrage de l'appli.
        /// </summary>
        internal static bool ShouldNotify(string latestVersion, string? lastNotifiedVersion)
        {
            if (string.IsNullOrWhiteSpace(lastNotifiedVersion)) return true;
            return IsNewer(latestVersion, lastNotifiedVersion);
        }

        internal static bool IsNewer(string latest, string current)
        {
            if (Version.TryParse(latest, out var lv) && Version.TryParse(current, out var cv))
                return lv > cv;
            return string.CompareOrdinal(latest, current) > 0;
        }
    }
}
