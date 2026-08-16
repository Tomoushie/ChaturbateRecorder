// Models/HistoryItem.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ChaturbateRecorderApp.Models
{
    public partial class HistoryItem : ObservableObject
    {
        // Propriétés basées sur les colonnes de l'ancien ListView (MainForm.cs)
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public long Size { get; set; } = 0;
        public string Duration { get; set; } = "N/A"; // Format humain (ex: "1h23m", "45m12s")
        public DateTime Date { get; set; } = DateTime.MinValue;
        public string PlatformBadge { get; set; } = ""; // Ex: "CB", "TT", "TW" (optionnel, à extraire du nom ou de l'info.json)

        // Propriété pour la miniature (chemin vers le fichier image)
        [ObservableProperty]
        private string? _thumbnailPath; // Chemin local vers une miniature (jpg/png) associée

        // Constructeur
        public HistoryItem(string name, string fullPath, long size, string duration, DateTime date, string platformBadge = "")
        {
            Name = name;
            FullPath = fullPath;
            Size = size;
            Duration = duration;
            Date = date;
            PlatformBadge = platformBadge;
        }
    }
}