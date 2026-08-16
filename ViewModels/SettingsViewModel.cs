// ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Models;
using ChaturbateRecorderApp.Services;
using System;
using System.IO; // Pour la validation de chemin
using System.Windows.Input;
using System.Windows.Forms; // Pour FolderBrowserDialog et OpenFileDialog (voir remarque ci-dessous)

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private AppSettings _originalSettings; // Pour comparer les changements ou annuler

        // --- Propriétés éditables liées à AppSettings ---
        // On utilise ObservableProperty pour notifier l'UI des changements
        [ObservableProperty]
        private string _captureDir = "";

        [ObservableProperty]
        private string _logDir = "";

        [ObservableProperty]
        private string _toolsDir = "";

        [ObservableProperty]
        private string _tempDir = "";

        [ObservableProperty]
        private string _thumbnailsDir = "";

        [ObservableProperty]
        private string _ytDlpPath = "";

        [ObservableProperty]
        private string _ffmpegPath = "";

        [ObservableProperty]
        private string _ffprobePath = "";

        [ObservableProperty]
        private string _cookiesFilePath = "";

        [ObservableProperty]
        private string _proxyUrl = "";

        [ObservableProperty]
        private int _watchIntervalSeconds = 60;

        [ObservableProperty]
        private bool _autoReconnectDefault = true;

        [ObservableProperty]
        private string _defaultFormat = "best";

        [ObservableProperty]
        private string _defaultContainer = "mkv";

        [ObservableProperty]
        private string _theme = "Light";

        [ObservableProperty]
        private string _language = "en";

        [ObservableProperty]
        private bool _showLogs = false;

        [ObservableProperty]
        private int _logMaxFileSizeBytes = 10485760; // 10 Mo

        [ObservableProperty]
        private int _ytDlpWatchdogTimeoutSeconds = 180; // 3 min

        // Commandes
        public ICommand BrowseCaptureDirCommand { get; }
        public ICommand BrowseLogDirCommand { get; }
        public ICommand BrowseToolsDirCommand { get; }
        public ICommand BrowseTempDirCommand { get; }
        public ICommand BrowseThumbnailsDirCommand { get; }
        public ICommand BrowseYtDlpPathCommand { get; }
        public ICommand BrowseFfmpegPathCommand { get; }
        public ICommand BrowseFfprobePathCommand { get; }
        public ICommand BrowseCookiesPathCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand CancelChangesCommand { get; } // Pour annuler les modifications non sauvegardées

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Charger les paramètres actuels et les copier dans les propriétés du ViewModel
            LoadSettings();

            // Initialiser les commandes
            BrowseCaptureDirCommand = new RelayCommand(BrowseCaptureDir);
            BrowseLogDirCommand = new RelayCommand(BrowseLogDir);
            BrowseToolsDirCommand = new RelayCommand(BrowseToolsDir);
            BrowseTempDirCommand = new RelayCommand(BrowseTempDir);
            BrowseThumbnailsDirCommand = new RelayCommand(BrowseThumbnailsDir);
            BrowseYtDlpPathCommand = new RelayCommand(BrowseYtDlpPath);
            BrowseFfmpegPathCommand = new RelayCommand(BrowseFfmpegPath);
            BrowseFfprobePathCommand = new RelayCommand(BrowseFfprobePath);
            BrowseCookiesPathCommand = new RelayCommand(BrowseCookiesPath);
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            CancelChangesCommand = new RelayCommand(CancelChanges);
        }

        private void LoadSettings()
        {
            // Copie les valeurs de _settingsService.Settings dans les propriétés du ViewModel
            // et sauvegarde une copie initiale
            var currentSettings = _settingsService.Settings;
            _originalSettings = new AppSettings // Crée une copie
            {
                CaptureDir = currentSettings.CaptureDir,
                LogDir = currentSettings.LogDir,
                ToolsDir = currentSettings.ToolsDir,
                TempDir = currentSettings.TempDir,
                ThumbnailsDir = currentSettings.ThumbnailsDir,
                YtDlpPath = currentSettings.YtDlpPath,
                FFmpegPath = currentSettings.FFmpegPath,
                FFprobePath = currentSettings.FFprobePath,
                CookiesFilePath = currentSettings.CookiesFilePath,
                ProxyUrl = currentSettings.ProxyUrl,
                WatchIntervalSeconds = currentSettings.WatchIntervalSeconds,
                AutoReconnectDefault = currentSettings.AutoReconnectDefault,
                DefaultFormat = currentSettings.DefaultFormat,
                DefaultContainer = currentSettings.DefaultContainer,
                Theme = currentSettings.Theme,
                Language = currentSettings.Language,
                ShowLogs = currentSettings.ShowLogs,
                LogMaxFileSizeBytes = currentSettings.LogMaxFileSizeBytes,
                YtDlpWatchdogTimeoutSeconds = currentSettings.YtDlpWatchdogTimeoutSeconds
            };

            // Affecte les propriétés du ViewModel
            CaptureDir = _originalSettings.CaptureDir;
            LogDir = _originalSettings.LogDir;
            ToolsDir = _originalSettings.ToolsDir;
            TempDir = _originalSettings.TempDir;
            ThumbnailsDir = _originalSettings.ThumbnailsDir;
            YtDlpPath = _originalSettings.YtDlpPath;
            FfmpegPath = _originalSettings.FFmpegPath;
            FfprobePath = _originalSettings.FFprobePath;
            CookiesFilePath = _originalSettings.CookiesFilePath;
            ProxyUrl = _originalSettings.ProxyUrl;
            WatchIntervalSeconds = _originalSettings.WatchIntervalSeconds;
            AutoReconnectDefault = _originalSettings.AutoReconnectDefault;
            DefaultFormat = _originalSettings.DefaultFormat;
            DefaultContainer = _originalSettings.DefaultContainer;
            Theme = _originalSettings.Theme;
            Language = _originalSettings.Language;
            ShowLogs = _originalSettings.ShowLogs;
            LogMaxFileSizeBytes = _originalSettings.LogMaxFileSizeBytes;
            YtDlpWatchdogTimeoutSeconds = _originalSettings.YtDlpWatchdogTimeoutSeconds;
        }

        // --- Commandes de navigation/browse ---
        // REMARQUE IMPORTANTE : System.Windows.Forms (FolderBrowserDialog, OpenFileDialog)
        // est spécifique à Windows et n'est pas nativement disponible dans WPF pur.
        // Si vous souhaitez une solution multiplateforme, vous devrez utiliser une bibliothèque
        // tierce (comme Ookii.Dialogs.Wpf) ou implémenter un service d'interface native.
        // Pour l'instant, on garde l'exemple avec Forms, mais cela liera l'application à Windows.
        private void BrowseCaptureDir()
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = CaptureDir };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                CaptureDir = dialog.SelectedPath;
            }
        }

        private void BrowseLogDir()
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = LogDir };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                LogDir = dialog.SelectedPath;
            }
        }

        private void BrowseToolsDir()
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = ToolsDir };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ToolsDir = dialog.SelectedPath;
            }
        }

        private void BrowseTempDir()
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = TempDir };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                TempDir = dialog.SelectedPath;
            }
        }

        private void BrowseThumbnailsDir()
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = ThumbnailsDir };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ThumbnailsDir = dialog.SelectedPath;
            }
        }

        private void BrowseYtDlpPath()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Executables|*.exe|Tous les fichiers (*.*)|*.*",
                FileName = Path.GetFileName(YtDlpPath),
                InitialDirectory = Path.GetDirectoryName(YtDlpPath)
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                YtDlpPath = dialog.FileName;
            }
        }

        private void BrowseFfmpegPath()
        {
             using var dialog = new OpenFileDialog
            {
                Filter = "Executables|*.exe|Tous les fichiers (*.*)|*.*",
                FileName = Path.GetFileName(FfmpegPath),
                InitialDirectory = Path.GetDirectoryName(FfmpegPath)
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FfmpegPath = dialog.FileName;
            }
        }

        private void BrowseFfprobePath()
        {
             using var dialog = new OpenFileDialog
            {
                Filter = "Executables|*.exe|Tous les fichiers (*.*)|*.*",
                FileName = Path.GetFileName(FfprobePath),
                InitialDirectory = Path.GetDirectoryName(FfprobePath)
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FfprobePath = dialog.FileName;
            }
        }

        private void BrowseCookiesPath()
        {
             using var dialog = new OpenFileDialog
            {
                Filter = "Fichiers texte|*.txt|Tous les fichiers (*.*)|*.*",
                FileName = Path.GetFileName(CookiesFilePath),
                InitialDirectory = Path.GetDirectoryName(CookiesFilePath)
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                CookiesFilePath = dialog.FileName;
            }
        }

        // --- Commande de sauvegarde ---
        private async Task SaveSettingsAsync()
        {
            // Met à jour l'objet Settings du service avec les valeurs du ViewModel
            _settingsService.Settings.CaptureDir = CaptureDir;
            _settingsService.Settings.LogDir = LogDir;
            _settingsService.Settings.ToolsDir = ToolsDir;
            _settingsService.Settings.TempDir = TempDir;
            _settingsService.Settings.ThumbnailsDir = ThumbnailsDir;
            _settingsService.Settings.YtDlpPath = YtDlpPath;
            _settingsService.Settings.FFmpegPath = FfmpegPath;
            _settingsService.Settings.FFprobePath = FfprobePath;
            _settingsService.Settings.CookiesFilePath = CookiesFilePath;
            _settingsService.Settings.ProxyUrl = ProxyUrl;
            _settingsService.Settings.WatchIntervalSeconds = WatchIntervalSeconds;
            _settingsService.Settings.AutoReconnectDefault = AutoReconnectDefault;
            _settingsService.Settings.DefaultFormat = DefaultFormat;
            _settingsService.Settings.DefaultContainer = DefaultContainer;
            _settingsService.Settings.Theme = Theme;
            _settingsService.Settings.Language = Language;
            _settingsService.Settings.ShowLogs = ShowLogs;
            _settingsService.Settings.LogMaxFileSizeBytes = LogMaxFileSizeBytes;
            _settingsService.Settings.YtDlpWatchdogTimeoutSeconds = YtDlpWatchdogTimeoutSeconds;

            // Demander au service de sauvegarder
            await _settingsService.SaveAsync();

            // Mettre à jour la copie originale pour les futurs annulations
            LoadSettings(); // Recharge la copie originale à partir des nouveaux paramètres sauvegardés
        }

        // --- Commande d'annulation ---
        private void CancelChanges()
        {
            // Restaure les valeurs du ViewModel à partir de la copie sauvegardée
            CaptureDir = _originalSettings.CaptureDir;
            LogDir = _originalSettings.LogDir;
            ToolsDir = _originalSettings.ToolsDir;
            TempDir = _originalSettings.TempDir;
            ThumbnailsDir = _originalSettings.ThumbnailsDir;
            YtDlpPath = _originalSettings.YtDlpPath;
            FfmpegPath = _originalSettings.FFmpegPath;
            FfprobePath = _originalSettings.FFprobePath;
            CookiesFilePath = _originalSettings.CookiesFilePath;
            ProxyUrl = _originalSettings.ProxyUrl;
            WatchIntervalSeconds = _originalSettings.WatchIntervalSeconds;
            AutoReconnectDefault = _originalSettings.AutoReconnectDefault;
            DefaultFormat = _originalSettings.DefaultFormat;
            DefaultContainer = _originalSettings.DefaultContainer;
            Theme = _originalSettings.Theme;
            Language = _originalSettings.Language;
            ShowLogs = _originalSettings.ShowLogs;
            LogMaxFileSizeBytes = _originalSettings.LogMaxFileSizeBytes;
            YtDlpWatchdogTimeoutSeconds = _originalSettings.YtDlpWatchdogTimeoutSeconds;
        }
    }
}