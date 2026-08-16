using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    public class TrustedBinaryEntry
    {
        public string Sha256 { get; set; } = "";
        public DateTime TrustedAtUtc { get; set; }
    }

    /// <summary>
    /// Confiance à la première utilisation (TOFU) pour yt-dlp/ffmpeg : au-delà
    /// du hash figé dans AppConfig (celui testé par le mainteneur au moment de
    /// la release), l'utilisateur peut approuver explicitement un binaire dont
    /// le hash a changé (yt-dlp en particulier est mis à jour très souvent —
    /// un hash figé dans le binaire de l'appli devient obsolète en quelques
    /// jours pour quiconque télécharge "la dernière version" comme recommandé).
    /// Le hash approuvé est mémorisé ; toute nouvelle valeur différente (y
    /// compris un retour à une version antérieure) redemande une approbation
    /// explicite plutôt que de faire confiance silencieusement.
    /// Même pattern JSON que SettingsManager/FavoritesManager.
    /// </summary>
    public static class TrustedBinaryStore
    {
        private static string StoreFile => Path.Combine(AppConfig.AppDir, "trusted-binaries.json");

        private static Dictionary<string, TrustedBinaryEntry> Load()
        {
            if (!File.Exists(StoreFile)) return new Dictionary<string, TrustedBinaryEntry>();

            try
            {
                var raw = File.ReadAllText(StoreFile);
                if (string.IsNullOrWhiteSpace(raw)) return new Dictionary<string, TrustedBinaryEntry>();
                return JsonSerializer.Deserialize<Dictionary<string, TrustedBinaryEntry>>(raw)
                    ?? new Dictionary<string, TrustedBinaryEntry>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Fichier de binaires approuvés illisible, ignoré : {ex.Message}", LogLevel.WARN);
                return new Dictionary<string, TrustedBinaryEntry>();
            }
        }

        private static void Save(Dictionary<string, TrustedBinaryEntry> trusted)
        {
            try
            {
                File.WriteAllText(StoreFile, JsonSerializer.Serialize(trusted));
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la sauvegarde des binaires approuvés : {ex.Message}", LogLevel.ERROR);
            }
        }

        /// <summary>Hash précédemment approuvé pour ce binaire, ou null.</summary>
        public static string? GetTrustedHash(string binaryKey)
        {
            var trusted = Load();
            return trusted.TryGetValue(binaryKey, out var entry) ? entry.Sha256 : null;
        }

        public static void Trust(string binaryKey, string sha256)
        {
            var trusted = Load();
            trusted[binaryKey] = new TrustedBinaryEntry { Sha256 = sha256, TrustedAtUtc = DateTime.UtcNow };
            Save(trusted);
        }
    }
}
