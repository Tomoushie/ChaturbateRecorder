using System;
using System.IO;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.Security
{
    /// <summary>
    /// Vérifie que l'exécutable tourne depuis un emplacement légitime : ni un
    /// partage réseau (UNC ou lecteur mappé), ni un dossier temporaire/éphémère
    /// (%TEMP%, Téléchargements, Bureau, Corbeille), ni un dossier compressé
    /// NTFS. Défense contre les "repacks" qui s'exécutent après extraction
    /// furtive dans %TEMP% ou depuis un partage distant non fiable.
    /// Équivalent conceptuel de Test-TrustedBinary côté emplacement du process
    /// lui-même plutôt que côté binaire.
    /// </summary>
    public static class WorkingDirectoryValidator
    {
        public static bool IsAuthorizedLocation(string appDir)
        {
            if (string.IsNullOrWhiteSpace(appDir))
            {
                Logger.Log("Dossier d'exécution vide.", LogLevel.ERROR);
                return false;
            }

            var normalized = appDir.TrimEnd('\\');

            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
            {
                Logger.Log($"Exécution depuis un partage réseau UNC interdite : {normalized}", LogLevel.ERROR);
                return false;
            }

            var root = Path.GetPathRoot(normalized);
            if (!string.IsNullOrEmpty(root))
            {
                try
                {
                    var drive = new DriveInfo(root);
                    if (drive.DriveType == DriveType.Network)
                    {
                        Logger.Log($"Exécution depuis un lecteur réseau mappé interdite : {normalized} ({root})", LogLevel.ERROR);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Impossible de déterminer le type de lecteur pour '{root}' : {ex.Message}", LogLevel.WARN);
                }
            }

            var riskyRoots = new[]
            {
                Path.GetTempPath(),
                Environment.GetEnvironmentVariable("TEMP"),
                Environment.GetEnvironmentVariable("TMP"),
                SafeCombine(Environment.SpecialFolder.UserProfile, "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            };

            foreach (var risky in riskyRoots)
            {
                if (string.IsNullOrEmpty(risky)) continue;
                var normalizedRisky = risky.TrimEnd('\\');
                if (normalized.Equals(normalizedRisky, StringComparison.OrdinalIgnoreCase) ||
                    normalized.StartsWith(normalizedRisky + @"\", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log($"Exécution depuis un dossier temporaire/éphémère interdite : {normalized}", LogLevel.ERROR);
                    return false;
                }
            }

            if (normalized.IndexOf(@"$Recycle.Bin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Logger.Log($"Exécution depuis la corbeille interdite : {normalized}", LogLevel.ERROR);
                return false;
            }

            try
            {
                if (Directory.Exists(normalized))
                {
                    var attrs = File.GetAttributes(normalized);
                    if ((attrs & FileAttributes.Compressed) != 0)
                    {
                        Logger.Log($"Exécution depuis un dossier compressé NTFS interdite : {normalized}", LogLevel.ERROR);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la vérification de compression du dossier '{normalized}' : {ex.Message}", LogLevel.WARN);
            }

            return true;
        }

        private static string? SafeCombine(Environment.SpecialFolder folder, string subFolder)
        {
            try
            {
                var basePath = Environment.GetFolderPath(folder);
                return string.IsNullOrEmpty(basePath) ? null : Path.Combine(basePath, subFolder);
            }
            catch
            {
                return null;
            }
        }
    }
}
