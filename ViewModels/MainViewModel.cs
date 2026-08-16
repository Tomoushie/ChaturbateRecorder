using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp.ViewModels
{
    public sealed partial class NavSection : ObservableObject
    {
        public required string Key { get; init; }
        public required string IconKey { get; init; }
        [ObservableProperty] private string _label = "";
    }

    public partial class MainViewModel : ObservableObject, System.IDisposable
    {
        public ObservableCollection<NavSection> Sections { get; } = new()
        {
            new NavSection { Key = "streams", IconKey = "Icon.Camera" },
            new NavSection { Key = "history", IconKey = "Icon.Folder" },
            new NavSection { Key = "settings", IconKey = "Icon.Sliders" },
            new NavSection { Key = "support", IconKey = "Icon.Heart" }
        };

        [ObservableProperty] private NavSection? _selectedSection;

        /// <summary>
        /// Modèle de la section « Enregistrer ». Construit une seule fois et
        /// gardé : le reconstruire à chaque navigation relirait le fichier des
        /// salons et perdrait ce que l'utilisateur vient de taper dans le
        /// champ d'URL en changeant d'onglet.
        /// </summary>
        public StreamsViewModel Streams { get; } = new();

        public void Dispose() => Streams.Dispose();

        public MainViewModel()
        {
            RefreshLabels();
            SelectedSection = Sections[0];
        }

        public void RefreshLabels()
        {
            foreach (var section in Sections)
            {
                section.Label = Localization.Get("nav." + section.Key);
            }
        }
    }
}