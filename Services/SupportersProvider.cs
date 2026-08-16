using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>D'où vient la liste finalement affichée.</summary>
    public enum SupportersOrigin
    {
        /// <summary>Le site n'a pas répondu : seule la liste embarquée est affichée.</summary>
        EmbeddedOnly,
        /// <summary>Le site a répondu : liste embarquée fusionnée avec la sienne.</summary>
        Refreshed,
    }

    public sealed class SupportersList
    {
        public required IReadOnlyList<string> Names { get; init; }
        public required SupportersOrigin Origin { get; init; }
    }

    /// <summary>
    /// Construit la liste des donateurs affichée par <c>UI/SupportersForm</c>
    /// (104.0) : liste embarquée (<see cref="Supporters.Embedded"/>) fusionnée
    /// avec <c>supporters.json</c> servi par le site du projet.
    ///
    /// **Tout ce qui vient du réseau est du texte que l'application va
    /// afficher** — donc assaini avant de l'être. Le risque n'est pas
    /// l'exécution de code (WinForms rend du texte brut, pas du HTML) mais
    /// l'affichage trompeur : une marque de direction U+202E inverse le rendu
    /// des caractères qui la suivent, un saut de ligne fabrique de fausses
    /// entrées, et une liste sans borne ferait grossir la fenêtre sans fin.
    /// <see cref="Clean(string?)"/> traite les trois.
    ///
    /// **Aucune requête au démarrage** : le chargement n'a lieu qu'à
    /// l'ouverture de la fenêtre. Une application qui contacte un serveur sans
    /// que l'utilisateur l'ait demandé révèle son IP à chaque lancement, ce qui
    /// n'irait pas avec le reste du logiciel.
    /// </summary>
    public static class SupportersProvider
    {
        /// <summary>Au-delà, la liste n'est plus une liste de remerciements.</summary>
        public const int MaxNames = 500;

        /// <summary>
        /// Un nom plus long est tronqué, pas rejeté : couper un pseudo reste
        /// préférable à effacer quelqu'un de la liste. Au-delà de cette
        /// longueur il ne s'agit de toute façon plus d'un pseudonyme.
        /// </summary>
        public const int MaxNameLength = 40;

        /// <summary>
        /// Le fichier attendu fait quelques kilo-octets. La borne existe pour
        /// que la lecture s'arrête d'elle-même si l'URL sert autre chose.
        /// </summary>
        public const int MaxResponseBytes = 64 * 1024;

        private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(6);

        /// <summary>
        /// Assainit un nom. Renvoie <c>null</c> si rien d'affichable n'en
        /// reste. Les catégories Unicode Cc (contrôle), Cf (formatage, dont
        /// les marques de direction) et Co (usage privé) sont SUPPRIMÉES, les
        /// espaces sous toutes leurs formes repliés en un seul.
        /// </summary>
        public static string? Clean(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;

            var sb = new StringBuilder(Math.Min(raw.Length, MaxNameLength));
            var runeCount = 0;
            var pendingSpace = false;

            // Parcours par rune et non par char : un pseudo peut contenir un
            // emoji, donc une paire de substituts qu'il ne faut ni couper en
            // deux ni prendre pour un caractère de catégorie Surrogate.
            foreach (var rune in raw.EnumerateRunes())
            {
                // U+FFFD n'apparaît ici que produit par EnumerateRunes face à
                // une séquence UTF-16 invalide : ce n'est pas du contenu.
                if (rune.Value == 0xFFFD) continue;

                // Les espaces AVANT les catégories : une tabulation et un saut
                // de ligne sont des caractères de contrôle, mais les supprimer
                // purement et simplement souderait les mots ("Jean\nDupont"
                // deviendrait "JeanDupont"). Ils valent donc un espace, et
                // c'est le repliement qui les rend inoffensifs.
                if (Rune.IsWhiteSpace(rune))
                {
                    // Espace en attente plutôt qu'ajouté tout de suite : évite
                    // à la fois les espaces de tête et les doublons.
                    pendingSpace = sb.Length > 0;
                    continue;
                }

                var category = Rune.GetUnicodeCategory(rune);
                if (category is UnicodeCategory.Control
                             or UnicodeCategory.Format
                             or UnicodeCategory.PrivateUse
                             or UnicodeCategory.OtherNotAssigned)
                    continue;

                if (pendingSpace)
                {
                    if (runeCount + 1 >= MaxNameLength) break;
                    sb.Append(' ');
                    runeCount++;
                    pendingSpace = false;
                }

                sb.Append(rune);
                if (++runeCount >= MaxNameLength) break;
            }

            return sb.Length == 0 ? null : sb.ToString();
        }

        /// <summary>
        /// Assainit une suite de noms, retire les vides et les doublons (sans
        /// tenir compte de la casse), trie et borne à <see cref="MaxNames"/>.
        ///
        /// **Le tri alphabétique n'est pas cosmétique** : l'ordre d'un fichier
        /// écrit à la main finit par refléter la chronologie ou les montants.
        /// Trier supprime la question.
        /// </summary>
        public static IReadOnlyList<string> Clean(IEnumerable<string?>? raw)
        {
            if (raw is null) return Array.Empty<string>();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new List<string>();

            foreach (var candidate in raw)
            {
                var name = Clean(candidate);
                if (name is null || !seen.Add(name)) continue;
                names.Add(name);
                if (names.Count >= MaxNames) break;
            }

            names.Sort(StringComparer.InvariantCultureIgnoreCase);
            return names;
        }

        /// <summary>
        /// Liste embarquée seule, affichable immédiatement — la fenêtre s'ouvre
        /// dessus avant même de tenter le réseau.
        /// </summary>
        public static SupportersList FromEmbedded() => new()
        {
            Names = Clean(Supporters.Embedded),
            Origin = SupportersOrigin.EmbeddedOnly,
        };

        /// <summary>
        /// Liste embarquée fusionnée avec celle du site. **Ne lève jamais** :
        /// une panne réseau doit dégrader l'affichage, pas ouvrir une boîte
        /// d'erreur au milieu d'un écran de remerciements.
        /// </summary>
        public static async Task<SupportersList> LoadAsync()
        {
            try
            {
                var remote = await FetchAsync().ConfigureAwait(false);
                if (remote is null) return FromEmbedded();

                return new SupportersList
                {
                    Names = Clean(Supporters.Embedded.Concat(remote)),
                    Origin = SupportersOrigin.Refreshed,
                };
            }
            catch (Exception ex)
            {
                Logger.Log($"Liste des donateurs : le site n'a pas répondu ({ex.Message}). Affichage de la liste embarquée.", LogLevel.WARN);
                return FromEmbedded();
            }
        }

        /// <summary>
        /// Extrait les noms du JSON du site. Deux formes acceptées : un objet
        /// <c>{"supporters": [...]}</c> (celle du dépôt, qui laisse la place à
        /// d'autres champs) ou un tableau nu. Renvoie <c>null</c> si le
        /// document n'est ni l'un ni l'autre — un fichier inattendu ne doit pas
        /// effacer la liste embarquée.
        /// </summary>
        public static IReadOnlyList<string>? ParseJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement array;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    array = root;
                }
                else if (root.ValueKind == JsonValueKind.Object
                         && root.TryGetProperty("supporters", out var prop)
                         && prop.ValueKind == JsonValueKind.Array)
                {
                    array = prop;
                }
                else
                {
                    return null;
                }

                var names = new List<string>();
                foreach (var item in array.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        names.Add(item.GetString() ?? "");
                }
                return names;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static async Task<IReadOnlyList<string>?> FetchAsync()
        {
            var url = AppConfig.SupportersUrl;

            // HTTPS exigé explicitement : la liste est du texte affiché tel
            // quel, et l'URL est une constante du dépôt — la vérifier ici coûte
            // une ligne et rend l'intention impossible à défaire par mégarde.
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"Liste des donateurs : URL non HTTPS ignorée ({url}).", LogLevel.WARN);
                return null;
            }

            using var http = new HttpClient { Timeout = FetchTimeout };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ChaturbateRecorder-Supporters");

            using var response = await http
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Log($"Liste des donateurs : réponse HTTP {(int)response.StatusCode} du site.", LogLevel.WARN);
                return null;
            }

            if (response.Content.Headers.ContentLength > MaxResponseBytes) return null;

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            // Un octet de plus que la borne : si le tampon est rempli, c'est
            // que la réponse la dépasse, et la lecture s'arrête là plutôt que
            // de suivre un flux sans fin.
            var buffer = new byte[MaxResponseBytes + 1];
            var read = await stream
                .ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false)
                .ConfigureAwait(false);

            if (read > MaxResponseBytes)
            {
                Logger.Log("Liste des donateurs : réponse du site trop volumineuse, ignorée.", LogLevel.WARN);
                return null;
            }

            var offset = 0;
            // JsonDocument refuse un BOM UTF-8, que GitHub Pages peut servir si
            // le fichier en porte un. Le retirer ici évite un échec de parsing
            // qui n'aurait rien à voir avec le contenu.
            if (read >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF) offset = 3;

            var json = Encoding.UTF8.GetString(buffer, offset, read - offset);
            return ParseJson(json);
        }
    }
}
