using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    public sealed record HistoryEntry(string Nom, string CheminComplet, long Taille, DateTime Date, string? CheminVignette);

    public static class HistoryService
    {
        private static readonly string[] Extensions = { ".mp4", ".mkv", ".mov" }; // Les memes trois que le WinForms. Un .part en cours d'ecriture n'y figure pas : il n'est pas encore une video, et l'afficher ferait croire a un enregistrement disponible.

        public const int Maximum = 50; // Borne reprise telle quelle. Le dossier de capture peut contenir des milliers de fichiers, et les cinquante plus recents sont les seuls qu'on parcourt en pratique.

        public static Task<IReadOnlyList<HistoryEntry>> ListerAsync()
        {
            return Task.Run(() => Lister());
        }

        public static IReadOnlyList<HistoryEntry> Lister()
        {
            var dossier = AppConfig.CaptureDir;
            if (!Directory.Exists(dossier)) return Array.Empty<HistoryEntry>();

            try
            {
                return new DirectoryInfo(dossier).GetFiles()
                    .Where(f => Extensions.Contains(f.Extension.ToLowerInvariant()))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Take(Maximum)
                    .Select(f => new HistoryEntry(
                        f.Name,
                        f.FullName,
                        f.Length,
                        f.LastWriteTime,
                        Vignette(f)
                    ))
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Log($"Historique : lecture du dossier de capture impossible — {ex.Message}", LogLevel.WARN);
                return Array.Empty<HistoryEntry>();
            }
        }

        private static string? Vignette(FileInfo video)
        {
            var chemin = Path.Combine(video.DirectoryName!, Path.GetFileNameWithoutExtension(video.Name) + ".jpg");
            return File.Exists(chemin) ? chemin : null;
        }

        public static string FormaterTaille(long octets)
        {
            if (octets >= 1073741824) return $"{octets / 1073741824.0:0.#} Go";
            if (octets >= 1048576) return $"{octets / 1048576.0:0.#} Mo";
            if (octets >= 1024) return $"{octets / 1024.0:0.#} Ko";
            return $"{octets} o";
        }
    }
}