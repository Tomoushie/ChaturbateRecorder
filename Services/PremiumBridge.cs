using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using ChaturbateRecorderApp.Config;
using SentinelGuard;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Membres du composant premium, résolus une fois par réflexion.
    ///
    /// **Pourquoi de la réflexion plutôt qu'une interface partagée** (97.0 /
    /// premium) : une interface obligerait le composant fermé à référencer
    /// l'assembly de l'application, or la variante portable est publiée en
    /// `PublishSingleFile` auto-contenue — l'assembly y est embarquée, et le
    /// liage d'un greffon externe sur une identité d'assembly embarquée est
    /// exactement le genre de piège qui ne se voit qu'à l'exécution, chez
    /// l'utilisateur, sur la seule variante qu'on teste le moins.
    ///
    /// Le prix de ce choix est l'absence de contrôle à la compilation. Il est
    /// payé en gardant la surface MINUSCULE et en types primitifs uniquement,
    /// et en isolant le liage ici, où il se teste sans DLL réelle.
    /// </summary>
    internal sealed class PremiumBinding
    {
        internal const string AssemblyFileName = "StreamRecorderPro.dll";
        internal const string TypeName = "StreamRecorderPro.PremiumModule";

        /// <summary>
        /// Même nom que <c>StreamRecorderPro.Licence.FileName</c>. Dupliqué
        /// volontairement : l'application libre ne référence PAS l'assembly
        /// payante (voir le commentaire de classe), et cette constante ne sert
        /// qu'à répondre à « y a-t-il un fichier de licence à côté de
        /// l'exécutable ? » pour l'affichage. Les deux doivent rester
        /// synchronisés à la main, comme <c>machine-id.txt</c>.
        /// </summary>
        internal const string LicenceFileName = "licence.key";

        private readonly object _instance;
        private readonly PropertyInfo _version;
        private readonly PropertyInfo _licensedTo;
        private readonly PropertyInfo _licenceProblem;
        private readonly PropertyInfo _usageSummary;
        private readonly MethodInfo _capturePreview;
        private readonly MethodInfo _liveWindow;

        // Membres OPTIONNELS (v2.3). Une application à jour doit continuer de
        // fonctionner avec une DLL plus ancienne : si l'un de ces membres
        // manque, le composant reste utilisable pour le reste, et seule la
        // fonctionnalité correspondante est indisponible. Les rendre OBLIGATOIRES
        // casserait le premium de tous les acheteurs qui n'ont pas encore la
        // nouvelle DLL.
        private readonly MethodInfo? _capturePreviewWithSession;
        private readonly MethodInfo? _liveWindowEx;
        private readonly MethodInfo? _multiLive;
        private readonly MethodInfo? _saveReplayClip;
        private readonly MethodInfo? _exportMarkerClips;

        private PremiumBinding(object instance, PropertyInfo version, PropertyInfo licensedTo,
                               PropertyInfo licenceProblem, PropertyInfo usageSummary,
                               MethodInfo capturePreview, MethodInfo liveWindow,
                               MethodInfo? capturePreviewWithSession, MethodInfo? liveWindowEx,
                               MethodInfo? multiLive, MethodInfo? saveReplayClip, MethodInfo? exportMarkerClips)
        {
            _instance = instance;
            _version = version;
            _licensedTo = licensedTo;
            _licenceProblem = licenceProblem;
            _usageSummary = usageSummary;
            _capturePreview = capturePreview;
            _liveWindow = liveWindow;
            _capturePreviewWithSession = capturePreviewWithSession;
            _liveWindowEx = liveWindowEx;
            _multiLive = multiLive;
            _saveReplayClip = saveReplayClip;
            _exportMarkerClips = exportMarkerClips;
        }

        /// <summary>
        /// Vérifie qu'un type honore le contrat, et l'instancie.
        ///
        /// **Fonction pure sur un `Type`**, donc éprouvable avec des types
        /// écrits dans le projet de tests — pas besoin de fabriquer une DLL.
        /// Retourne null ET un motif lisible : un composant refusé en silence
        /// donnerait « le premium ne marche pas » sans rien à examiner.
        /// </summary>
        internal static PremiumBinding? Bind(Type type, out string probleme)
        {
            probleme = "";

            static PropertyInfo? Chaine(Type t, string nom) =>
                t.GetProperty(nom, BindingFlags.Public | BindingFlags.Instance) is { } p
                && p.PropertyType == typeof(string) && p.CanRead ? p : null;

            var version = Chaine(type, "Version");
            if (version == null) { probleme = "propriété string Version absente"; return null; }

            var licensedTo = Chaine(type, "LicensedTo");
            if (licensedTo == null) { probleme = "propriété string LicensedTo absente"; return null; }

            var licenceProblem = Chaine(type, "LicenceProblem");
            if (licenceProblem == null) { probleme = "propriété string LicenceProblem absente"; return null; }

            var usageSummary = Chaine(type, "UsageSummary");
            if (usageSummary == null) { probleme = "propriété string UsageSummary absente"; return null; }

            var capture = type.GetMethod("TryCapturePreview", BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(int) },
                null);
            if (capture == null || capture.ReturnType != typeof(bool))
            {
                probleme = "méthode bool TryCapturePreview(string, string, string, string, int) absente";
                return null;
            }

            var liveWindow = type.GetMethod("TryShowLiveWindow", BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(int) },
                null);
            if (liveWindow == null || liveWindow.ReturnType != typeof(bool))
            {
                probleme = "méthode bool TryShowLiveWindow(string, string, string, string, int) absente";
                return null;
            }

            // OPTIONNELS : absents d'une DLL antérieure à la v2.3. Une
            // signature incorrecte est traitée comme absente — une nouvelle
            // version du composant doit pouvoir évoluer sans rendre
            // l'application inutilisable.
            static MethodInfo? Optionnelle(Type t, string nom, Type[] arguments, Type retour)
            {
                var m = t.GetMethod(nom, BindingFlags.Public | BindingFlags.Instance, null, arguments, null);
                return m != null && m.ReturnType == retour ? m : null;
            }

            var captureSession = Optionnelle(type, "TryCapturePreviewWithSession",
                new[]
                {
                    typeof(string), typeof(string), typeof(string), typeof(string),
                    typeof(string), typeof(string), typeof(int)
                },
                typeof(bool));

            var liveEx = Optionnelle(type, "TryShowLiveWindowEx",
                new[]
                {
                    typeof(string), typeof(string), typeof(string), typeof(string),
                    typeof(bool), typeof(bool), typeof(int)
                },
                typeof(bool));

            var multiLive = Optionnelle(type, "TryShowMultiLive",
                new[]
                {
                    typeof(string), typeof(string), typeof(string), typeof(string),
                    typeof(string), typeof(bool), typeof(int)
                },
                typeof(bool));

            var replay = Optionnelle(type, "TrySaveReplayClip",
                new[] { typeof(string), typeof(string), typeof(string), typeof(int) },
                typeof(bool));

            var markers = Optionnelle(type, "ExportMarkerClips",
                new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(int) },
                typeof(string));

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                probleme = "constructeur sans argument absent";
                return null;
            }

            object? instance;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                // TargetInvocationException enveloppe la vraie cause : c'est
                // elle qu'il faut journaliser, l'enveloppe ne dit rien.
                probleme = $"construction impossible : {(ex.InnerException ?? ex).Message}";
                return null;
            }

            if (instance == null) { probleme = "construction impossible : instance nulle"; return null; }

            return new PremiumBinding(instance, version, licensedTo, licenceProblem, usageSummary,
                                      capture, liveWindow, captureSession, liveEx,
                                      multiLive, replay, markers);
        }

        internal string Version => Lire(_version);
        internal string LicensedTo => Lire(_licensedTo);
        internal string LicenceProblem => Lire(_licenceProblem);
        internal string UsageSummary => Lire(_usageSummary);

        private string Lire(PropertyInfo p)
        {
            try { return p.GetValue(_instance) as string ?? ""; }
            catch (Exception ex) { Logger.Log($"Premium : lecture de {p.Name} impossible — {(ex.InnerException ?? ex).Message}", LogLevel.WARN); return ""; }
        }

        internal bool TryCapturePreview(string ytDlpPath, string ffmpegPath, string roomUrl, string destinationJpg, int timeoutSeconds)
        {
            try
            {
                return _capturePreview.Invoke(_instance,
                    new object[] { ytDlpPath, ffmpegPath, roomUrl, destinationJpg, timeoutSeconds }) is true;
            }
            catch (Exception ex)
            {
                // Un composant tiers ne doit jamais pouvoir faire tomber
                // l'application : il est OPTIONNEL, son échec est un
                // non-évènement pour qui n'a pas acheté.
                Logger.Log($"Premium : aperçu impossible — {(ex.InnerException ?? ex).Message}", LogLevel.WARN);
                return false;
            }
        }

        internal bool TryShowLiveWindow(string ytDlpPath, string ffmpegPath, string roomUrl, string titre, int timeoutSeconds)
        {
            try
            {
                return _liveWindow.Invoke(_instance,
                    new object[] { ytDlpPath, ffmpegPath, roomUrl, titre, timeoutSeconds }) is true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Premium : vidéo en direct impossible — {(ex.InnerException ?? ex).Message}", LogLevel.WARN);
                return false;
            }
        }

        /// <summary>
        /// v2.3 — null si la DLL chargée est antérieure à la v2.3 : l'appelant
        /// obtient alors simplement « non disponible », sans perdre le reste du
        /// premium.
        /// </summary>
        internal bool TryCapturePreviewWithSession(string ytDlpPath, string ffmpegPath, string roomUrl,
                                                   string destinationJpg, string cookiesPath, string proxyUrl,
                                                   int timeoutSeconds)
        {
            // COMPATIBILITÉ : une DLL antérieure à la v2.3 ne connaît pas la
            // session. On retombe alors sur l'ancien aperçu SANS cookies plutôt
            // que de priver l'acheteur de TOUTES ses vignettes.
            if (_capturePreviewWithSession == null)
            {
                try
                {
                    return _capturePreview.Invoke(_instance,
                        new object[] { ytDlpPath, ffmpegPath, roomUrl, destinationJpg, timeoutSeconds }) is true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Premium : aperçu (repli DLL antérieure) impossible — {(ex.InnerException ?? ex).Message}", LogLevel.WARN);
                    return false;
                }
            }

            try
            {
                return _capturePreviewWithSession.Invoke(_instance,
                    new object[] { ytDlpPath, ffmpegPath, roomUrl, destinationJpg,
                                   cookiesPath ?? "", proxyUrl ?? "", timeoutSeconds }) is true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Premium : aperçu avec session impossible — {(ex.InnerException ?? ex).Message}", LogLevel.WARN);
                return false;
            }
        }

        internal bool TryShowLiveWindowEx(string ytDlpPath, string ffmpegPath, string roomUrl, string titre,
                                          bool compact, bool topmost, int timeoutSeconds)
        {
            if (_liveWindowEx == null) return false;
            try
            {
                return _liveWindowEx.Invoke(_instance,
                    new object[] { ytDlpPath, ffmpegPath, roomUrl, titre, compact, topmost, timeoutSeconds }) is true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Premium : direct PiP impossible — {(ex.InnerException ?? ex).Message}", LogLevel.WARN);
                return false;
            }
        }

        internal bool TryShowMultiLive(string ytDlpPath, string ffmpegPath, string jsonRooms,
                                       string cookiesPath, string proxyUrl, bool topmost, int timeoutSeconds)
        {
            if (_multiLive == null) return false;
            try
            {
                return _multiLive.Invoke(_instance,
                    new object[] { ytDlpPath, ffmpegPath, jsonRooms, cookiesPath, proxyUrl, topmost, timeoutSeconds }) is true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Premium : multi-live impossible — {(ex.InnerException ?? ex).Message}", LogLevel.WARN);
                return false;
            }
        }

        internal bool TrySaveReplayClip(string ffmpegPath, string captureFilePath, string destinationPath, int seconds)
        {
            if (_saveReplayClip == null) return false;
            try
            {
                return _saveReplayClip.Invoke(_instance,
                    new object[] { ffmpegPath, captureFilePath, destinationPath, seconds }) is true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Premium : rétro-clip impossible — {(ex.InnerException ?? ex).Message}", LogLevel.WARN);
                return false;
            }
        }

        internal string ExportMarkerClips(string ffmpegPath, string captureFilePath, string markersJson,
                                          string outputDirectory, int paddingSeconds)
        {
            if (_exportMarkerClips == null) return "";
            try
            {
                return _exportMarkerClips.Invoke(_instance,
                    new object[] { ffmpegPath, captureFilePath, markersJson, outputDirectory, paddingSeconds })
                    as string ?? "";
            }
            catch (Exception ex)
            {
                Logger.Log($"Premium : clips de marqueurs impossibles — {(ex.InnerException ?? ex).Message}", LogLevel.WARN);
                return "";
            }
        }
    }

    /// <summary>
    /// Passerelle vers « Stream Recorder Pro », le composant fermé et payant
    /// (97.0). Chargé depuis le dossier de l'application s'il est présent.
    ///
    /// **L'application libre ne vérifie AUCUNE licence.** Elle demande au
    /// composant s'il est en règle et le croit. Mettre le contrôle ici
    /// reviendrait à en publier le code source sous licence MIT, c'est-à-dire à
    /// livrer le mode d'emploi du contournement avec le produit. Le contrôle
    /// vit dans le composant, avec sa clé publique.
    ///
    /// **Son absence est le cas NORMAL**, pas une erreur : la très grande
    /// majorité des utilisateurs ne l'auront jamais. Rien de ce qui suit ne
    /// doit produire de dialogue, ni empêcher l'application de démarrer.
    /// </summary>
    public sealed class PremiumBridge
    {
        private PremiumBinding? _module;

        /// <summary>
        /// Levé après un <see cref="Reload"/> qui a changé l'état de licence
        /// (acceptée, refusée, ou nom de l'acheteur différent).
        ///
        /// **Pourquoi un événement** : installer une licence depuis la fenêtre
        /// Premium ne doit pas demander de redémarrer l'application. La fenêtre
        /// principale s'y abonne pour retirer le bouton « Passer à Premium » et
        /// sa bannière ; sans ça, l'acheteur verrait encore « acheter » alors
        /// que le produit est déjà actif.
        /// </summary>
        public event Action? LicenceChanged;

        /// <summary>Le composant est présent ET honore le contrat.</summary>
        public bool IsLoaded => _module != null;

        /// <summary>
        /// Un fichier <c>licence.key</c> existe-t-il à côté de l'exécutable ?
        ///
        /// Sert UNIQUEMENT à distinguer « jamais activé » (cas normal d'un
        /// utilisateur de la version gratuite : on ne l'importune pas) de
        /// « licence présente mais refusée » (quelqu'un a payé et croit être
        /// activé : il faut le lui dire). Ne prouve rien sur la validité.
        /// </summary>
        public bool LicenceFilePresent =>
            File.Exists(Path.Combine(AppConfig.AppDir, PremiumBinding.LicenceFileName));

        /// <summary>
        /// Vrai quand le refus vient d'une licence AUTHENTIQUE liée à une autre
        /// installation — le cas d'un changement d'ordinateur, qui se règle par
        /// un transfert gratuit et non par un nouvel achat.
        ///
        /// **Comparaison sur le texte, faute de mieux** : l'application libre
        /// ne référence pas l'assembly payante, donc elle ne peut pas comparer
        /// à <c>Licence.MessageDesaccordMachine</c>. La chaîne « autre
        /// installation » est stable depuis la 2.2.0 ; si elle change côté
        /// premium, ce test silencieux retombera simplement sur le message
        /// générique — jamais sur un faux positif alarmant.
        /// </summary>
        public bool IsMachineMismatch =>
            LicenceProblem.Contains("autre installation", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Nom de l'acheteur, tel qu'il est SIGNÉ dans sa licence. Vide si le
        /// composant est absent ou sa licence refusée.
        ///
        /// Il est affiché dans la fenêtre « Remerciements », et c'est la seule
        /// mesure anti-partage retenue : une clé qui circule dit à qui elle
        /// appartient. Lier la licence à la machine punirait surtout les
        /// honnêtes, qui changent d'ordinateur.
        /// </summary>
        public string LicensedTo => _module?.LicensedTo ?? "";

        /// <summary>Vrai si le composant est là ET sa licence acceptée.</summary>
        public bool IsLicensed => _module != null && LicensedTo.Length > 0;

        /// <summary>Motif lisible d'un refus de licence, pour le panneau Diagnostic.</summary>
        public string LicenceProblem { get; private set; } = "";

        /// <summary>Version du composant, pour le panneau Diagnostic.</summary>
        public string Version => _module?.Version ?? "";

        /// <summary>
        /// Compteurs d'usage locaux par fonctionnalité, pour le panneau
        /// Diagnostic. Vide si le composant est absent — pas de « 0 » pour
        /// chaque fonctionnalité, qui laisserait croire à une mesure faite.
        /// </summary>
        public string UsageSummary => _module?.UsageSummary ?? "";

        /// <summary>
        /// Cherche et charge le composant. Appelé une fois au démarrage.
        ///
        /// **L'empreinte SHA-256 est journalisée à chaque chargement.** Cette
        /// application vérifie déjà le hash de yt-dlp et de ffmpeg avant de les
        /// lancer (SentinelGuard) ; charger du CODE sans laisser la moindre
        /// trace de ce qui a été chargé serait le seul angle mort de la chaîne.
        /// Ça ne remplace pas une signature — quiconque peut écrire dans ce
        /// dossier peut déjà remplacer l'exécutable — mais un journal permet de
        /// répondre à « qu'est-ce qui tournait sur cette machine ? ».
        /// </summary>
        public void Load() => Charger(rechargement: false);

        /// <summary>
        /// Relit <c>licence.key</c> et reconstruit le composant, SANS redémarrer
        /// l'application. Appelé après une installation de licence depuis la
        /// fenêtre Premium.
        ///
        /// Reconstruire est nécessaire, pas paresseux : le composant vérifie sa
        /// licence dans son constructeur (voir <c>PremiumModule</c>). Une simple
        /// relecture de propriété ne verrait jamais le nouveau fichier.
        ///
        /// **L'événement n'est levé que si l'état a changé.** L'appelant de
        /// l'installation teste <see cref="IsLicensed"/> de toute façon ; lever
        /// pour rien ferait repeindre la fenêtre principale à chaque tentative
        /// refusée.
        /// </summary>
        public void Reload()
        {
            var etaitLicencie = IsLicensed;
            var nomAvant = LicensedTo;

            Charger(rechargement: true);

            if (etaitLicencie == IsLicensed &&
                string.Equals(nomAvant, LicensedTo, StringComparison.Ordinal)) return;

            Logger.Log(IsLicensed
                ? $"Premium : licence installée à chaud, au nom de {LicensedTo}."
                : $"Premium : licence rechargée mais refusée — {LicenceProblem}.", LogLevel.INFO);

            LicenceChanged?.Invoke();
        }

        private void Charger(bool rechargement)
        {
            var chemin = Path.Combine(AppConfig.AppDir, PremiumBinding.AssemblyFileName);
            if (!File.Exists(chemin))
            {
                // Au démarrage, l'absence est le cas NORMAL : on ne touche à
                // rien. En rechargement, c'est un composant qui a disparu
                // pendant la session — l'état doit suivre le disque.
                if (rechargement)
                {
                    _module = null;
                    LicenceProblem = $"composant {PremiumBinding.AssemblyFileName} absent";
                }
                return;
            }

            try
            {
                if (!rechargement)
                    Logger.Log($"Premium : composant trouvé, SHA-256 {Empreinte(chemin)}", LogLevel.INFO);

                var assembly = Assembly.LoadFrom(chemin);
                var type = assembly.GetType(PremiumBinding.TypeName, throwOnError: false);
                if (type == null)
                {
                    _module = null;
                    LicenceProblem = $"type {PremiumBinding.TypeName} introuvable";
                    Logger.Log($"Premium : {LicenceProblem}.", LogLevel.WARN);
                    return;
                }

                var lie = PremiumBinding.Bind(type, out var probleme);
                if (lie == null)
                {
                    _module = null;
                    LicenceProblem = probleme;
                    Logger.Log($"Premium : composant ignoré — {probleme}.", LogLevel.WARN);
                    return;
                }

                _module = lie;
                LicenceProblem = lie.LicenceProblem;

                Logger.Log(LicensedTo.Length > 0
                    ? $"Premium : composant {lie.Version} chargé, licence au nom de {LicensedTo}."
                    : $"Premium : composant {lie.Version} chargé, SANS licence valide — {LicenceProblem}.", LogLevel.INFO);
            }
            catch (Exception ex)
            {
                // Assembly incompatible, tronquée, compilée pour une autre
                // architecture : l'application continue sans premium.
                _module = null;
                LicenceProblem = ex.Message;
                Logger.Log($"Premium : chargement impossible ({chemin}) — {ex.Message}", LogLevel.WARN);
            }
        }

        /// <summary>
        /// Demande une vignette du direct. Faux si le composant est absent, sa
        /// licence refusée, ou la capture ratée — l'appelant n'a pas à
        /// distinguer les trois, il affiche simplement la carte sans image.
        /// </summary>
        public bool TryCapturePreview(string roomUrl, string destinationJpg, int timeoutSeconds = 20)
        {
            if (_module == null || !IsLicensed) return false;
            return _module.TryCapturePreview(AppConfig.YtDlpPath, AppConfig.FFmpegPath,
                roomUrl, destinationJpg, timeoutSeconds);
        }

        /// <summary>
        /// Ouvre une fenêtre de vidéo en direct pour ce salon. Faux si le
        /// composant est absent, sa licence refusée, ou l'ouverture ratée —
        /// l'appelant n'a rien d'autre à faire dans ce cas.
        /// </summary>
        public bool TryShowLiveWindow(string roomUrl, string titre, int timeoutSeconds = 20)
        {
            if (_module == null || !IsLicensed) return false;
            return _module.TryShowLiveWindow(AppConfig.YtDlpPath, AppConfig.FFmpegPath,
                roomUrl, titre, timeoutSeconds);
        }

        /// <summary>
        /// v2.3 — aperçu résolu avec la session (cookies/proxy) : indispensable
        /// pour les salons réservés à un compte connecté. Faux si le composant
        /// est absent, sa licence refusée, ou si la DLL ne connaît pas encore
        /// ce membre (acheteur qui n'a pas mis à jour le module).
        /// </summary>
        public bool TryCapturePreviewWithSession(string roomUrl, string destinationJpg,
                                                 string? cookiesPath, string? proxyUrl,
                                                 int timeoutSeconds = 20)
        {
            if (_module == null || !IsLicensed) return false;
            return _module.TryCapturePreviewWithSession(AppConfig.YtDlpPath, AppConfig.FFmpegPath,
                roomUrl, destinationJpg, cookiesPath ?? "", proxyUrl ?? "", timeoutSeconds);
        }

        /// <summary>
        /// v2.3 — direct en fenêtre normale ou en Picture-in-Picture compacte.
        /// </summary>
        public bool TryShowLiveWindowEx(string roomUrl, string titre, bool compact, bool topmost,
                                        int timeoutSeconds = 20)
        {
            if (_module == null || !IsLicensed) return false;
            return _module.TryShowLiveWindowEx(AppConfig.YtDlpPath, AppConfig.FFmpegPath,
                roomUrl, titre, compact, topmost, timeoutSeconds);
        }

        /// <summary>v2.3 — grille multi-live (1 à 4 directs).</summary>
        public bool TryShowMultiLive(string jsonRooms, string? cookiesPath, string? proxyUrl,
                                     bool topmost, int timeoutSeconds = 20)
        {
            if (_module == null || !IsLicensed) return false;
            return _module.TryShowMultiLive(AppConfig.YtDlpPath, AppConfig.FFmpegPath, jsonRooms,
                cookiesPath ?? "", proxyUrl ?? "", topmost, timeoutSeconds);
        }

        /// <summary>
        /// v2.3 — « garder les N dernières secondes » de la capture en cours.
        /// Faux si la DLL est antérieure à la v2.3 ou si le `.part` refuse le
        /// seek (MP4 non finalisé).
        /// </summary>
        public bool TrySaveReplayClip(string captureFilePath, string destinationPath, int seconds)
        {
            if (_module == null || !IsLicensed) return false;
            return _module.TrySaveReplayClip(AppConfig.FFmpegPath, captureFilePath, destinationPath, seconds);
        }

        /// <summary>v2.3 — exporte les clips des marqueurs sur le fichier finalisé.</summary>
        public string ExportMarkerClips(string captureFilePath, string markersJson, string outputDirectory,
                                        int paddingSeconds = 15)
        {
            if (_module == null || !IsLicensed) return "";
            return _module.ExportMarkerClips(AppConfig.FFmpegPath, captureFilePath, markersJson,
                                             outputDirectory, paddingSeconds);
        }

        private static string Empreinte(string chemin)
        {
            try
            {
                using var flux = File.OpenRead(chemin);
                return Convert.ToHexString(SHA256.HashData(flux));
            }
            catch (Exception ex)
            {
                return $"illisible ({ex.Message})";
            }
        }
    }
}
