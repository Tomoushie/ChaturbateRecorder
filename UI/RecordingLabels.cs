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

        /// <summary>
        /// La barre de la carte doit-elle être INDÉTERMINÉE ?
        ///
        /// **Oui dès qu'une capture tourne sans progression chiffrée**, et c'est
        /// le cas NORMAL d'un direct : il n'a pas de fin connue, yt-dlp ne rend
        /// donc souvent aucun pourcentage. Laissée déterminée à 0, la barre
        /// reste VIDE pendant toute la capture — l'écran dit « en cours » à côté
        /// d'une barre morte, ce qui se lit comme un enregistrement figé.
        ///
        /// Le défaut a vécu jusqu'au premier essai sur un vrai direct : rien ne
        /// posait l'indétermination au DÉMARRAGE, seulement à la réception d'une
        /// progression qui n'arrivait jamais. Trouvé par le mainteneur, sur une
        /// capture, en comparant avec la version WinForms dont la barre vit.
        ///
        /// Fonction pure et isolée pour la même raison que ses deux voisines :
        /// c'est une décision d'affichage, et une décision se vérifie.
        /// </summary>
        /// <param name="enCours">Vrai tant qu'une capture tourne pour ce salon.</param>
        /// <param name="progression">Dernier pourcentage reçu, 0 si aucun.</param>
        public static bool BarreIndeterminee(bool enCours, int progression) =>
            enCours && progression <= 0;
    }
}