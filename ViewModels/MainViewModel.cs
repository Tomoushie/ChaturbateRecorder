// ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Services;
using System;
using System.Windows.Input;

namespace ChaturbateRecorderApp.ViewModels
{
    // Enum pour représenter les différentes vues principales
    public enum MainViewType
    {
        Monitor,
        History,
        Settings
    }

    public partial class MainViewModel : ObservableObject
    {
        // Properties pour les données à afficher dans MainWindow
        [ObservableProperty]
        private string _statusMessage = "Prêt";

        [ObservableProperty]
        private string _licensedTo = ""; // Informations de licence potentiellement issues d'un service

        // Propriété pour contrôler la vue affichée
        [ObservableProperty]
        private MainViewType _currentViewType = MainViewType.Monitor; // Vue par défaut

        // Commands pour les actions de haut niveau (navigation, etc.)
        public ICommand NavigateToHistoryCommand { get; }
        public ICommand NavigateToMonitorCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }

        // Injection d'un service pour gérer la licence (exemple)
        private readonly Services.IPremiumModuleService? _premiumService;

        public MainViewModel(Services.IPremiumModuleService? premiumService) // Injection du service
        {
            _premiumService = premiumService;

            // Initialiser les commandes
            NavigateToHistoryCommand = new RelayCommand(NavigateToHistory);
            NavigateToMonitorCommand = new RelayCommand(NavigateToMonitor);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);

            // Mettre à jour les infos de licence si le service est disponible
            UpdateLicenseInfo();
        }

        private void UpdateLicenseInfo()
        {
            if (_premiumService != null && _premiumService.IsLoaded)
            {
                LicensedTo = _premiumService.LicensedTo ?? "Licence invalide";
                if (!string.IsNullOrEmpty(_premiumService.LicenceProblem))
                {
                    StatusMessage = $"Licence problème: {_premiumService.LicenceProblem}";
                }
                else
                {
                    StatusMessage = "Licence valide - Fonctionnalités premium activées";
                }
            }
            else
            {
                 LicensedTo = "Aucune licence";
                 StatusMessage = "Fonctionnalités premium non disponibles";
            }
        }

        // Méthodes de navigation : mettent simplement à jour CurrentViewType
        private void NavigateToHistory()
        {
            CurrentViewType = MainViewType.History;
            StatusMessage = "Navigation vers l'historique...";
        }

        private void NavigateToMonitor()
        {
            CurrentViewType = MainViewType.Monitor;
            StatusMessage = "Navigation vers la surveillance...";
        }

        private void NavigateToSettings()
        {
            CurrentViewType = MainViewType.Settings;
            StatusMessage = "Navigation vers les paramètres...";
        }
    }
}