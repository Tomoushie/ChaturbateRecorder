// ViewModels/MonitorViewModel.cs (mis à jour)
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Models;
using ChaturbateRecorderApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class MonitorViewModel : ObservableObject
    {
        // ... champs existants ...
        private readonly IMonitorService _monitorService;
        // AJOUTER CE CHAMP :
        private readonly IRecordingService _recordingService;

        // ... propriétés existantes ...
        public ObservableCollection<MonitoredRoom> MonitoredRooms => _monitorService.Rooms;

        [ObservableProperty]
        private string _newRoomUrl = "";

        // AJOUTER CETTE PROPRIÉTÉ :
        [ObservableProperty]
        private MonitoredRoom? _selectedRoom; // Pour savoir sur quel salon agir


        // AJOUTER CES COMMANDES :
        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }

        // ... commandes existantes ...
        public ICommand AddRoomCommand { get; }
        public ICommand RemoveSelectedRoomCommand { get; }

        // ... constructeur existant, AJOUTER LE SERVICE :
        public MonitorViewModel(IMonitorService monitorService, IRecordingService recordingService) // Injecter RecordingService
        {
            _monitorService = monitorService;
            // AJOUTER CETTE LIGNE :
            _recordingService = recordingService;

            // Initialiser les commandes
            AddRoomCommand = new RelayCommand(AddRoom, CanAddRoom);
            RemoveSelectedRoomCommand = new RelayCommand<MonitoredRoom>(RemoveRoom, CanRemoveRoom);

            // AJOUTER CES LIGNES :
            StartRecordingCommand = new RelayCommand<MonitoredRoom>(StartRecording, CanStartRecording);
            StopRecordingCommand = new RelayCommand<MonitoredRoom>(StopRecording, CanStopRecording);
        }

        // ... méthodes existantes ...

        // AJOUTER CES MÉTHODES :
        private bool CanStartRecording(MonitoredRoom? room) => room != null && room.Status == "En ligne" && !_recordingService.IsRecording(room.Url);
        private void StartRecording(MonitoredRoom room) => _monitorService.StartRecordingForRoom(room);

        private bool CanStopRecording(MonitoredRoom? room) => room != null && _recordingService.IsRecording(room.Url);
        private void StopRecording(MonitoredRoom room) => _monitorService.StopRecordingForRoom(room);

        // ... reste inchangé ...
    }
}