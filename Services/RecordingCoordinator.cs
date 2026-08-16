using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
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

        /// <summary>Reconnexion programmée en attente, par URL.</summary>
        private readonly Dictionary<string, DispatcherTimer> _reconnexions = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Minuteur d'arrêt programmé, par URL.</summary>
        private readonly Dictionary<string, DispatcherTimer> _minuteurs = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Paramètres de la capture, gardés pour pouvoir la REJOUER à
        /// l'identique lors d'une reconnexion — sauf le nom de fichier, qui
        /// lui doit changer.
        /// </summary>
        private readonly Dictionary<string, (string? Format, string Conteneur)> _parametres =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Une reconnexion est programmée : (url, secondes d'attente, tentative, total).</summary>
        public event Action<string, int, int, int>? ReconnexionProgrammee;

        /// <summary>Temps restant avant l'arrêt programmé : (url, texte déjà formaté).</summary>
        public event Action<string, string>? Decompte;

        public void Demarrer(string url, string? formatSelector = null, string containerExt = "mp4",
                             bool reconnexionAuto = false, int minutesMinuteur = 0)
        {
            if (_enCours.ContainsKey(url))
                return;

            var job = new RecordingJob
            {
                RoomName = Platforms.DisplayName(url),
                SourceUrl = url,
                CaptureDir = AppConfig.CaptureDir,
                CodecChoice = "copy",
                ContainerExt = containerExt,
                AutoReconnectEnabled = reconnexionAuto,
                TimerMinutes = minutesMinuteur,
            };

            _parametres[url] = (formatSelector, containerExt);

            // Les trois abonnements CAPTURENT l'url : les evenements du moteur
            // ne la portent pas — il ne connait qu'un processus, pas le salon
            // qui l'a demande. C'est la fermeture qui fait le lien, et c'est
            // aussi ce qui permet a plusieurs captures de tourner de front.
            //
            // Poses UNE SEULE FOIS, sur le moteur du job : une reconnexion
            // rejoue `Start` sur le MEME DownloadEngine, donc reabonner a
            // chaque tentative empilerait les gestionnaires et ferait lever
            // chaque evenement autant de fois qu'il y a eu de reconnexions.
            job.Engine.OnStateChanged += etat => SurFilDInterface(() => SurEtat(url, job, etat));

            job.Engine.OnProgress += pourcent =>
                SurFilDInterface(() => Progression?.Invoke(url, pourcent));

            job.Engine.OnLogLine += ligne =>
                SurFilDInterface(() => LigneDeJournal?.Invoke(url, ligne));

            // INSCRIT AVANT DE DEMARRER, et l'ordre est le sujet : le moteur
            // peut rendre son premier etat — y compris Failed — pendant l'appel
            // a Start. Inscrire apres laisserait ce retrait porter sur une
            // entree absente, puis l'ajout deposerait un FANTOME que plus rien
            // n'enleverait : EnCours resterait vrai pour toujours, et le bouton
            // afficherait « Arreter » sur une capture terminee.
            _enCours[url] = job;

            ArmerLeMinuteur(url, job);

            try
            {
                DemarrerLeMoteur(url, job);
            }
            catch (Exception ex)
            {
                _enCours.Remove(url);
                AnnulerLeMinuteur(url);
                Logger.Log($"Demarrage impossible pour {url} : {ex.Message}", LogLevel.ERROR);
                throw;
            }
        }

        /// <summary>
        /// Lance yt-dlp pour ce job. Extrait de <see cref="Demarrer"/> parce
        /// qu'une reconnexion doit le REJOUER — c'est le pendant du
        /// « StartEngine » que la version WinForms capturait en fermeture.
        ///
        /// **Le nom de sortie est régénéré à chaque appel**, et c'est
        /// volontaire : un horodatage frais donne un fichier ET un journal
        /// distincts par tentative. Réutiliser le précédent ferait écraser la
        /// capture déjà obtenue par celle de la reconnexion — c'est-à-dire
        /// perdre ce qu'on venait justement de sauver.
        /// </summary>
        private void DemarrerLeMoteur(string url, RecordingJob job)
        {
            var horodatage = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            job.OutputBaseName = $"{job.RoomName}-{horodatage}";
            var journal = Path.Combine(AppConfig.LogDir, $"{job.OutputBaseName}.log");
            var sortie = Path.Combine(job.CaptureDir, $"{job.OutputBaseName}.%(ext)s");
            var (format, conteneur) = _parametres.TryGetValue(url, out var p) ? p : (null, "mp4");

            job.Engine.Start(
                AppConfig.YtDlpPath,
                AppConfig.FFmpegPath,
                url,
                sortie,
                journal,
                format,
                conteneur,
                // Un composant desactive n'est pas transmis a yt-dlp. Passer un
                // cookies.txt invalide faisait echouer TOUTES les captures :
                // mieux vaut enregistrer sans authentification que rien.
                SafeMode.IsEnabled(SafeComponent.Cookies) ? AppConfig.CookiesFilePath : "",
                SafeMode.IsEnabled(SafeComponent.Proxy) ? AppConfig.ProxyUrl : "",
                AppConfig.YtDlpWatchdogTimeoutSeconds,
                AppConfig.LogMaxFileSizeBytes
            );
        }

        /// <summary>
        /// Décide, sur un état rendu par le moteur, si la capture est finie ou
        /// si elle mérite une reconnexion.
        /// </summary>
        private void SurEtat(string url, RecordingJob job, DownloadState etat)
        {
            if (etat is DownloadState.Completed or DownloadState.Failed or DownloadState.Stopped)
            {
                // **SEUL `Failed` déclenche une reconnexion.** `Stopped` est un
                // arrêt DEMANDÉ et `Completed` une fin normale : rejouer l'un
                // ou l'autre relancerait une capture que personne n'a demandée.
                // `Arreter` coupe d'ailleurs le drapeau avant d'arrêter le
                // moteur, pour que l'arrêt manuel ne puisse pas passer ici par
                // un chemin détourné.
                if (etat == DownloadState.Failed
                    && job.AutoReconnectEnabled
                    && job.ReconnectAttempt < AppConfig.AutoReconnectMaxAttempts)
                {
                    ProgrammerReconnexion(url, job);
                    // L'entrée RESTE dans la table : du point de vue de
                    // l'utilisateur ce salon est toujours pris en charge, et
                    // libérer la place laisserait démarrer une seconde capture
                    // pendant que la première attend sa nouvelle tentative.
                    return;
                }

                _enCours.Remove(url);
                _parametres.Remove(url);
                AnnulerLeMinuteur(url);
            }

            EtatChange?.Invoke(url, etat);
        }

        /// <summary>
        /// Programme une nouvelle tentative après le délai configuré.
        /// </summary>
        private void ProgrammerReconnexion(string url, RecordingJob job)
        {
            job.ReconnectAttempt++;
            var tentative = job.ReconnectAttempt;
            var delai = AppConfig.AutoReconnectDelaySeconds;

            Logger.Log($"Reconnexion automatique de {url} dans {delai}s " +
                       $"(tentative {tentative}/{AppConfig.AutoReconnectMaxAttempts}).");

            AnnulerReconnexion(url);
            var minuteur = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delai) };
            minuteur.Tick += (s, e) =>
            {
                minuteur.Stop();
                _reconnexions.Remove(url);

                // Le salon a pu être arrêté ou retiré pendant l'attente : sans
                // ce contrôle, la tentative repartirait sur une capture que
                // l'utilisateur croit terminée.
                if (!_enCours.ContainsKey(url) || !job.AutoReconnectEnabled) return;

                try
                {
                    DemarrerLeMoteur(url, job);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Reconnexion impossible pour {url} : {ex.Message}", LogLevel.ERROR);
                    _enCours.Remove(url);
                    AnnulerLeMinuteur(url);
                    EtatChange?.Invoke(url, DownloadState.Failed);
                }
            };
            _reconnexions[url] = minuteur;
            minuteur.Start();

            ReconnexionProgrammee?.Invoke(url, delai, tentative, AppConfig.AutoReconnectMaxAttempts);
        }

        private void AnnulerReconnexion(string url)
        {
            if (!_reconnexions.TryGetValue(url, out var m)) return;
            m.Stop();
            _reconnexions.Remove(url);
        }

        /// <summary>
        /// Arme l'arrêt programmé, s'il y en a un.
        ///
        /// **`??=` et non `=`** : le décompte part du PREMIER démarrage et
        /// n'est pas remis à zéro par une reconnexion. « Arrêter après 2 h »
        /// désigne deux heures de temps écoulé, pas deux heures par tentative
        /// — sinon un salon instable enregistrerait indéfiniment.
        /// </summary>
        private void ArmerLeMinuteur(string url, RecordingJob job)
        {
            if (job.TimerMinutes <= 0) return;

            job.StopAtUtc ??= DateTime.UtcNow.AddMinutes(job.TimerMinutes);

            var minuteur = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            minuteur.Tick += (s, e) =>
            {
                if (job.StopAtUtc is not { } echeance) return;

                var restant = echeance - DateTime.UtcNow;
                if (restant > TimeSpan.Zero)
                {
                    Decompte?.Invoke(url, RecordingTimer.FormatRemaining(restant));
                    return;
                }

                // Le minuteur est coupé AVANT d'arrêter le moteur : sans cela
                // chaque tick suivant redemanderait l'arrêt.
                AnnulerLeMinuteur(url);
                Logger.Log($"Duree maximale atteinte pour {url} ({job.TimerMinutes} min) : arret.");
                Arreter(url);
            };
            _minuteurs[url] = minuteur;
            minuteur.Start();
        }

        private void AnnulerLeMinuteur(string url)
        {
            if (!_minuteurs.TryGetValue(url, out var m)) return;
            m.Stop();
            _minuteurs.Remove(url);
        }

        public void Arreter(string url)
        {
            if (!_enCours.TryGetValue(url, out var job)) return;

            // **COUPER LA RECONNEXION AVANT D'ARRÊTER LE MOTEUR.** Tuer yt-dlp
            // fait remonter un état d'échec ; si le drapeau était encore armé,
            // un arrêt demandé par l'utilisateur relancerait aussitôt une
            // capture. La version WinForms fait exactement cela, au même
            // endroit et pour la même raison.
            job.AutoReconnectEnabled = false;

            // **ARRÊTER PENDANT UNE RECONNEXION EN ATTENTE NE TUE AUCUN
            // PROCESSUS**, donc le moteur ne rendra AUCUN état, donc rien ne
            // viendrait retirer l'entrée ni repeindre la carte : elle resterait
            // affichée « reconnexion » avec un bouton « Arrêter » sans effet,
            // et le salon serait injoignable pour toujours. On conclut donc
            // nous-mêmes dans ce cas précis.
            var reconnexionEnAttente = _reconnexions.ContainsKey(url);
            AnnulerReconnexion(url);
            AnnulerLeMinuteur(url);

            if (reconnexionEnAttente)
            {
                _enCours.Remove(url);
                _parametres.Remove(url);
                EtatChange?.Invoke(url, DownloadState.Stopped);
                return;
            }

            job.Engine.Stop();
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