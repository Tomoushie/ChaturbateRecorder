using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Rend au fichier de capture son vrai nom, une fois yt-dlp terminé.
    ///
    /// **UN DIRECT NE SE TERMINE JAMAIS SEUL** : c'est l'utilisateur ou
    /// l'application qui coupe, donc yt-dlp est TUÉ et ne renomme pas son
    /// temporaire. Sans ce service, chaque capture restait un
    /// `<nom>.mp4.part` — lisible, mais introuvable : l'historique ne liste
    /// que `.mp4`, `.mkv` et `.mov`. L'application enregistrait donc
    /// correctement des vidéos que personne ne voyait jamais.
    ///
    /// Le portage WPF avait perdu cette étape, et rien ne le signalait : la
    /// capture « marchait », le fichier grossissait, l'écran disait vrai.
    /// Constaté au premier essai sur un vrai direct — trois `.part` dans le
    /// dossier de captures, zéro entrée dans l'historique.
    ///
    /// **LA MINIATURE EST DÉSORMAIS REPRISE ICI** (24-08) : `HistoryService`
    /// et la vue Historique savaient déjà la lire (`CheminVignette`), rien ne
    /// l'avait jamais écrite côté WPF. Portée depuis le WinForms — mêmes
    /// arguments ffmpeg, même repli sur le début du fichier — mais volontairement
    /// PAS sur le chemin critique : elle part une fois le fichier renommé, sans
    /// jamais faire échouer la finalisation elle-même.
    /// </summary>
    public static class CaptureFinalizer
    {
        /// <summary>Essais de renommage, et attente entre deux.</summary>
        internal const int Essais = 12;
        internal const int AttenteMs = 250;

        /// <summary>
        /// Renomme `<nomBase>.<extension>.part` en `<nomBase>.<extension>`.
        ///
        /// **RÉESSAIS, parce que le fichier peut encore être verrouillé** : le
        /// processus vient d'être tué et Windows relâche son handle avec un
        /// léger retard. Un essai unique échouait par intermittence côté
        /// WinForms — le genre de défaut qu'on croit corrigé jusqu'à son
        /// retour.
        ///
        /// **UN `.part` VIDE N'EST PAS RENOMMÉ** : cela déposerait un fichier
        /// de 0 octet dans l'historique, à côté des vrais enregistrements. Un
        /// démarrage qui échoue immédiatement en produit un.
        /// </summary>
        /// <returns>Vrai si un fichier a bien été renommé.</returns>
        public static async Task<bool> FinaliserAsync(string dossier, string? nomBase, string extension)
        {
            if (string.IsNullOrWhiteSpace(nomBase)) return false;

            var final = Path.Combine(dossier, $"{nomBase}.{extension}");
            var part = final + ".part";

            // Deja fait : yt-dlp renomme lui-meme quand le flux se termine
            // normalement, ce qui arrive pour une VOD meme si jamais pour un
            // direct. Rien a faire, et surtout rien a ecraser.
            if (File.Exists(final)) return false;
            if (!File.Exists(part)) return false;

            for (var essai = 0; essai < Essais; essai++)
            {
                try
                {
                    if (new FileInfo(part).Length == 0)
                    {
                        Logger.Log($"Fichier temporaire vide, non renomme : {part}", LogLevel.WARN);
                        return false;
                    }

                    File.Move(part, final);
                    Logger.Log($"Enregistrement finalise : {Path.GetFileName(final)}");

                    // Jamais sur le chemin critique : une miniature ratee ne
                    // doit pas faire croire que la finalisation a echoue.
                    try
                    {
                        await GenererVignetteAsync(final).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Generation de la miniature impossible pour {final} : {ex.Message}", LogLevel.WARN);
                    }

                    return true;
                }
                catch (IOException) when (essai < Essais - 1)
                {
                    await Task.Delay(AttenteMs).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Droits refuses, disque plein, chemin devenu invalide : la
                    // capture EXISTE toujours sous son nom temporaire, et le
                    // journal doit dire ou la chercher.
                    Logger.Log($"Finalisation impossible pour {part} : {ex.Message}", LogLevel.ERROR);
                    return false;
                }
            }

            Logger.Log($"Fichier encore verrouille apres {Essais} essais : {part}", LogLevel.WARN);
            return false;
        }

        /// <summary>
        /// Extrait une image de la capture, posee en .jpg a cote d'elle —
        /// c'est exactement le chemin que <c>HistoryService.Vignette</c> lit
        /// deja. Respecte le Safe Mode (composant Ffmpeg) : un ffmpeg absent
        /// ou desactive ne doit ni lever ni ralentir la finalisation.
        /// </summary>
        private static async Task GenererVignetteAsync(string videoPath)
        {
            if (!SafeMode.IsEnabled(SafeComponent.Ffmpeg) || !File.Exists(AppConfig.FFmpegPath))
            {
                return;
            }

            var miniature = Path.Combine(Path.GetDirectoryName(videoPath)!, Path.GetFileNameWithoutExtension(videoPath) + ".jpg");
            if (File.Exists(miniature))
            {
                return;
            }

            var ok = await ExtraireImageAsync(videoPath, miniature, AppConfig.ThumbnailOffsetSeconds).ConfigureAwait(false);

            // Repli sur le debut du fichier : au-dela de la fin REELLE d'un
            // enregistrement plus court que le decalage demande, ffmpeg
            // n'ecrit rien avec -ss place avant -i (mesure cote WinForms).
            if (!ok && AppConfig.ThumbnailOffsetSeconds > 0)
            {
                ok = await ExtraireImageAsync(videoPath, miniature, 0).ConfigureAwait(false);
            }

            Logger.Log(ok
                ? $"Miniature creee : {miniature}"
                : $"Erreur creation miniature pour {videoPath}", ok ? LogLevel.INFO : LogLevel.WARN);
        }

        /// <summary>
        /// Un appel a ffmpeg, une image. Le verdict est l'EXISTENCE du fichier
        /// et non le code de sortie : c'est le fichier que l'historique ira
        /// lire. Pas de redirection stdout/stderr : rien ne les lit, et un
        /// ffmpeg plus bavard que prevu remplirait le tampon du pipe sans
        /// jamais debloquer le processus avant le delai.
        /// </summary>
        private static async Task<bool> ExtraireImageAsync(string video, string miniature, int decalageSecondes)
        {
            var psi = new ProcessStartInfo
            {
                FileName = AppConfig.FFmpegPath,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var a in new[]
            {
                "-ss", decalageSecondes.ToString(),
                "-i", video,
                "-frames:v", "1",
                "-q:v", "2",
                miniature,
                "-y",
                "-loglevel", "error"
            })
            {
                psi.ArgumentList.Add(a);
            }

            using var p = Process.Start(psi);
            if (p == null) return false;

            using var delai = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                await p.WaitForExitAsync(delai.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Un ffmpeg fige garderait un handle de lecture sur la
                // capture. L'ancien defaut WinForms se contentait d'abandonner
                // l'attente en le laissant tourner.
                try { p.Kill(entireProcessTree: true); } catch { /* deja parti */ }
                return false;
            }

            return File.Exists(miniature);
        }
    }
}
