using System;
using System.IO;

namespace SentinelGuard
{
    /// <summary>
    /// Checks that an executable runs from a legitimate location: not a network
    /// share (UNC or mapped drive), not a temporary or ephemeral folder
    /// (<c>%TEMP%</c>, Downloads, Desktop, recycle bin), and not an
    /// NTFS-compressed folder. A defence against repacks that run straight after
    /// being extracted into <c>%TEMP%</c>, or from an untrusted remote share.
    /// </summary>
    public static class WorkingDirectoryValidator
    {
        /// <summary>
        /// Checks that the application is running from a sane location. Rejects
        /// UNC paths and mapped network drives, temporary and ephemeral folders
        /// (<c>%TEMP%</c>, Downloads, Desktop), the recycle bin, and
        /// NTFS-compressed folders — all places where a binary is easy to
        /// replace, or where execution is a sign the app was launched straight
        /// out of an archive.
        /// </summary>
        /// <param name="appDir">
        /// The directory to check, typically <see cref="System.AppContext.BaseDirectory"/>.
        /// </param>
        /// <returns><see langword="true"/> if the location is acceptable.</returns>
        /// <remarks>
        /// This is a deny list, not an allow list: an unknown location is
        /// accepted. It catches accidental and opportunistic cases, not a
        /// determined attacker who controls where the app is installed.
        /// </remarks>
        public static bool IsAuthorizedLocation(string appDir) =>
            IsAuthorizedLocation(appDir, out _);

        /// <summary>
        /// Same as <see cref="IsAuthorizedLocation(string)"/>, but also reports
        /// why the location was rejected.
        /// </summary>
        /// <param name="appDir">The directory to check.</param>
        /// <param name="reason">
        /// On rejection, a human-readable explanation; <see langword="null"/> on success.
        /// </param>
        /// <returns><see langword="true"/> if the location is acceptable.</returns>
        public static bool IsAuthorizedLocation(string appDir, out string? reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(appDir))
            {
                reason = "Execution directory is empty.";
                return false;
            }

            var normalized = appDir.TrimEnd('\\');

            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
            {
                reason = $"Running from a UNC network share is not allowed: {normalized}";
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
                        reason = $"Running from a mapped network drive is not allowed: {normalized} ({root})";
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
                    reason = $"Running from a temporary or ephemeral folder is not allowed: {normalized}";
                    return false;
                }
            }

            if (normalized.IndexOf(@"$Recycle.Bin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                reason = $"Running from the recycle bin is not allowed: {normalized}";
                return false;
            }

            try
            {
                if (Directory.Exists(normalized))
                {
                    var attrs = File.GetAttributes(normalized);
                    if ((attrs & FileAttributes.Compressed) != 0)
                    {
                        reason = $"Running from an NTFS-compressed folder is not allowed: {normalized}";
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
