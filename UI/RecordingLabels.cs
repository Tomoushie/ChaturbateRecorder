using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp.UI
{
    // LES CLES SONT CELLES QUI EXISTENT DEJA dans la table de chaines, pas
    // des nouvelles : « job.state.completed », « job.state.failed » et
    // « job.state.stopped » y sont traduites depuis longtemps, et « en cours »
    // comme « hors ligne » aussi. Creer « state.finished » a cote de
    // « job.state.completed » aurait donne deux cles pour la meme idee, dont
    // une seule serait maintenue a la prochaine relecture des traductions.
    public static class RecordingLabels
    {
        /// <summary>
        /// Convertit l'état de téléchargement en état de ligne de salle.
        /// </summary>
        /// <param name="etat">L'état de téléchargement.</param>
        /// <returns>L'état de ligne de salle correspondant.</returns>
        public static RoomRowState EtatDeCarte(DownloadState etat)
        {
            return etat switch
            {
                DownloadState.Running => RoomRowState.Recording,
                DownloadState.Completed => RoomRowState.Finished,
                DownloadState.Failed => RoomRowState.Failed,
                DownloadState.Stopped => RoomRowState.Finished,
                _ => RoomRowState.Idle
            };
        }

        /// <summary>
        /// Rend le libelle correspondant à l'état de téléchargement.
        /// </summary>
        /// <param name="etat">L'état de téléchargement.</param>
        /// <returns>Le libelle correspondant.</returns>
        public static string Libelle(DownloadState etat)
        {
            return etat switch
            {
                DownloadState.Running => Localization.Get("job.running"),
                DownloadState.Completed => Localization.Get("job.state.completed"),
                DownloadState.Failed => Localization.Get("job.state.failed"),
                DownloadState.Stopped => Localization.Get("job.state.stopped"),
                _ => Localization.Get("watch.state.offline")
            };
        }
    }
}