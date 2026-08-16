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
        /// <summary>
        /// La source n'existe pas (40.0). Distinct d'Offline : une faute de
        /// frappe ne sera JAMAIS suivie d'une diffusion, l'attendre
        /// indéfiniment n'a aucun sens. Seules certaines plateformes le disent
        /// — Chaturbate rend les deux cas indiscernables, Twitch et TikTok non.
        /// </summary>
        NotFound,
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
        /// Phrases par lesquelles les extracteurs annoncent qu'il n'y a rien à
        /// enregistrer. Toutes relevées sur le vrai yt-dlp, jamais supposées :
        /// "Room is currently offline" (Chaturbate, 2026-08-05),
        /// "The channel is not currently live" (Twitch et TikTok, 2026-08-09).
        /// </summary>
        private static readonly string[] OfflineMarkers =
        {
            "is currently offline",
            "is not currently live",
        };

        /// <summary>
        /// Phrase par laquelle Twitch annonce un compte inexistant. Chaturbate
        /// n'a pas d'équivalent : chez lui, une faute de frappe est
        /// indiscernable d'une absence.
        /// </summary>
        private const string NotFoundMarker = "does not exist";

        /// <summary>
        /// Valeurs de <c>live_status</c> qui signifient « ce n'est pas une
        /// diffusion en cours ». Le reste — y compris le "NA" que yt-dlp
        /// imprime quand l'extracteur ne renseigne pas le champ — laisse
        /// décider le code de sortie, ce qui préserve le comportement
        /// historique sur Chaturbate.
        /// </summary>
        private static readonly string[] NotLiveStatuses =
        {
            "not_live", "was_live", "post_live", "is_upcoming",
        };

        /// <summary>
        /// Traduit le résultat brut de yt-dlp en état de source. Séparée de
        /// l'exécution du processus pour rester testable sans réseau : c'est la
        /// seule partie qui puisse être fausse sans se voir.
        ///
        /// **Règle de prudence** : tout ce qui n'est pas explicitement l'un ou
        /// l'autre devient Unknown, jamais Online. Un réseau coupé ne doit pas
        /// déclencher un enregistrement, et un salon banni ne doit pas faire
        /// croire à une absence temporaire qu'on attendrait indéfiniment.
        ///
        /// **Le code de sortie ne suffit pas depuis 40.0** : mesuré le
        /// 2026-08-09, YouTube rend 0 sur une vidéo ORDINAIRE. S'en tenir au
        /// code de retour aurait fait démarrer un « enregistrement » de VOD dès
        /// qu'une URL YouTube entrait dans la surveillance.
        /// </summary>
        internal static RoomStatus Classify(int exitCode, string standardOutput, string standardError)
        {
            var stdout = standardOutput ?? "";
            var stderr = standardError ?? "";

            if (exitCode == 0)
            {
                foreach (var status in NotLiveStatuses)
                    if (stdout.Contains(status, StringComparison.OrdinalIgnoreCase))
                        return RoomStatus.Offline;

                return RoomStatus.Online;
            }

            if (stderr.Contains(NotFoundMarker, StringComparison.OrdinalIgnoreCase))
                return RoomStatus.NotFound;

            foreach (var marker in OfflineMarkers)
                if (stderr.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    return RoomStatus.Offline;

            return RoomStatus.Unknown;
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
            // Demande explicitement l'état de diffusion (40.0) : sur YouTube,
            // le code de sortie vaut 0 aussi bien pour un live que pour une
            // vidéo déjà enregistrée. Sans ce champ, les deux se confondent.
            psi.ArgumentList.Add("--print");
            psi.ArgumentList.Add("live_status=%(live_status)s");
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
                var stdout = await stdoutTask;
                return Classify(process.ExitCode, stdout, stderr);
            }
            catch (Exception ex)
            {
                Logger.Log($"Surveillance : contrôle de {roomUrl} impossible : {ex.Message}", LogLevel.WARN);
                return RoomStatus.Unknown;
            }
        }
    }
}
