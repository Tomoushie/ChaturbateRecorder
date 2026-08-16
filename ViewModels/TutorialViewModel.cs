namespace ChaturbateRecorderApp.ViewModels
{
    using System;
    using System.Collections.Generic;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using ChaturbateRecorderApp.UI;

    public partial class TutorialViewModel : ObservableObject
    {
        public sealed record Etape(string Titre, string Corps);

        private static readonly string[] Cles = { "welcome", "start", "quality", "saveDir", "privacy", "tracking", "support", "security" };

        public IReadOnlyList<Etape> Etapes { get; }

        private int _index;
        public int Index
        {
            get => _index;
            private set
            {
                if (SetProperty(ref _index, value))
                {
                    OnPropertyChanged(nameof(EtapeCourante));
                    OnPropertyChanged(nameof(Progression));
                    OnPropertyChanged(nameof(EstPremiere));
                    OnPropertyChanged(nameof(EstDerniere));
                    OnPropertyChanged(nameof(LibelleSuivant));
                }
            }
        }

        public Etape EtapeCourante => Etapes[Index];
        public string Progression => Localization.Format("tutorial.stepProgress", Index + 1, Etapes.Count);
        public bool EstPremiere => Index == 0;
        public bool EstDerniere => Index == Etapes.Count - 1;
        public string LibelleSuivant => EstDerniere ? Localization.Get("tutorial.finish") : Localization.Get("tutorial.next");
        public string LibellePrecedent { get; } = Localization.Get("tutorial.back");

        public TutorialViewModel()
        {
            // Construite dans une liste locale puis EXPOSEE en lecture seule :
            // remplir directement la propriete demanderait qu'elle soit
            // mutable, et rien ne doit ajouter d'etape apres coup.
            var etapes = new List<Etape>();
            foreach (var cle in Cles)
            {
                etapes.Add(new Etape(
                    Localization.Get("tutorial." + cle + ".title"),
                    Localization.Get("tutorial." + cle + ".body")));
            }
            Etapes = etapes;
        }

        [RelayCommand]
        private void Precedent()
        {
            if (!EstPremiere) Index--;
        }

        public event Action? Termine;

        [RelayCommand]
        private void Suivant()
        {
            if (EstDerniere) Termine?.Invoke();
            else Index++;
        }
    }
}