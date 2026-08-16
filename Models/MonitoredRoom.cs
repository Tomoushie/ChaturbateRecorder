// Models/MonitoredRoom.cs (mis à jour)
// ... using ...
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ChaturbateRecorderApp.Models
{
    public partial class MonitoredRoom : ObservableObject
    {
        // ... propriétés existantes ...

        [ObservableProperty]
        private string _recordingStatus = "Non enregistré"; // Ex: "Non enregistré", "En cours", "Arrêté", "Erreur"

        [ObservableProperty]
        private string _recordingProgress = ""; // Ex: "50 Mo - 00:15:30"

        // Lier l'état d'enregistrement à l'état du job
        public void UpdateRecordingState(RecordingState state, string progress = "")
        {
            RecordingProgress = progress;
            switch (state)
            {
                case RecordingState.Running:
                    RecordingStatus = "En cours";
                    break;
                case RecordingState.Stopped:
                    RecordingStatus = "Arrêté";
                    break;
                case RecordingState.Error:
                    RecordingStatus = "Erreur";
                    break;
                default:
                    RecordingStatus = "Non enregistré";
                    break;
            }
        }
    }
}