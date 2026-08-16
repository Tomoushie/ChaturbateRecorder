// Models/RecordingJob.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Diagnostics;

namespace ChaturbateRecorderApp.Models
{
    public enum RecordingState
    {
        Stopped,      // Non démarré ou arrêté
        Running,      // En cours d'enregistrement
        Error,        // Erreur survenue
        // ... d'autres états si nécessaire (Pending, Paused, ...)
    }

    public partial class RecordingJob : ObservableObject
    {
        public string RoomUrl { get; }
        public string OutputPath { get; } // Chemin complet du fichier de sortie final
        public string LogPath { get; } // Chemin du fichier de log
        public RecordingState State { get; private set; } = RecordingState.Stopped;

        // Pour gérer les processus
        private Process? _ytDlpProcess;
        private Process? _ffmpegProcess; // Si utilisé pour réencodage/minature en parallèle

        // Exposer des infos potentiellement utiles
        [ObservableProperty]
        private string _progressInfo = "Prêt"; // Ex: "50 Mo - 00:15:30"
        [ObservableProperty]
        private DateTime _startTime = DateTime.MinValue;
        [ObservableProperty]
        private DateTime? _stopTime; // Null si non encore arrêté

        public RecordingJob(string roomUrl, string outputPath, string logPath)
        {
            RoomUrl = roomUrl;
            OutputPath = outputPath;
            LogPath = logPath;
        }

        // Méthodes pour contrôler le job
        public void Start(ProcessStartInfo ytDlpStartInfo, ProcessStartInfo? ffmpegStartInfo = null)
        {
            if (State == RecordingState.Running)
            {
                // Déjà en cours
                return;
            }

            try
            {
                _ytDlpProcess = Process.Start(ytDlpStartInfo);
                if (_ytDlpProcess != null)
                {
                    if (ffmpegStartInfo != null)
                    {
                        _ffmpegProcess = Process.Start(ffmpegStartInfo); // Démarrage potentiel d'FFmpeg
                    }
                    State = RecordingState.Running;
                    StartTime = DateTime.Now;
                    ProgressInfo = "Démarré...";
                    // Lancer une tâche pour surveiller la progression ? (à voir plus tard)
                }
                else
                {
                    State = RecordingState.Error;
                    ProgressInfo = "Erreur: Impossible de démarrer le processus.";
                }
            }
            catch (Exception ex)
            {
                State = RecordingState.Error;
                ProgressInfo = $"Erreur: {ex.Message}";
            }
        }

        public void Stop()
        {
            if (State != RecordingState.Running)
            {
                // Rien à arrêter
                return;
            }

            try
            {
                // Tuer les processus
                _ytDlpProcess?.Kill(entireProcessTree: true); // Tue aussi les enfants
                _ffmpegProcess?.Kill(entireProcessTree: true);
                _ytDlpProcess?.WaitForExit(2000); // Attendre un peu
                _ffmpegProcess?.WaitForExit(2000);
                // Optionnel : Fermer proprement avec CloseMainWindow si Kill n'est pas souhaité
                // _ytDlpProcess?.CloseMainWindow();
                // _ffmpegProcess?.CloseMainWindow();
            }
            catch (Exception ex)
            {
                // Gérer l'erreur d'arrêt si nécessaire
                Console.WriteLine($"Erreur lors de l'arrêt du processus d'enregistrement: {ex.Message}");
            }
            finally
            {
                _ytDlpProcess?.Dispose();
                _ffmpegProcess?.Dispose();
                _ytDlpProcess = null;
                _ffmpegProcess = null;
                State = RecordingState.Stopped;
                StopTime = DateTime.Now;
                ProgressInfo = "Arrêté.";
            }
        }

        // Autres méthodes utiles potentielles : Pause, Resume, GetDuration, GetSize, ...
    }
}