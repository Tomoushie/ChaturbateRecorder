using System;
using System.IO;

namespace ChaturbateRecorder.Security
{
    /// <summary>
    /// Vérifie qu'un exécutable tourne depuis un emplacement légitime : ni un
    /// partage réseau (UNC ou lecteur mappé), ni un dossier temporaire/éphémère
    /// (%TEMP%, Téléchargements, Bureau, Corbeille), ni un dossier compressé
    /// NTFS. Défense contre les "repacks" qui s'exécutent après extraction
    /// furtive dans %TEMP% ou depuis un partage distant non fiable.
    /// </summary>
    public static class WorkingDirectoryValidator
    {
        public static bool IsAuthorizedLocation(string appDir) =>
            IsAuthorizedLocation(appDir, out _);

        public static bool IsAuthorizedLocation(string appDir, out string? reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(appDir))
            {
                reason = "Dossier d'exécution vide.";
                return false;
            }

            var normalized = appDir.TrimEnd('\\');

            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
            {
                reason = $"Exécution depuis un partage réseau UNC interdite : {normalized}";
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
                        reason = $"Exécution depuis un lecteur réseau mappé interdite : {normalized} ({root})";
                        return false;
                    }
                }
                catch
                {
                    // Type de lecteur indéterminable : non bloquant (comportement volontaire).
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
                    reason = $"Exécution depuis un dossier temporaire/éphémère interdite : {normalized}";
                    return false;
                }
            }

            if (normalized.IndexOf(@"$Recycle.Bin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                reason = $"Exécution depuis la corbeille interdite : {normalized}";
                return false;
            }

            try
            {
                if (Directory.Exists(normalized))
                {
                    var attrs = File.GetAttributes(normalized);
                    if ((attrs & FileAttributes.Compressed) != 0)
                    {
                        reason = $"Exécution depuis un dossier compressé NTFS interdite : {normalized}";
                        return false;
                    }
                }
            }
            catch
            {
                // Statut de compression indéterminable : non bloquant (comportement volontaire).
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
