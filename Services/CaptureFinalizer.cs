using System;
using System.IO;
using System.Threading.Tasks;

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
    /// La miniature n'est PAS reprise ici, décision du mainteneur : elle
    /// demande ffmpeg, le mode sans échec et le rafraîchissement de
    /// l'historique, et la version WinForms y a déjà payé deux défauts.
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
    }
}
