using System;
using System.IO;

namespace SentinelGuard
{
    /// <summary>
    /// Keeps log files from growing without bound, and clears out old ones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one place in the package that touches files you own rather
    /// than inspecting files you distrust, and it belongs here for a practical
    /// reason: a process supervised by <see cref="GuardedProcessRunner"/> can
    /// emit output for hours, and an unbounded log file is a way to fill a disk
    /// — including the disk the application writes its real output to.
    /// </para>
    /// <para>
    /// <b>Every failure is swallowed on purpose.</b> Rotation runs from inside
    /// the logging path itself; reporting a rotation failure through the log
    /// would be a loop, and failing loudly would take down an application over
    /// housekeeping. The return value tells the caller what happened.
    /// </para>
    /// </remarks>
    public static class LogFileRotator
    {
        /// <summary>
        /// Renames <paramref name="filePath"/> with a timestamp suffix if it has
        /// grown past <paramref name="maxSizeBytes"/>, freeing the original name
        /// so the caller can reopen an empty file under it.
        /// </summary>
        /// <param name="filePath">Log file to check.</param>
        /// <param name="maxSizeBytes">
        /// Size threshold in bytes. Zero or negative disables rotation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the file was renamed. <see langword="false"/>
        /// if it was small enough, missing, or locked by another handle.
        /// </returns>
        public static bool RotateIfTooLarge(string filePath, long maxSizeBytes) =>
            RotateIfTooLarge(filePath, maxSizeBytes, out _);

        /// <summary>
        /// Renames <paramref name="filePath"/> if it exceeds
        /// <paramref name="maxSizeBytes"/>, reporting why nothing happened.
        /// </summary>
        /// <param name="filePath">Log file to check.</param>
        /// <param name="maxSizeBytes">Size threshold in bytes; zero or negative disables rotation.</param>
        /// <param name="reason">
        /// Why the file was not rotated, or <see langword="null"/> if it was.
        /// </param>
        /// <returns><see langword="true"/> if the file was renamed.</returns>
        public static bool RotateIfTooLarge(string filePath, long maxSizeBytes, out string? reason)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                reason = "Log file path is empty.";
                return false;
            }

            if (maxSizeBytes <= 0)
            {
                reason = "Rotation is disabled (size threshold is zero or negative).";
                return false;
            }

            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists)
                {
                    reason = "Log file does not exist yet.";
                    return false;
                }

                if (info.Length < maxSizeBytes)
                {
                    reason = "Log file is still below the size threshold.";
                    return false;
                }

                var rotatedPath = Path.Combine(
                    info.DirectoryName ?? string.Empty,
                    $"{Path.GetFileNameWithoutExtension(filePath)}-{DateTime.Now:yyyyMMdd-HHmmss}{info.Extension}");

                File.Move(filePath, rotatedPath);
                reason = null;
                return true;
            }
            catch (Exception ex)
            {
                // Le cas courant : le fichier est encore ouvert en écriture.
                // Non bloquant — au pire il continue de grossir jusqu'au
                // prochain passage.
                reason = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Deletes every file in <paramref name="logDirectory"/> last modified
        /// more than <paramref name="maxAgeDays"/> days ago.
        /// </summary>
        /// <param name="logDirectory">Directory to sweep. Not recursive.</param>
        /// <param name="maxAgeDays">
        /// Maximum age in days. Zero or negative deletes nothing, so a
        /// misconfigured setting cannot wipe a directory.
        /// </param>
        /// <returns>Number of files actually deleted.</returns>
        /// <remarks>
        /// Call this at startup: no file from an earlier session is open yet, so
        /// nothing is locked. Files that cannot be deleted are skipped and
        /// retried on the next run.
        /// </remarks>
        public static int PurgeOlderThan(string logDirectory, int maxAgeDays)
        {
            if (maxAgeDays <= 0 || string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
                return 0;

            var cutoff = DateTime.Now.AddDays(-maxAgeDays);
            var deleted = 0;

            foreach (var file in Directory.GetFiles(logDirectory))
            {
                try
                {
                    if (File.GetLastWriteTime(file) >= cutoff) continue;
                    File.Delete(file);
                    deleted++;
                }
                catch
                {
                    // Verrouillé ou inaccessible : sans importance, on repassera.
                }
            }

            return deleted;
        }
    }
}
