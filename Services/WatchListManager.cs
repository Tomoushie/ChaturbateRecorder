using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Liste des salons surveillés (88.0 / 4.3). Même patron que
    /// FavoritesManager : un JSON à côté de l'exe, chargé au démarrage.
    ///
    /// **Liste volontairement distincte des favoris** (choix du mainteneur) :
    /// un favori est un raccourci de saisie, un salon surveillé engage
    /// l'application à interroger le site en boucle et à démarrer un
    /// enregistrement sans supervision. Mélanger les deux ferait surveiller
    /// des dizaines de salons par le simple fait de les avoir mis en favori.
    ///
    /// **Y figurer suffit à être surveillé** : pas de case « actif » par
    /// entrée. Ajouter, c'est demander la surveillance ; ne plus la vouloir,
    /// c'est retirer.
    /// </summary>
    public class WatchListManager
    {
        public List<string> Rooms { get; private set; } = new();

        private static string WatchFile => Path.Combine(AppConfig.AppDir, "watchlist.json");

        public void Load()
        {
            if (!File.Exists(WatchFile)) return;

            try
            {
                var raw = File.ReadAllText(WatchFile);
                if (string.IsNullOrWhiteSpace(raw)) return;
                Rooms = JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Liste de surveillance illisible, ignorée : {ex.Message}", LogLevel.WARN);
                Rooms = new List<string>();
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(WatchFile, JsonSerializer.Serialize(Rooms));
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur d'enregistrement de la liste de surveillance : {ex.Message}", LogLevel.ERROR);
            }
        }

        /// <summary>Ajoute si absent. Retourne false si déjà surveillé.</summary>
        public bool Add(string url)
        {
            var trimmed = (url ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) return false;
            if (Rooms.Any(r => string.Equals(r, trimmed, StringComparison.OrdinalIgnoreCase))) return false;
            Rooms.Add(trimmed);
            return true;
        }

        public void Remove(string url)
            => Rooms.RemoveAll(r => string.Equals(r, url, StringComparison.OrdinalIgnoreCase));
    }
}
