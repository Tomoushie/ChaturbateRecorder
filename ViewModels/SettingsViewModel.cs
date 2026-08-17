using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using SentinelGuard;

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly UserSettings _reglages = SettingsManager.Load();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LegendeDossierCapture))]
        private string _captureDir = "";

        /// <summary>
        /// Où atterrissent réellement les captures quand le champ est vide.
        ///
        /// Un champ vide n'est PAS une absence de dossier : le réglage vaut
        /// null et l'application suit le « Vidéos » du système. Sans cette
        /// ligne, l'écran laissait croire que rien n'était configuré alors que
        /// les enregistrements partaient bien quelque part — l'écran Historique
        /// les retrouvait, mais les Réglages ne disaient pas où.
        /// </summary>
        public string LegendeDossierCapture =>
            string.IsNullOrWhiteSpace(CaptureDir)
                ? Localization.Format("settings.captureFolderDefault", AppConfig.DefaultCaptureDir())
                : "";

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
            // La legende est une chaine FORMATEE, donc hors de portee de
            // `{ui:Str}` : elle a besoin de son propre reveil, sans quoi elle
            // resterait dans la langue du lancement pendant que tout l'ecran
            // autour d'elle change — exactement le defaut qu'on vient de
            // reparer ailleurs.
            Localization.LanguageChanged += () => OnPropertyChanged(nameof(LegendeDossierCapture));

            _captureDir = _reglages.CaptureDir ?? "";
            _cookiesFilePath = _reglages.CookiesFilePath ?? "";
            _proxyUrl = _reglages.ProxyUrl ?? "";
            _autoReconnect = _reglages.AutoReconnectDefault;
            _autoUpdateCheck = _reglages.AutoUpdateCheck;
            _watchIntervalSeconds = _reglages.WatchIntervalSeconds;
            _showLogs = _reglages.ShowLogs;
            // Le CHAMP et non la propriete, VOLONTAIREMENT : passer par la
            // propriete declencherait `OnFrancaisChanged` pendant la
            // construction, donc reappliquerait la langue deja en place. On veut
            // seulement refleter l'etat courant.
#pragma warning disable MVVMTK0034
            _francais = Localization.Current == AppLanguage.French;
#pragma warning restore MVVMTK0034
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
                    Logger.Log(motif ?? "dossier refuse, motif non precise", LogLevel.WARN);
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
                    Logger.Log(motif ?? "fichier refuse, motif non precise", LogLevel.WARN);
                }
            }
        }

        [RelayCommand]
        private void Enregistrer()
        {
            // Les PROPRIETES, pas les champs : lire `_captureDir` court-circuite
            // la propriete generee, ce que l'analyseur du toolkit signale
            // (MVVMTK0034). C'est sans effet aujourd'hui — meme champ derriere —
            // mais le jour ou la propriete gagne une regle, le contournement la
            // sauterait en silence.
            _reglages.CaptureDir = !string.IsNullOrEmpty(CaptureDir) ? CaptureDir : null;
            _reglages.CookiesFilePath = !string.IsNullOrEmpty(CookiesFilePath) ? CookiesFilePath : null;
            _reglages.ProxyUrl = !string.IsNullOrEmpty(ProxyUrl) ? ProxyUrl : null;
            _reglages.AutoReconnectDefault = AutoReconnect;
            _reglages.AutoUpdateCheck = AutoUpdateCheck;
            _reglages.WatchIntervalSeconds = Math.Clamp(WatchIntervalSeconds, 30, 3600);
            _reglages.ShowLogs = ShowLogs;
            // Le theme suit la case, et il est desormais PERSISTE : c'etait le
            // dernier reglage de cet ecran a ne pas survivre a une fermeture.
            _reglages.Theme = Sombre ? "dark" : "light";
            _reglages.Language = Francais ? "fr" : "en";

            try
            {
                SettingsManager.Save(_reglages);
                Message = Localization.Get("settings.saved");
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Logger.Log(ex.Message, LogLevel.ERROR);
            }
        }
    }
}