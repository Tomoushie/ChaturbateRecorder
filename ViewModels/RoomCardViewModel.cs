using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class RoomCardViewModel : ObservableObject
    {
        private readonly RoomEntry _entree;
        private RoomRowState _state = RoomRowState.Idle;
        private bool _autoRecord;

        public RoomCardViewModel(RoomEntry entree)
        {
            _entree = entree;
            Url = entree.Url;
            AutoRecord = entree.AutoRecord;
            RoomName = entree.Url;
            _state = RoomRowState.Idle;
            ThemeManager.Applied += OnThemeApplied;
        }

        public string Url { get; }

        [ObservableProperty]
        private string _roomName;

        // Initialises ici et pas dans le constructeur : la carte est construite
        // AVANT que la plateforme soit resolue et que l'etat soit libelle. Sans
        // valeur, ces trois champs valent null a la premiere liaison, ce que le
        // compilateur signalait en CS8618.
        [ObservableProperty]
        private string _platformIconKey = "";

        [ObservableProperty]
        private string _detail = "";

        [ObservableProperty]
        private string _stateLabel = "";

        [ObservableProperty]
        private int _progress;

        [ObservableProperty]
        private bool _indeterminate;

        public RoomRowState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    OnPropertyChanged(nameof(StateBrushKey));
                    OnPropertyChanged(nameof(IsExpanded));
                    OnPropertyChanged(nameof(CardHeight));
                }
            }
        }

        public string StateBrushKey => RoomCardVisuals.StateBrushKey(State);
        public bool IsExpanded => RoomCardVisuals.IsExpanded(State);
        public double CardHeight => RoomCardVisuals.HeightFor(State);

        /// <summary>
        /// Vrai tant qu'une capture tourne pour ce salon.
        ///
        /// Distinct de <c>State == Recording</c>, et ce n'est pas un doublon :
        /// l'état décrit ce que la carte MONTRE (il passe par « terminé » ou
        /// « échec »), celui-ci décrit ce que le bouton doit FAIRE. Les deux se
        /// désynchronisent le temps que yt-dlp rende la main — c'est justement
        /// l'instant où un second clic relancerait une capture par-dessus la
        /// première.
        /// </summary>
        [ObservableProperty]
        private bool _isRecording;

        /// <summary>
        /// Le detail « de repos » de la carte — plateforme et date d'ajout.
        ///
        /// Il est garde a part parce que le DECOMPTE du minuteur s'affiche au
        /// meme endroit : sans copie, la premiere seconde de compte a rebours
        /// ecraserait definitivement la plateforme et la date, qui ne
        /// reviendraient qu'au prochain rechargement de la liste.
        /// </summary>
        public string DetailBase { get; set; } = "";

        public bool AutoRecord
        {
            get => _autoRecord;
            set
            {
                if (SetProperty(ref _autoRecord, value))
                {
                    _entree.AutoRecord = value;
                }
            }
        }

        public void Detach()
        {
            ThemeManager.Applied -= OnThemeApplied;
        }

        private void OnThemeApplied()
        {
            OnPropertyChanged(nameof(StateBrushKey));
        }
    }
}