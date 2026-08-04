using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChaturbateRecorderApp.Services
{
    public enum RoomStatus
    {
        /// <summary>Le salon diffuse : un enregistrement peut démarrer.</summary>
        Online,
        /// <summary>Le salon ne diffuse pas — ou n'existe pas, les deux étant indiscernables.</summary>
        Offline,
        /// <summary>Ni l'un ni l'autre : réseau coupé, salon banni, yt-dlp absent. NE JAMAIS traiter comme Online.</summary>
        Unknown,
    }

    /// <summary>
    /// Détermine si un salon diffuse, en interrogeant yt-dlp en mode simulation
    /// (88.0 / 4.3 « Surveillance »).
    ///
    /// **Pourquoi yt-dlp et pas une requête HTTP directe** : mesuré le
    /// 2026-08-05, yt-dlp atteint l'API JSON de Chaturbate sans être bloqué,
    /// alors qu'un HttpClient se fait refuser en 403 par la protection
    /// anti-robots (voir l'épisode 92.0 dans CLAUDE.md). yt-dlp est déjà
    /// embarqué et déjà utilisé pour enregistrer : aucune dépendance nouvelle,
    /// et le même composant décide « en ligne » et sait enregistrer.
    ///
    /// `--simulate` ne télécharge rien : yt-dlp résout la source puis s'arrête.
    /// </summary>
    public static class RoomStatusChecker
    {
        /// <summary>
        /// Message exact de l'extracteur Chaturbate quand la diffusion est
        /// arrêtée. Observé tel quel le 2026-08-05 :
        /// "ERROR: [Chaturbate] &lt;room&gt;: Room is currently offline".
        /// </summary>
        private const string OfflineMarker = "is currently offline";

        /// <summary>
        /// Traduit le résultat brut de yt-dlp en état de salon. Séparée de
        /// l'exécution du processus pour rester testable sans réseau : c'est la
        /// seule partie qui puisse être fausse sans se voir.
        ///
        /// **Règle de prudence** : tout ce qui n'est pas explicitement l'un ou
        /// l'autre devient Unknown, jamais Online. Un réseau coupé ne doit pas
        /// déclencher un enregistrement, et un salon banni ne doit pas faire
        /// croire à une absence temporaire qu'on attendrait indéfiniment.
        /// </summary>
        internal static RoomStatus Classify(int exitCode, string standardError)
        {
            if (exitCode == 0) return RoomStatus.Online;

            return (standardError ?? "").Contains(OfflineMarker, StringComparison.OrdinalIgnoreCase)
                ? RoomStatus.Offline
                : RoomStatus.Unknown;
        }

        public static async Task<RoomStatus> CheckAsync(string ytDlpPath, string roomUrl,
            string? cookiesFilePath = null, string? proxyUrl = null,
            int timeoutSeconds = 45, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(ytDlpPath))
            {
                Logger.Log($"Surveillance : yt-dlp introuvable ({ytDlpPath}).", LogLevel.ERROR);
                return RoomStatus.Unknown;
            }

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
            };
            psi.ArgumentList.Add("--simulate");
            psi.ArgumentList.Add("--no-warnings");
            psi.ArgumentList.Add("--no-playlist");
            // Une seule tentative : c'est la boucle de surveillance qui réessaie,
            // à son propre rythme. Les reprises internes de yt-dlp allongeraient
            // le contrôle sans rien apporter.
            psi.ArgumentList.Add("--retries");
            psi.ArgumentList.Add("1");
            if (!string.IsNullOrWhiteSpace(cookiesFilePath) && File.Exists(cookiesFilePath))
            {
                psi.ArgumentList.Add("--cookies");
                psi.ArgumentList.Add(cookiesFilePath);
            }
            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                psi.ArgumentList.Add("--proxy");
                psi.ArgumentList.Add(proxyUrl);
            }
            psi.ArgumentList.Add(roomUrl);

            try
            {
                using var process = new Process { StartInfo = psi };
                process.Start();

                var stderrTask = process.StandardError.ReadToEndAsync();
                // Le flux de sortie doit être consommé même s'il ne sert à rien :
                // un tube saturé bloquerait yt-dlp jusqu'au délai d'attente.
                var stdoutTask = process.StandardOutput.ReadToEndAsync();

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* déjà mort */ }
                    Logger.Log($"Surveillance : contrôle de {roomUrl} interrompu (délai dépassé).", LogLevel.WARN);
                    return RoomStatus.Unknown;
                }

                var stderr = await stderrTask;
                await stdoutTask;
                return Classify(process.ExitCode, stderr);
            }
            catch (Exception ex)
            {
                Logger.Log($"Surveillance : contrôle de {roomUrl} impossible : {ex.Message}", LogLevel.WARN);
                return RoomStatus.Unknown;
            }
        }
    }
}
