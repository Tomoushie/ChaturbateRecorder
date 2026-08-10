using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChaturbateRecorderApp.Config;
using SentinelGuard;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>Un salon connu de l'application. Persisté dans rooms.json.</summary>
    public sealed class RoomEntry
    {
        public required string Url { get; set; }

        /// <summary>
        /// Enregistrer automatiquement dès que ce salon passe en ligne.
        ///
        /// **C'est ce drapeau qui remplace l'ancienne liste de surveillance**,
        /// et il préserve la décision du mainteneur (voir WatchListManager) :
        /// un salon connu n'est PAS un salon surveillé. Sans lui, fusionner les
        /// deux listes ferait sonder le site pour chaque favori — exactement ce
        /// que la séparation d'origine cherchait à éviter.
        /// </summary>
        public bool AutoRecord { get; set; }

        public DateTime AddedUtc { get; set; }
    }

    /// <summary>
    /// État affiché par une carte de salon (97.0 étape 2). Dérivé, jamais
    /// persisté : il se recalcule à chaque changement.
    /// </summary>
    public enum RoomRowState
    {
        /// <summary>Rien en cours, et le salon ne diffuse pas.</summary>
        Idle,
        /// <summary>État inconnu : réseau coupé, yt-dlp absent. Jamais traité comme en ligne.</summary>
        Unknown,
        /// <summary>Diffuse, mais rien n'est enregistré — « Démarrer » a du sens.</summary>
        Live,
        /// <summary>Enregistrement en cours.</summary>
        Recording,
        /// <summary>Interrompu, une reconnexion automatique est programmée.</summary>
        Reconnecting,
        /// <summary>Enregistrement terminé, la carte montre encore son résultat.</summary>
        Finished,
        /// <summary>Échec du dernier enregistrement.</summary>
        Failed,
        /// <summary>La source n'existe pas — inutile de l'attendre.</summary>
        NotFound,
    }

    /// <summary>
    /// Liste unifiée des salons (97.0 étape 2), qui remplace
    /// <see cref="FavoritesManager"/> et <see cref="WatchListManager"/>.
    ///
    /// **Pourquoi fusionner deux listes que le mainteneur avait séparées** :
    /// elles décrivaient les mêmes salons sous deux angles, si bien qu'un même
    /// salon pouvait figurer dans les favoris, dans la surveillance, ET dans
    /// les enregistrements en cours — trois endroits, trois vérités possibles.
    /// La raison d'origine de la séparation (« être favori ne doit pas déclencher
    /// une surveillance ») est conservée telle quelle par
    /// <see cref="RoomEntry.AutoRecord"/>, qui est faux par défaut.
    /// </summary>
    public class RoomStore
    {
        private readonly List<RoomEntry> _rooms = new();

        public IReadOnlyList<RoomEntry> Rooms => _rooms;

        private static string StoreFile => Path.Combine(AppConfig.AppDir, "rooms.json");
        private static string LegacyFavorites => AppConfig.FavoritesFile;
        private static string LegacyWatchList => Path.Combine(AppConfig.AppDir, "watchlist.json");

        /// <summary>
        /// Compare deux adresses de salon. Hôte et schéma en minuscules, barre
        /// finale ignorée : `.../someroom/` et `.../someroom` désignent le même
        /// salon, et laisser les deux coexister donnerait deux cartes pour une
        /// seule diffusion — donc deux enregistrements simultanés du même flux.
        /// </summary>
        public static string Normalize(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            var trimmed = url.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return trimmed.TrimEnd('/').ToLowerInvariant();

            var chemin = uri.AbsolutePath.TrimEnd('/');
            return $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}{chemin}".ToLowerInvariant();
        }

        /// <summary>
        /// Construit la liste unifiée à partir des deux anciens fichiers.
        /// Séparée du disque pour être vérifiable : c'est le seul endroit où
        /// les données de quelqu'un peuvent être perdues.
        ///
        /// **`AutoRecord` n'est vrai que pour ce qui était réellement
        /// surveillé.** Un favori migré arrive désactivé : passer trente favoris
        /// en surveillance automatique parce qu'on a fusionné deux fichiers
        /// serait un changement de comportement que personne n'a demandé.
        /// </summary>
        public static List<RoomEntry> Merge(IEnumerable<string>? favoris, IEnumerable<string>? surveilles, DateTime nowUtc)
        {
            var vus = new Dictionary<string, RoomEntry>(StringComparer.OrdinalIgnoreCase);

            void Ajouter(string? url, bool auto)
            {
                var cle = Normalize(url);
                if (cle.Length == 0) return;

                if (vus.TryGetValue(cle, out var deja))
                {
                    // Présent dans les deux fichiers : la surveillance gagne.
                    // Perdre une surveillance active serait un vrai dommage,
                    // en gagner une par erreur n'en est pas un.
                    if (auto) deja.AutoRecord = true;
                    return;
                }

                vus[cle] = new RoomEntry { Url = url!.Trim(), AutoRecord = auto, AddedUtc = nowUtc };
            }

            foreach (var f in favoris ?? Enumerable.Empty<string>()) Ajouter(f, auto: false);
            foreach (var s in surveilles ?? Enumerable.Empty<string>()) Ajouter(s, auto: true);

            return vus.Values.ToList();
        }

        /// <summary>
        /// État d'une carte, dérivé de ce que sait l'application. Fonction pure
        /// et testable : c'est elle qui décide ce que voit l'utilisateur, et
        /// une erreur ici afficherait « hors ligne » sur un enregistrement en
        /// cours.
        ///
        /// **L'enregistrement prime sur l'état du salon** : tant qu'un job
        /// tourne, peu importe ce que le dernier sondage a répondu — le sondage
        /// peut avoir échoué alors que la capture, elle, reçoit des données.
        /// </summary>
        public static RoomRowState Resolve(RoomStatus statut, DownloadState? job, bool reconnexionPrevue)
        {
            if (job == DownloadState.Running) return RoomRowState.Recording;
            if (reconnexionPrevue) return RoomRowState.Reconnecting;

            if (job == DownloadState.Failed) return RoomRowState.Failed;
            if (job == DownloadState.Completed || job == DownloadState.Stopped) return RoomRowState.Finished;

            return statut switch
            {
                RoomStatus.Online => RoomRowState.Live,
                RoomStatus.NotFound => RoomRowState.NotFound,
                RoomStatus.Unknown => RoomRowState.Unknown,
                _ => RoomRowState.Idle,
            };
        }

        public void Load()
        {
            _rooms.Clear();

            if (File.Exists(StoreFile))
            {
                try
                {
                    var brut = File.ReadAllText(StoreFile);
                    if (!string.IsNullOrWhiteSpace(brut))
                    {
                        var lus = JsonSerializer.Deserialize<List<RoomEntry>>(brut) ?? new List<RoomEntry>();
                        foreach (var r in lus) AjouterSiSure(r);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Liste des salons illisible, reconstruction depuis les anciens fichiers : {ex.Message}", LogLevel.WARN);
                }
            }

            Migrer();
        }

        /// <summary>
        /// **Les deux anciens fichiers ne sont PAS supprimés.** Quelqu'un qui
        /// revient à une version antérieure doit retrouver ses favoris et sa
        /// surveillance ; les effacer rendrait la mise à jour irréversible pour
        /// un gain nul.
        /// </summary>
        private void Migrer()
        {
            var favoris = LireListe(LegacyFavorites);
            var surveilles = LireListe(LegacyWatchList);

            foreach (var r in Merge(favoris, surveilles, DateTime.UtcNow)) AjouterSiSure(r);

            if (_rooms.Count > 0)
            {
                Logger.Log($"Liste des salons construite depuis les anciens fichiers : {_rooms.Count} salon(s), " +
                           $"dont {_rooms.Count(r => r.AutoRecord)} en surveillance automatique.", LogLevel.INFO);
                Save();
            }
        }

        private static List<string> LireListe(string chemin)
        {
            if (!File.Exists(chemin)) return new List<string>();
            try
            {
                var brut = File.ReadAllText(chemin);
                if (string.IsNullOrWhiteSpace(brut)) return new List<string>();
                return JsonSerializer.Deserialize<List<string>>(brut) ?? new List<string>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Ancien fichier illisible, ignoré ({chemin}) : {ex.Message}", LogLevel.WARN);
                return new List<string>();
            }
        }

        /// <summary>
        /// Revalide l'adresse, comme le faisait FavoritesManager : un fichier
        /// modifié à la main ne doit pas pouvoir réintroduire une URL refusée
        /// par le bac à sable.
        /// </summary>
        private void AjouterSiSure(RoomEntry entree)
        {
            if (string.IsNullOrWhiteSpace(entree.Url)) return;

            if (!UrlValidator.IsSafeUrl(entree.Url, AppConfig.Whitelist, AppConfig.Blacklist, out var motif))
            {
                Logger.Log($"Salon ignoré ({entree.Url}) : {motif}", LogLevel.WARN);
                return;
            }

            if (Find(entree.Url) != null) return;
            _rooms.Add(entree);
        }

        public RoomEntry? Find(string url)
        {
            var cle = Normalize(url);
            return _rooms.FirstOrDefault(r => string.Equals(Normalize(r.Url), cle, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Ajoute si absent. Retourne false si le salon est déjà connu.</summary>
        public bool Add(string url, bool autoRecord = false)
        {
            if (Find(url) != null) return false;
            AjouterSiSure(new RoomEntry { Url = url.Trim(), AutoRecord = autoRecord, AddedUtc = DateTime.UtcNow });
            Save();
            return true;
        }

        public bool Remove(string url)
        {
            var entree = Find(url);
            if (entree == null) return false;
            _rooms.Remove(entree);
            Save();
            return true;
        }

        public bool SetAutoRecord(string url, bool auto)
        {
            var entree = Find(url);
            if (entree == null || entree.AutoRecord == auto) return false;
            entree.AutoRecord = auto;
            Save();
            return true;
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never };
                File.WriteAllText(StoreFile, JsonSerializer.Serialize(_rooms, options));
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur d'enregistrement de la liste des salons : {ex.Message}", LogLevel.ERROR);
            }
        }
    }
}
