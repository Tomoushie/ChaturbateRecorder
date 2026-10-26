using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Config;
using SentinelGuard;

namespace ChaturbateRecorderApp.Services
{
    public static class DiagnosticReport
    {
        public static string Statique(PremiumBridge? premium)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Application : v{typeof(DiagnosticReport).Assembly.GetName().Version?.ToString(3)}");
            sb.AppendLine($".NET : {RuntimeInformation.FrameworkDescription}");
            sb.AppendLine($"Système : {Environment.OSVersion} ({(Environment.Is64BitProcess ? "64" : "32")} bits)");
            sb.AppendLine();

            sb.AppendLine("Intégrité des binaires (empreinte SHA-256)");
            sb.AppendLine($"yt-dlp.exe : {DecrireEmpreinte("yt-dlp", AppConfig.YtDlpPath, AppConfig.YtDlpExpectedSha256)}");
            sb.AppendLine($"ffmpeg.exe : {DecrireEmpreinte("ffmpeg", AppConfig.FFmpegPath, AppConfig.FfmpegExpectedSha256)}");

            sb.AppendLine("Composant premium (Stream Recorder Pro)");
            sb.AppendLine(DecrirePremium(premium));
            // 120.0 — à fournir avec une commande (itch.io, virement) : c'est
            // sur cet identifiant que la licence sera liée. Dans le rapport
            // Diagnostic pour qui l'a manqué dans la fenêtre Premium.
            sb.AppendLine($"Identifiant d'installation : {MachineId.Obtenir()}");

            sb.AppendLine("Dossier d'exécution");
            sb.AppendLine($"Emplacement autorisé : {(WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir) ? "oui" : "non")}");

            sb.AppendLine("ACL (droits d'écriture élargis détectés ?)");
            sb.AppendLine(DecrireAcl("AppDir", AppConfig.AppDir));
            sb.AppendLine(DecrireAcl("CaptureDir", AppConfig.CaptureDir));
            sb.AppendLine(DecrireAcl("LogDir", AppConfig.LogDir));

            sb.AppendLine($"Proxy configuré : {(string.IsNullOrWhiteSpace(AppConfig.ProxyUrl) ? "aucun" : AppConfig.ProxyUrl)}");

            return sb.ToString();
        }

        public static async Task<string> CompletAsync(PremiumBridge? premium)
        {
            var statique = Statique(premium);
            var sb = new StringBuilder(statique);

            sb.AppendLine("Binaires (versions)");
            sb.AppendLine($"yt-dlp.exe : {await VersionBinaireAsync(AppConfig.YtDlpPath, "--version")}");
            sb.AppendLine($"ffmpeg.exe : {await VersionBinaireAsync(AppConfig.FFmpegPath, "-version")}");

            sb.AppendLine("Réseau");
            sb.AppendLine($"chaturbate.com : {(await JoignableAsync("https://chaturbate.com") ? "joignable" : "injoignable")}");
            sb.AppendLine($"https://api.github.com : {(await JoignableAsync("https://api.github.com") ? "joignable" : "injoignable")}");

            return sb.ToString();
        }

        private static string DecrireEmpreinte(string cle, string chemin, string attendu)
        {
            if (!File.Exists(chemin))
                return "absent";

            try
            {
                using (var stream = File.OpenRead(chemin))
                {
                    var hash = Convert.ToHexString(SHA256.HashData(stream));

                    // L'empreinte figée dans AppConfig n'est plus qu'un moyen
                    // d'ÉPINGLER une build précise, et elle est vide par
                    // défaut depuis 2.1.1 (voir le commentaire là-bas) : la
                    // référence normale est celle que l'installateur a
                    // inscrite dans trusted-binaries.json après avoir vérifié
                    // le téléchargement contre la somme publiée par l'auteur
                    // du binaire. Une constante compilée ne pouvait pas suivre
                    // ffmpeg ni yt-dlp, qui changent de build sous la même
                    // URL, et affichait « INATTENDU » — le mot qui signale une
                    // altération — pour des fichiers conformes à leur source.
                    if (string.IsNullOrEmpty(attendu))
                        attendu = TrustedBinaryStore.GetTrustedHash(cle) ?? "";

                    if (string.IsNullOrEmpty(attendu))
                        return $"non vérifié ({hash.Substring(0, 8)}...)";
                    else if (string.Equals(hash, attendu, StringComparison.OrdinalIgnoreCase))
                        return $"conforme ({hash.Substring(0, 8)}...)";
                    else
                        return $"INATTENDU ({hash.Substring(0, 8)}... au lieu de {attendu.Substring(0, 8)}...)";
                }
            }
            catch (Exception ex)
            {
                return $"illisible : {ex.Message}";
            }
        }

        private static string DecrireAcl(string libelle, string chemin)
        {
            if (!Directory.Exists(chemin))
                return $"{libelle} : dossier absent";

            try
            {
                var details = string.Empty;
                if (AclValidator.TryFindBroadWriteAccess(chemin, out details))
                    return $"{libelle} : ÉLARGIS — {details}";
                else
                    return $"{libelle} : normaux";
            }
            catch (Exception ex)
            {
                return $"{libelle} : non vérifiable ({ex.Message})";
            }
        }

        private static string DecrirePremium(PremiumBridge? premium)
        {
            if (premium == null)
                return "État indisponible.";
            else if (!premium.IsLoaded)
                return $"Non installé (normal) — {(premium.LicenceProblem.Length > 0 ? premium.LicenceProblem : "aucun StreamRecorderPro.dll à côté de l'application")}." ;
            else if (premium.IsLicensed)
                return $"Actif, v{premium.Version} — licence au nom de {premium.LicensedTo}. Usage : {premium.UsageSummary}.";
            else
                // Depuis le 21-09 le module est livré à TOUS : présent sans
                // licence est l'état NORMAL d'un utilisateur gratuit, pas une
                // panne — le dire sans majuscules d'alarme.
                return $"Inclus (v{premium.Version}), non activé — {premium.LicenceProblem}.";
        }

        private static async Task<string> VersionBinaireAsync(string chemin, string arguments)
        {
            if (!File.Exists(chemin))
                return "absent";

            using (var process = new Process())
            {
                process.StartInfo.FileName = chemin;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        return line.Length > 120 ? line.Substring(0, 120) : line;
                    }
                }

                return "indéterminée";
            }
        }

        private static async Task<bool> JoignableAsync(string url)
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
            {
                try
                {
                    // 2.1.1 — L'EN-TÊTE D'IDENTIFICATION N'EST PAS DÉCORATIVE :
                    // l'API de GitHub refuse par 403 toute requête qui n'en
                    // porte pas, et HttpClient n'en envoie aucune par défaut.
                    // Le Diagnostic annonçait donc « api.github.com :
                    // injoignable » sur une machine parfaitement connectée
                    // (constaté chez le mainteneur le 21-09), alors que la
                    // vérification de mise à jour, elle, fonctionne — elle
                    // s'identifie (UpdateChecker). Mesuré : HEAD sans en-tête
                    // 403, avec en-tête 200.
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("ChaturbateRecorder-Diagnostic");
                    var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}