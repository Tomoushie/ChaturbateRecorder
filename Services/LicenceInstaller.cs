using System;
using System.IO;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Ce qui a empêché une installation de licence. Volontairement
    /// indépendant de la langue : la fenêtre traduit, ce service ne connaît
    /// que des cas.
    /// </summary>
    internal enum IssueInstallation
    {
        Aucune,
        /// <summary>Fichier choisi introuvable, vide ou déraisonnablement gros.</summary>
        FichierSource,
        /// <summary>Le fichier a été posé, mais la licence a été REFUSÉE (et l'ancienne restaurée).</summary>
        LicenceRefusee,
        /// <summary>Copie/sauvegarde impossible : droits, disque plein, antivirus.</summary>
        Ecriture,
    }

    internal sealed class ResultatInstallation
    {
        internal ResultatInstallation(IssueInstallation issue, string detail = "")
        {
            Issue = issue;
            Detail = detail;
        }

        internal bool Succes => Issue == IssueInstallation.Aucune;
        internal IssueInstallation Issue { get; }

        /// <summary>Message technique (exception, chemin) pour le journal — jamais affiché tel quel.</summary>
        internal string Detail { get; }
    }

    /// <summary>
    /// Installe un <c>licence.key</c> choisi par l'acheteur, en remplaçant
    /// l'éventuel précédent.
    ///
    /// **Pourquoi ce service existe** : la consigne historique était « copie le
    /// fichier à côté de ChaturbateRecorder.exe, puis relance ». C'est
    /// exactement le genre de manipulation qu'un acheteur pressé rate — ou fait
    /// au mauvais endroit, ou après avoir téléchargé le fichier dans un dossier
    /// temporaire. Ici, l'application copie elle-même au bon endroit.
    ///
    /// **Une licence refusée ne doit JAMAIS écraser une licence qui
    /// fonctionnait.** D'où la sauvegarde avant remplacement et la restauration
    /// si le rechargement refuse le nouveau fichier : quelqu'un qui se trompe
    /// de fichier ne perd pas son achat.
    ///
    /// Le fichier de sauvegarde (<c>licence.key.avant</c>) reste sur disque
    /// après une installation réussie : c'est une roue de secours manuelle, et
    /// aucun code ne le lit.
    /// </summary>
    internal static class LicenceInstaller
    {
        internal const string BackupSuffix = ".avant";

        /// <summary>
        /// Un fichier de licence est du JSON de quelques centaines d'octets.
        /// La borne évite qu'un fichier choisi par erreur (vidéo, ISO) soit
        /// chargé entier en mémoire avant d'être rejeté.
        /// </summary>
        private const long TailleMaxOctets = 1024 * 1024;

        /// <summary>
        /// Copie <paramref name="cheminSource"/> vers
        /// <c>%EXEDIR%\licence.key</c> (ou <paramref name="cheminCible"/>,
        /// fourni par les tests), puis appelle <paramref name="recharger"/>
        /// pour faire relire la licence au composant.
        ///
        /// <paramref name="recharger"/> retourne vrai si la licence est
        /// ACCEPTÉE après relecture. En cas de refus, l'ancien fichier est
        /// remis en place et le composant relu une seconde fois — pour que
        /// l'état en mémoire corresponde de nouveau au disque.
        /// </summary>
        internal static ResultatInstallation Installer(
            string cheminSource, Func<bool> recharger, string? cheminCible = null)
        {
            byte[] nouveau;
            try
            {
                if (!File.Exists(cheminSource))
                    return new ResultatInstallation(IssueInstallation.FichierSource, "fichier choisi introuvable");

                var taille = new FileInfo(cheminSource).Length;
                if (taille == 0 || taille > TailleMaxOctets)
                    return new ResultatInstallation(IssueInstallation.FichierSource,
                        $"taille refusée : {taille} octet(s)");

                nouveau = File.ReadAllBytes(cheminSource);
            }
            catch (Exception ex)
            {
                return new ResultatInstallation(IssueInstallation.FichierSource, ex.Message);
            }

            return InstallerContenu(nouveau, recharger, cheminCible);
        }

        /// <summary>
        /// Même chose, à partir du CONTENU déjà en mémoire. Utilisé par
        /// l'activation en ligne : la licence arrive dans la réponse HTTP,
        /// inutile de la faire transiter par un fichier temporaire.
        /// </summary>
        internal static ResultatInstallation InstallerContenu(
            byte[] nouveau, Func<bool> recharger, string? cheminCible = null)
        {
            if (nouveau == null || nouveau.Length == 0 || nouveau.Length > TailleMaxOctets)
                return new ResultatInstallation(IssueInstallation.FichierSource,
                    $"taille refusée : {nouveau?.Length ?? 0} octet(s)");

            var cible = cheminCible ?? Path.Combine(AppConfig.AppDir, PremiumBinding.LicenceFileName);

            // Lu ENTIÈREMENT avant de toucher à la cible : si l'acheteur a
            // choisi le fichier déjà en place (ou la sauvegarde elle-même), la
            // sauvegarde ne peut plus l'écraser sous nos pieds.
            byte[]? ancienne = null;
            try
            {
                if (File.Exists(cible)) ancienne = File.ReadAllBytes(cible);
            }
            catch (Exception ex)
            {
                return new ResultatInstallation(IssueInstallation.Ecriture,
                    $"lecture de la licence actuelle impossible : {ex.Message}");
            }

            try
            {
                if (ancienne != null)
                    File.WriteAllBytes(cible + BackupSuffix, ancienne);

                File.WriteAllBytes(cible, nouveau);
            }
            catch (Exception ex)
            {
                Restaurer(cible, ancienne, recharger);
                return new ResultatInstallation(IssueInstallation.Ecriture, ex.Message);
            }

            if (recharger()) return new ResultatInstallation(IssueInstallation.Aucune);

            // Refusée : on remet l'ancienne (ou on supprime s'il n'y en avait
            // pas), puis on relit. Une licence invalide laissée en place
            // désactiverait un produit qui marchait avant le clic.
            Restaurer(cible, ancienne, recharger);
            return new ResultatInstallation(IssueInstallation.LicenceRefusee);
        }

        private static void Restaurer(string cible, byte[]? ancienne, Func<bool> recharger)
        {
            try
            {
                if (ancienne == null) File.Delete(cible);
                else File.WriteAllBytes(cible, ancienne);
            }
            catch
            {
                // Le pire cas est déjà arrivé (installation ratée). Échouer ici
                // masquerait la cause d'origine ; le prochain démarrage relira
                // simplement un fichier refusé.
            }

            try { recharger(); }
            catch
            {
                // Idem : l'état de la fenêtre sera rafraîchi au prochain
                // démarrage. Un service de copie de fichier ne fait pas tomber
                // l'application.
            }
        }
    }
}
