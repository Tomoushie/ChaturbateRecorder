using System;
using System.IO;
using System.Text.Json;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    public class UserSettings
    {
        public string? CaptureDir { get; set; }
        public string? LastSeenVersion { get; set; }
        public string? CookiesFilePath { get; set; }
        public string? ProxyUrl { get; set; }
        public bool HasSeenTutorial { get; set; }
        public bool? AdvancedMode { get; set; }
        // "fr" ou "en" (20.0) — absent/inconnu -> français par défaut.
        public string? Language { get; set; }
        // Déplacée dans la fenêtre Paramètres (19.0) : désormais mémorisée
        // entre les lancements (ne l'était pas quand la case vivait dans le
        // formulaire principal).
        public bool AutoReconnectDefault { get; set; }
        // Affichée une seule fois, la première fois que la fenêtre principale
        // est masquée dans la zone de notification (19.0) au lieu de fermer.
        public bool HasSeenTrayHint { get; set; }
        // Recherche automatique de mise à jour (79.0), activée par défaut.
        // L'initialiseur vaut aussi pour un settings.json antérieur à 79.0 :
        // System.Text.Json construit l'objet puis n'assigne que les propriétés
        // réellement présentes dans le fichier.
        public bool AutoUpdateCheck { get; set; } = true;
        // Dernière version pour laquelle une notification a été affichée, pour
        // ne pas re-notifier la même à chaque passage horaire (79.0).
        public string? LastNotifiedUpdateVersion { get; set; }
        // Intervalle entre deux passages de surveillance (88.0). 120 s par
        // defaut : chaque controle lance un processus yt-dlp, et dix salons
        // verifies toutes les minutes feraient 14 400 requetes par jour vers
        // le site. Plancher a 60 s, applique aussi a la lecture.
        public int WatchIntervalSeconds { get; set; } = 120;
    }

    /// <summary>
    /// Persistance simple des préférences utilisateur (JSON, même pattern que
    /// FavoritesManager) : dossier de sauvegarde choisi, dernière version vue
    /// (pour l'affichage du "Nouveautés" au premier lancement après update).
    /// </summary>
    public static class SettingsManager
    {
        private static string SettingsFile => Path.Combine(AppConfig.AppDir, "settings.json");

        public static UserSettings Load()
        {
            if (!File.Exists(SettingsFile)) return new UserSettings();

            try
            {
                var raw = File.ReadAllText(SettingsFile);
                if (string.IsNullOrWhiteSpace(raw)) return new UserSettings();
                return JsonSerializer.Deserialize<UserSettings>(raw) ?? new UserSettings();
            }
            catch (Exception ex)
            {
                Logger.Log($"Fichier de paramètres illisible, ignoré : {ex.Message}", LogLevel.WARN);
                return new UserSettings();
            }
        }

        public static void Save(UserSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la sauvegarde des paramètres : {ex.Message}", LogLevel.ERROR);
            }
        }
    }
}
