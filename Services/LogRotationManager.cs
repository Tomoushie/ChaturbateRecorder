using System;
using System.IO;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Rotation et purge des fichiers de logs (2.4). Volontairement silencieuse
    /// en cas d'erreur — RotateIfTooLarge est appelée depuis Logger.Log lui-même,
    /// un appel à Logger.Log en cas d'échec créerait une boucle.
    /// </summary>
    public static class LogRotationManager
    {
        /// <summary>
        /// Supprime les fichiers du dossier de logs non modifiés depuis plus de
        /// maxAgeDays. Appelée une seule fois au démarrage : à ce moment-là,
        /// aucun fichier d'une session précédente n'est encore ouvert/verrouillé.
        /// </summary>
        public static void PurgeOldLogs(string logDir, int maxAgeDays)
        {
            if (!Directory.Exists(logDir)) return;

            var cutoff = DateTime.Now.AddDays(-maxAgeDays);
            foreach (var file in Directory.GetFiles(logDir))
            {
                try
                {
                    if (File.GetLastWriteTime(file) < cutoff)
                        File.Delete(file);
                }
                catch
                {
                    // Fichier verrouillé/inaccessible : non bloquant, retenté au prochain démarrage.
                }
            }
        }

        /// <summary>
        /// Si le fichier dépasse la taille max, le renomme avec un suffixe
        /// horodaté pour libérer le nom d'origine (l'appelant peut alors
        /// recréer un fichier vide sous le nom d'origine). Ne s'applique
        /// qu'aux fichiers non verrouillés au moment de l'appel.
        /// </summary>
        public static bool RotateIfTooLarge(string filePath, long maxSizeBytes)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists || maxSizeBytes <= 0 || info.Length < maxSizeBytes) return false;

                var rotatedPath = Path.Combine(
                    info.DirectoryName ?? "",
                    $"{Path.GetFileNameWithoutExtension(filePath)}-{DateTime.Now:HHmmss}{info.Extension}");
                File.Move(filePath, rotatedPath);
                return true;
            }
            catch
            {
                // Non bloquant : au pire le fichier continue de grossir jusqu'au prochain appel.
                return false;
            }
        }
    }
}
