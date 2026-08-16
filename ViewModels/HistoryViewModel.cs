using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.ViewModels
{
    public sealed class HistoryItemViewModel
    {
        public HistoryItemViewModel(HistoryEntry entree)
        {
            Nom = entree.Nom;
            CheminComplet = entree.CheminComplet;
            CheminVignette = entree.CheminVignette;
            TailleLisible = HistoryService.FormaterTaille(entree.Taille);
            DateLisible = entree.Date.ToString("g");
        }

        public string Nom { get; }
        public string CheminComplet { get; }
        public string? CheminVignette { get; }
        public string TailleLisible { get; }
        public string DateLisible { get; }
    }

    public partial class HistoryViewModel : ObservableObject
    {
        public ObservableCollection<HistoryItemViewModel> Elements { get; } = new();
        [ObservableProperty] private bool _chargement;
        [ObservableProperty] private bool _vide;

        public HistoryViewModel()
        {
            _ = RafraichirAsync();
        }

        [RelayCommand]
        private async Task RafraichirAsync()
        {
            try
            {
                Chargement = true;
                var entrees = await HistoryService.ListerAsync();
                Elements.Clear();
                foreach (var entree in entrees)
                {
                    Elements.Add(new HistoryItemViewModel(entree));
                }
                Vide = Elements.Count == 0;
            }
            catch (Exception ex)
            {
                Logger.Log($"Historique : rafraîchissement impossible — {ex.Message}", LogLevel.WARN);
            }
            finally
            {
                Chargement = false;
            }
        }

        [RelayCommand]
        private static void OuvrirDossier(string? chemin)
        {
            if (string.IsNullOrEmpty(chemin))
                return;
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{chemin}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log($"Ouverture du dossier impossible — {ex.Message}", LogLevel.WARN);
            }
        }

        [RelayCommand]
        private static void OuvrirFichier(string? chemin)
        {
            if (string.IsNullOrEmpty(chemin) || !File.Exists(chemin))
                return;
            try
            {
                Process.Start(new ProcessStartInfo(chemin) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log($"Ouverture du fichier impossible — {ex.Message}", LogLevel.WARN);
            }
        }
    }
}