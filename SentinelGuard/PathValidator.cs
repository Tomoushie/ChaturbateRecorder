using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SentinelGuard
{
    /// <summary>
    /// Sandbox de chemins de fichiers : interdiction des UNC, chemins étendus
    /// (\\?\ , \\.\), espace de noms \Device\, flux ADS, noms réservés Windows,
    /// symlinks/reparse points (sur le chemin et sur chaque dossier parent).
    /// </summary>
    public static class PathValidator
    {
        private static readonly string[] ReservedNames =
        {
            "CON","PRN","AUX","NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        /// <summary>
        /// Checks that <paramref name="path"/> is a plain, absolute, local path,
        /// free of the tricks commonly used to escape a directory or to reach a
        /// device: UNC paths, extended paths, <c>\Device\</c> namespace,
        /// alternate data streams, control characters, reserved device names,
        /// and symlinks or reparse points (on the path itself and on every
        /// existing parent directory).
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="mustExist">
        /// When <see langword="true"/>, a path that does not exist on disk is
        /// rejected. When <see langword="false"/> (default), a well-formed path
        /// that does not exist yet is accepted — useful for an output file you
        /// are about to create.
        /// </param>
        /// <returns><see langword="true"/> if the path is acceptable.</returns>
        public static bool IsValidPath(string path, bool mustExist = false) =>
            IsValidPath(path, mustExist, out _);

        /// <summary>
        /// Same as <see cref="IsValidPath(string, bool)"/>, but also reports why
        /// the path was rejected.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="mustExist">
        /// When <see langword="true"/>, a path that does not exist on disk is rejected.
        /// </param>
        /// <param name="reason">
        /// On rejection, a human-readable explanation; <see langword="null"/>
        /// when the path is accepted. Nothing is logged — what you do with this
        /// message is up to you.
        /// </param>
        /// <returns><see langword="true"/> if the path is acceptable.</returns>
        public static bool IsValidPath(string path, bool mustExist, out string? reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                reason = "Chemin vide.";
                return false;
            }

            if (Regex.IsMatch(path, @"^\\\\[^\?\.]"))
            {
                reason = $"Chemin UNC interdit : {path}";
                return false;
            }

            if (Regex.IsMatch(path, @"^\\\\\?\\") || Regex.IsMatch(path, @"^\\\\\.\\"))
            {
                reason = $"Chemin étendu (\\\\?\\ ou \\\\.\\) interdit : {path}";
                return false;
            }

            if (path.IndexOf(@"\Device\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                reason = $"Chemin faisant référence à \\Device\\ interdit : {path}";
                return false;
            }

            var driveLessPath = Regex.Replace(path, "^[a-zA-Z]:", "");
            if (driveLessPath.Contains(':'))
            {
                reason = $"Flux alternatif NTFS (ADS) interdit : {path}";
                return false;
            }

            if (Regex.IsMatch(path, "[\x00-\x1F]"))
            {
                reason = $"Caractères de contrôle interdits dans le chemin : {path}";
                return false;
            }

            if (!Regex.IsMatch(path, @"^[a-zA-Z]:\\"))
            {
                reason = $"Le chemin doit être un chemin absolu local (ex: C:\\...) : {path}";
                return false;
            }

            var segments = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in segments)
            {
                if (Regex.IsMatch(seg, "^[a-zA-Z]:$")) continue;
                var baseName = seg.Split('.')[0].ToUpperInvariant();
                if (Array.IndexOf(ReservedNames, baseName) >= 0)
                {
                    reason = $"Nom de fichier/dossier réservé Windows interdit : '{seg}'";
                    return false;
                }
            }

            if (File.Exists(path) || Directory.Exists(path))
            {
                try
                {
                    var attrs = File.GetAttributes(path);
                    if ((attrs & FileAttributes.ReparsePoint) != 0)
                    {
                        reason = $"Symlink / reparse point interdit : {path}";
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    reason = $"Erreur lors de la vérification du chemin '{path}' : {ex.Message}";
                    return false;
                }
            }
            else if (mustExist)
            {
                reason = $"Chemin introuvable : {path}";
                return false;
            }

            // Vérifie aussi que chaque dossier parent existant n'est pas un reparse point.
            var current = Path.GetDirectoryName(path);
            while (!string.IsNullOrEmpty(current) && Directory.Exists(current))
            {
                try
                {
                    var attrs = File.GetAttributes(current);
                    if ((attrs & FileAttributes.ReparsePoint) != 0)
                    {
                        reason = $"Un dossier parent est un symlink / reparse point : {current}";
                        return false;
                    }
                }
                catch
                {
                    // Dossier parent inaccessible : non bloquant (comportement volontaire).
                }

                var newCurrent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(newCurrent) || newCurrent == current) break;
                current = newCurrent;
            }

            return true;
        }
    }
}
