// ViewModels/HistoryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Models;
using ChaturbateRecorderApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics; // Pour Process.Start
using System.IO; // Pour File.Exists, Directory.Exists
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly IHistoryService _historyService;
        private readonly string _captureDir;
        private readonly string _ffprobePath;
        private readonly string _ffmpegPath;
        private readonly string _thumbnailDir;

        // Expose la collection d'entrées d'historique du service
        public ObservableCollection<HistoryItem> HistoryEntries => _historyService.HistoryEntries;

        // Commandes
        public ICommand RefreshCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        // Ajoutez d'autres commandes si nécessaire (Supprimer, Filtres, etc.)

        // Propriété pour le chemin du dossier d'historique (affichage)
        public string CaptureDirectory => _captureDir;

        // Propriété pour le nombre d'entrées (affichage)
        public int EntryCount => HistoryEntries.Count;

        // Propriété pour permettre à la vue de sélectionner un item
        [ObservableProperty]
        private HistoryItem? _selectedEntry;

        public HistoryViewModel(IHistoryService historyService, string captureDirectory, string ffprobePath, string ffmpegPath, string thumbnailDirectory)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _captureDir = captureDirectory ?? throw new ArgumentNullException(nameof(captureDirectory));
            _ffprobePath = ffprobePath ?? throw new ArgumentNullException(nameof(ffprobePath));
            _ffmpegPath = ffmpegPath ?? throw new ArgumentNullException(nameof(ffmpegPath));
            _thumbnailDir = thumbnailDirectory ?? throw new ArgumentNullException(nameof(thumbnailDirectory));

            // Initialiser les commandes
            RefreshCommand = new AsyncRelayCommand(RefreshHistoryAsync);
            OpenFileCommand = new RelayCommand<HistoryItem>(OpenFile, CanOpenFile);
            OpenFolderCommand = new RelayCommand(OpenFolder);

            // Charger l'historique au démarrage du ViewModel
            // On peut appeler RefreshHistoryAsync ici ou laisser le chargement se faire via un bouton.
            // Pour l'instant, on ne le fait pas automatiquement ici, on laisse l'utilisateur cliquer sur Rafraîchir ou on le fait via MainViewModel si nécessaire.
        }

        // Commande : Rafraîchir l'historique
        private async Task RefreshHistoryAsync()
        {
            // Appelle le service pour rafraîchir la liste
            await _historyService.RefreshHistoryAsync(_captureDir, _ffprobePath, _ffmpegPath, _thumbnailDir);
            // EntryCount est recalculé automatiquement via la propriété getter
            OnPropertyChanged(nameof(EntryCount)); // Notifie le changement du nombre d'entrées
        }

        // Commande : Ouvrir un fichier d'historique
        private void OpenFile(HistoryItem? item)
        {
            if (item != null && File.Exists(item.FullPath))
            {
                try
                {
                    // Ouvre le fichier avec le programme par défaut
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.FullPath,
                        UseShellExecute = true
                    })?.Dispose(); // Dispose immédiatement le Process si nécessaire
                }
                catch (Exception ex)
                {
                    // Gérer l'erreur (afficher un message à l'utilisateur)
                    Console.WriteLine($"Erreur lors de l'ouverture du fichier : {ex.Message}");
                }
            }
        }

        private bool CanOpenFile(HistoryItem? item) => item != null && File.Exists(item.FullPath);

        // Commande : Ouvrir le dossier d'historique
        private void OpenFolder()
        {
            if (Directory.Exists(_captureDir))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _captureDir,
                        UseShellExecute = true
                    })?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'ouverture du dossier : {ex.Message}");
                }
            }
        }

        // Autres méthodes potentielles :
        // - DeleteFileCommand
        // - ApplyFiltersCommand (si vous implémentez des filtres)
        // - UpdateEntryCount() si ce n'était pas une propriété calculée
    }
}