using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    public sealed class RecordingCoordinator
    {
        private readonly Dictionary<string, RecordingJob> _enCours = new(StringComparer.OrdinalIgnoreCase);

        public event Action<string, DownloadState>? EtatChange;
        public event Action<string, double>? Progression;
        public event Action<string, string>? LigneDeJournal;

        public bool EnCours(string url)
        {
            return _enCours.ContainsKey(url);
        }

        public void Demarrer(string url, string? formatSelector = null, string containerExt = "mp4")
        {
            if (_enCours.ContainsKey(url))
                return;

            var job = new RecordingJob
            {
                RoomName = Platforms.DisplayName(url),
                SourceUrl = url,
                CaptureDir = AppConfig.CaptureDir,
                CodecChoice = "copy",
                ContainerExt = containerExt
            };

            // Les trois abonnements CAPTURENT l'url : les evenements du moteur
            // ne la portent pas — il ne connait qu'un processus, pas le salon
            // qui l'a demande. C'est la fermeture qui fait le lien, et c'est
            // aussi ce qui permet a plusieurs captures de tourner de front.
            job.Engine.OnStateChanged += etat => SurFilDInterface(() =>
            {
                // Retirer AVANT de prevenir : un abonne qui relance
                // immediatement doit trouver la place libre.
                if (etat is DownloadState.Completed or DownloadState.Failed or DownloadState.Stopped)
                    _enCours.Remove(url);

                EtatChange?.Invoke(url, etat);
            });

            job.Engine.OnProgress += pourcent =>
                SurFilDInterface(() => Progression?.Invoke(url, pourcent));

            job.Engine.OnLogLine += ligne =>
                SurFilDInterface(() => LigneDeJournal?.Invoke(url, ligne));

            var horodatage = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            job.OutputBaseName = $"{job.RoomName}-{horodatage}";
            var journal = Path.Combine(AppConfig.LogDir, $"{job.OutputBaseName}.log");
            var sortie = Path.Combine(job.CaptureDir, $"{job.OutputBaseName}.%(ext)s");

            // INSCRIT AVANT DE DEMARRER, et l'ordre est le sujet : le moteur
            // peut rendre son premier etat — y compris Failed — pendant l'appel
            // a Start. Inscrire apres laisserait ce retrait porter sur une
            // entree absente, puis l'ajout deposerait un FANTOME que plus rien
            // n'enleverait : EnCours resterait vrai pour toujours, et le bouton
            // afficherait « Arreter » sur une capture terminee.
            _enCours[url] = job;

            try
            {
                job.Engine.Start(
                    AppConfig.YtDlpPath,
                    AppConfig.FFmpegPath,
                    url,
                    sortie,
                    journal,
                    formatSelector,
                    containerExt,
                    SafeMode.IsEnabled(SafeComponent.Cookies) ? AppConfig.CookiesFilePath : "",
                    SafeMode.IsEnabled(SafeComponent.Proxy) ? AppConfig.ProxyUrl : "",
                    AppConfig.YtDlpWatchdogTimeoutSeconds,
                    AppConfig.LogMaxFileSizeBytes
                );
            }
            catch (Exception ex)
            {
                _enCours.Remove(url);
                Logger.Log($"Demarrage impossible pour {url} : {ex.Message}", LogLevel.ERROR);
                throw;
            }
        }

        public void Arreter(string url)
        {
            if (_enCours.TryGetValue(url, out var job))
            {
                job.Engine.Stop();
            }
        }

        private static void SurFilDInterface(Action action)
        {
            if (Application.Current?.Dispatcher is { } d && !d.CheckAccess())
                d.Invoke(action);
            else
                action();
        }

    }
}