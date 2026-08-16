using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using SentinelGuard;

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly UserSettings _reglages = SettingsManager.Load();

        [ObservableProperty]
        private string _captureDir = "";

        [ObservableProperty]
        private string _cookiesFilePath = "";

        [ObservableProperty]
        private string _proxyUrl = "";

        [ObservableProperty]
        private bool _autoReconnect;

        [ObservableProperty]
        private bool _autoUpdateCheck;

        [ObservableProperty]
        private int _watchIntervalSeconds;

        [ObservableProperty]
        private bool _showLogs;

        [ObservableProperty]
        private bool _francais;

        [ObservableProperty]
        private bool _sombre;

        [ObservableProperty]
        private string _message = "";

        public SettingsViewModel()
        {
            _captureDir = _reglages.CaptureDir ?? "";
            _cookiesFilePath = _reglages.CookiesFilePath ?? "";
            _proxyUrl = _reglages.ProxyUrl ?? "";
            _autoReconnect = _reglages.AutoReconnectDefault;
            _autoUpdateCheck = _reglages.AutoUpdateCheck;
            _watchIntervalSeconds = _reglages.WatchIntervalSeconds;
            _showLogs = _reglages.ShowLogs;
            _francais = Localization.Current == AppLanguage.French;
            _sombre = ThemeManager.Current == AppTheme.Dark;
        }

        partial void OnFrancaisChanged(bool value)
        {
            Localization.Current = value ? AppLanguage.French : AppLanguage.English;
        }

        partial void OnSombreChanged(bool value)
        {
            ThemeManager.Apply(value ? AppTheme.Dark : AppTheme.Light);
        }

        [RelayCommand]
        private void ChoisirDossier()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                if (PathValidator.IsValidPath(dialog.FolderName, mustExist: false, out var motif))
                {
                    CaptureDir = dialog.FolderName;
                    Message = "";
                }
                else
                {
                    Message = Localization.Get("error.invalidFolderSandbox");
                    Logger.Log(motif, LogLevel.WARN);
                }
            }
        }

        [RelayCommand]
        private void ChoisirCookies()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "cookies.txt|*.txt|Tous les fichiers|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                if (PathValidator.IsValidPath(dialog.FileName, mustExist: true, out var motif))
                {
                    CookiesFilePath = dialog.FileName;
                    Message = "";
                }
                else
                {
                    Message = Localization.Get("error.invalidFileSandbox");
                    Logger.Log(motif, LogLevel.WARN);
                }
            }
        }

        [RelayCommand]
        private void Enregistrer()
        {
            _reglages.CaptureDir = !string.IsNullOrEmpty(_captureDir) ? _captureDir : null;
            _reglages.CookiesFilePath = !string.IsNullOrEmpty(_cookiesFilePath) ? _cookiesFilePath : null;
            _reglages.ProxyUrl = !string.IsNullOrEmpty(_proxyUrl) ? _proxyUrl : null;
            _reglages.AutoReconnectDefault = _autoReconnect;
            _reglages.AutoUpdateCheck = _autoUpdateCheck;
            _reglages.WatchIntervalSeconds = Math.Clamp(_watchIntervalSeconds, 30, 3600);
            _reglages.ShowLogs = _showLogs;
            _reglages.Language = _francais ? "fr" : "en";

            try
            {
                SettingsManager.Save(_reglages);
                Message = "Reglages enregistres.";
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Logger.Log(ex.Message, LogLevel.ERROR);
            }
        }
    }
}