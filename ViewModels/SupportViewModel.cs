using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class SupportViewModel : ObservableObject
    {
        public ObservableCollection<string> Noms { get; } = new();
        [ObservableProperty] private string _statut = "";
        [ObservableProperty] private bool _chargement;

        public bool Vide => Noms.Count == 0;

        public SupportViewModel()
        {
            // Pas d'AddRange : ObservableCollection n'en a pas, et la version
            // de List<T> leverait un evenement par element de toute facon.
            foreach (var nom in SupportersProvider.FromEmbedded().Names)
                Noms.Add(nom);
            Statut = "";
        }

        [RelayCommand]
        private async Task ActualiserAsync()
        {
            Chargement = true;
            Statut = Localization.Get("thanks.refreshing");
            try
            {
                var liste = await SupportersProvider.LoadAsync();
                Noms.Clear();
                foreach (var nom in liste.Names)
                {
                    Noms.Add(nom);
                }
                Statut = liste.Origin == SupportersOrigin.Refreshed ? Localization.Get("thanks.upToDate") : Localization.Get("thanks.offline");
                OnPropertyChanged(nameof(Vide));
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogLevel.WARN);
                Statut = Localization.Get("thanks.offline");
            }
            finally
            {
                Chargement = false;
            }
        }

        [RelayCommand]
        private static void OuvrirDon()
        {
            try
            {
                Process.Start(new ProcessStartInfo(AppConfig.DonateUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogLevel.WARN);
            }
        }

        [RelayCommand]
        private static void OuvrirParrainage()
        {
            try
            {
                Process.Start(new ProcessStartInfo(AppConfig.SponsorUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogLevel.WARN);
            }
        }
    }
}