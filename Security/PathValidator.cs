using System;
using System.IO;
using System.Text.RegularExpressions;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.Security
{
    /// <summary>
    /// Sandbox de chemins de fichiers : interdiction des UNC, chemins étendus
    /// (\\?\ , \\.\), espace de noms \Device\, flux ADS, noms réservés Windows,
    /// symlinks/reparse points (sur le chemin et sur chaque dossier parent).
    /// Équivalent de Test-ValidPath côté PowerShell.
    /// </summary>
    public static class PathValidator
    {
        private static readonly string[] ReservedNames =
        {
            "CON","PRN","AUX","NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        public static bool IsValidPath(string path, bool mustExist = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Logger.Log("Chemin vide.", LogLevel.ERROR);
                return false;
            }

            if (Regex.IsMatch(path, @"^\\\\[^\?\.]"))
            {
                Logger.Log($"Chemin UNC interdit : {path}", LogLevel.ERROR);
                return false;
            }

            if (Regex.IsMatch(path, @"^\\\\\?\\") || Regex.IsMatch(path, @"^\\\\\.\\"))
            {
                Logger.Log($"Chemin étendu (\\\\?\\ ou \\\\.\\) interdit : {path}", LogLevel.ERROR);
                return false;
            }

            if (path.IndexOf(@"\Device\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Logger.Log($"Chemin faisant référence à \\Device\\ interdit : {path}", LogLevel.ERROR);
                return false;
            }

            var driveLessPath = Regex.Replace(path, "^[a-zA-Z]:", "");
            if (driveLessPath.Contains(':'))
            {
                Logger.Log($"Flux alternatif NTFS (ADS) interdit : {path}", LogLevel.ERROR);
                return false;
            }

            if (Regex.IsMatch(path, "[\x00-\x1F]"))
            {
                Logger.Log($"Caractères de contrôle interdits dans le chemin : {path}", LogLevel.ERROR);
                return false;
            }

            if (!Regex.IsMatch(path, @"^[a-zA-Z]:\\"))
            {
                Logger.Log($"Le chemin doit être un chemin absolu local (ex: C:\\...) : {path}", LogLevel.ERROR);
                return false;
            }

            var segments = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in segments)
            {
                if (Regex.IsMatch(seg, "^[a-zA-Z]:$")) continue;
                var baseName = seg.Split('.')[0].ToUpperInvariant();
                if (Array.IndexOf(ReservedNames, baseName) >= 0)
                {
                    Logger.Log($"Nom de fichier/dossier réservé Windows interdit : '{seg}'", LogLevel.ERROR);
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
                        Logger.Log($"Symlink / reparse point interdit : {path}", LogLevel.ERROR);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de la vérification du chemin '{path}' : {ex.Message}", LogLevel.ERROR);
                    return false;
                }
            }
            else if (mustExist)
            {
                Logger.Log($"Chemin introuvable : {path}", LogLevel.ERROR);
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
                        Logger.Log($"Un dossier parent est un symlink / reparse point : {current}", LogLevel.ERROR);
                        return false;
                    }
                }
                catch
                {
                    // Dossier parent inaccessible : non bloquant ici, comportement identique
                    // à la version PowerShell (ErrorAction SilentlyContinue).
                }

                var newCurrent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(newCurrent) || newCurrent == current) break;
                current = newCurrent;
            }

            return true;
        }
    }
}
