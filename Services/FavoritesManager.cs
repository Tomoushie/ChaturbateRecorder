using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Security;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Gestion persistante des favoris (JSON). Chaque favori est revalidé au
    /// chargement via UrlValidator, pour qu'un fichier modifié à la main ne
    /// puisse pas réintroduire une URL dangereuse.
    /// Équivalent de Save-Favorites / Load-Favorites / Add-FavoriteUrl /
    /// Remove-FavoriteUrl côté PowerShell.
    /// </summary>
    public class FavoritesManager
    {
        public List<string> Favorites { get; private set; } = new();

        public void Load()
        {
            Favorites = new List<string>();
            if (!File.Exists(AppConfig.FavoritesFile)) return;

            try
            {
                var raw = File.ReadAllText(AppConfig.FavoritesFile);
                if (string.IsNullOrWhiteSpace(raw)) return;

                var items = JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item) &&
                        UrlValidator.IsSafeUrl(item, AppConfig.Whitelist, AppConfig.Blacklist))
                    {
                        Favorites.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Fichier de favoris illisible, ignoré : {ex.Message}", LogLevel.WARN);
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Favorites);
                File.WriteAllText(AppConfig.FavoritesFile, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la sauvegarde des favoris : {ex.Message}", LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Ajoute une URL aux favoris. L'URL est toujours validée puis parsée
        /// via `new Uri(...)` AVANT tout accès à .Host — le bug historique de
        /// la version PowerShell (crash sur $url.Host appelé sur une string
        /// brute) n'a pas d'équivalent possible ici : le typage C# empêche
        /// structurellement d'accéder à .Host sur autre chose qu'un vrai Uri.
        /// </summary>
        public bool AddFavorite(string url)
        {
            if (!UrlValidator.IsSafeUrl(url, AppConfig.Whitelist, AppConfig.Blacklist))
                return false;

            var uri = new Uri(url);
            Logger.Log($"Ajout favori pour l'hôte : {uri.Host}");

            if (Favorites.Contains(url)) return false;

            Favorites.Add(url);
            Save();
            return true;
        }

        public void RemoveFavorite(string url)
        {
            Favorites.RemoveAll(f => f == url);
            Save();
        }
    }
}
