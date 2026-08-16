namespace ChaturbateRecorderApp.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using ChaturbateRecorderApp.Config;
    using ChaturbateRecorderApp.Services;
    using ChaturbateRecorderApp.UI;
    using SentinelGuard;

    public partial class StreamsViewModel : ObservableObject
    {
        private readonly RoomStore _store = new();
        public ObservableCollection<RoomCardViewModel> Rooms { get; } = new();
        [ObservableProperty]
        private string _newRoomUrl = "";
        [ObservableProperty]
        private string _addError = "";

        public StreamsViewModel()
        {
            _store.Load();
            Recharger();
        }

        public void Recharger()
        {
            foreach (var carte in Rooms.ToList())
            {
                carte.Detach();
            }
            Rooms.Clear();
            foreach (var entree in _store.Rooms)
            {
                Rooms.Add(Creer(entree));
            }
        }

        private static RoomCardViewModel Creer(RoomEntry entree)
        {
            var carte = new RoomCardViewModel(entree)
            {
                RoomName = Platforms.DisplayName(entree.Url),
                PlatformIconKey = CleDePictogramme(Platforms.Badge(Platforms.Detect(entree.Url)).Icon)
            };
            return carte;
        }

        private static string CleDePictogramme(string icon)
        {
            return icon switch
            {
                "twitch" => "Icon.Twitch",
                "youtube" => "Icon.YouTube",
                "tiktok" => "Icon.TikTok",
                _ => "Icon.Camera"
            };
        }

        [RelayCommand]
        private void Ajouter()
        {
            if (string.IsNullOrWhiteSpace(_newRoomUrl))
            {
                return;
            }

            if (!UrlValidator.IsSafeUrl(_newRoomUrl, AppConfig.Whitelist, AppConfig.Blacklist, out var motif))
            {
                _addError = motif ?? Localization.Get("error.invalidUrl");
                return;
            }

            _addError = "";
            _store.Add(_newRoomUrl);
            _store.Save();
            Recharger();
            _newRoomUrl = "";
        }

        [RelayCommand]
        private void Retirer(RoomCardViewModel? carte)
        {
            if (carte == null)
            {
                return;
            }

            _store.Remove(carte.Url);
            _store.Save();
            Recharger();
        }
    }
}