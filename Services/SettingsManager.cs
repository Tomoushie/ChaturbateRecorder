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
