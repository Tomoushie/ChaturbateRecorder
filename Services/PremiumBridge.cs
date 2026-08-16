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

        private readonly object _instance;
        private readonly PropertyInfo _version;
        private readonly PropertyInfo _licensedTo;
        private readonly PropertyInfo _licenceProblem;
        private readonly MethodInfo _capturePreview;

        private PremiumBinding(object instance, PropertyInfo version, PropertyInfo licensedTo,
                               PropertyInfo licenceProblem, MethodInfo capturePreview)
        {
            _instance = instance;
            _version = version;
            _licensedTo = licensedTo;
            _licenceProblem = licenceProblem;
            _capturePreview = capturePreview;
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

            var capture = type.GetMethod("TryCapturePreview", BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(int) },
                null);
            if (capture == null || capture.ReturnType != typeof(bool))
            {
                probleme = "méthode bool TryCapturePreview(string, string, string, string, int) absente";
                return null;
            }

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

            return new PremiumBinding(instance, version, licensedTo, licenceProblem, capture);
        }

        internal string Version => Lire(_version);
        internal string LicensedTo => Lire(_licensedTo);
        internal string LicenceProblem => Lire(_licenceProblem);

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

        /// <summary>Le composant est présent ET honore le contrat.</summary>
        public bool IsLoaded => _module != null;

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
        public void Load()
        {
            var chemin = Path.Combine(AppConfig.AppDir, PremiumBinding.AssemblyFileName);
            if (!File.Exists(chemin)) return;

            try
            {
                Logger.Log($"Premium : composant trouvé, SHA-256 {Empreinte(chemin)}", LogLevel.INFO);

                var assembly = Assembly.LoadFrom(chemin);
                var type = assembly.GetType(PremiumBinding.TypeName, throwOnError: false);
                if (type == null)
                {
                    LicenceProblem = $"type {PremiumBinding.TypeName} introuvable";
                    Logger.Log($"Premium : {LicenceProblem}.", LogLevel.WARN);
                    return;
                }

                var lie = PremiumBinding.Bind(type, out var probleme);
                if (lie == null)
                {
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
