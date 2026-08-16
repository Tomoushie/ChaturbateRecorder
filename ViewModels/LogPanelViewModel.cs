using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class LogPanelViewModel : ObservableObject, IDisposable
    {
        public sealed record LigneJournal(string Texte, LogLevel Niveau)
        {
            public string CleCouleur => Niveau switch
            {
                LogLevel.ERROR => "Brush.Danger",
                LogLevel.WARN => "Brush.Warning",
                _ => "Brush.FgMuted",
            };
        }

        public ObservableCollection<LigneJournal> Lignes { get; } = new();
        public const int MaximumLignes = 500;

        [ObservableProperty] private bool _visible;

        public LogPanelViewModel()
        {
            _visible = SettingsManager.Load().ShowLogs;
            Logger.OnLogLine += Recevoir;
        }

        private void Recevoir(string texte, LogLevel niveau)
        {
            if (Application.Current?.Dispatcher is { } d && !d.CheckAccess())
            {
                d.Invoke(() => Ajouter(texte, niveau));
            }
            else
            {
                Ajouter(texte, niveau);
            }
        }

        private void Ajouter(string texte, LogLevel niveau)
        {
            Lignes.Add(new LigneJournal(texte, niveau));
            if (Lignes.Count > MaximumLignes)
            {
                Lignes.RemoveAt(0);
            }
        }

        [RelayCommand]
        private void Vider() => Lignes.Clear();

        [RelayCommand]
        private static void OuvrirDossier()
        {
            try
            {
                Process.Start(new ProcessStartInfo(AppConfig.LogDir) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogLevel.ERROR);
            }
        }

        public void Dispose() => Logger.OnLogLine -= Recevoir;
    }
}