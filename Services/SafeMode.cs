using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>Composants que l'application sait désactiver sans s'arrêter.</summary>
    public enum SafeComponent
    {
        /// <summary>Réencodage et miniatures. L'enregistrement brut continue sans.</summary>
        Ffmpeg,
        /// <summary>Fichier cookies passé à yt-dlp.</summary>
        Cookies,
        /// <summary>Proxy SOCKS5/HTTP passé à yt-dlp.</summary>
        Proxy,
        /// <summary>Plusieurs enregistrements simultanés.</summary>
        MultiStream,
        /// <summary>Surveillance automatique des salons (88.0).</summary>
        Watch,
    }

    /// <summary>Pourquoi un composant est hors service.</summary>
    public enum SafeReason
    {
        /// <summary>Actif.</summary>
        None,
        /// <summary>Décoché par l'utilisateur dans les Paramètres.</summary>
        Manual,
        /// <summary>Désactivé par l'application, un contrôle ayant échoué.</summary>
        Automatic,
    }

    /// <summary>
    /// « Safe Mode » (29.0 / 2.2) : désactiver une fonctionnalité défaillante,
    /// le dire clairement, et **continuer à tourner**.
    ///
    /// **Le principe qui gouverne tout le reste** : aucune de ces défaillances
    /// ne justifie de refuser de démarrer. Un ffmpeg absent empêche le
    /// réencodage et les miniatures — il n'empêche pas d'enregistrer. Un fichier
    /// cookies illisible empêche d'accéder au contenu réservé — il n'empêche pas
    /// de capturer un flux public. Jusqu'ici ces situations produisaient soit un
    /// echec obscur au moment d'enregistrer, soit rien du tout (le cookies.txt
    /// invalide du 2026-08-08 faisait echouer TOUTES les captures en silence).
    ///
    /// Deux origines, un seul etat : l'utilisateur peut desactiver un composant
    /// a la main, et l'application peut le desactiver elle-meme apres un
    /// controle rate. Le second ne doit jamais ecraser le premier au point de
    /// faire croire a l'utilisateur qu'il a change d'avis — d'ou la distinction
    /// Manual / Automatic, qui sert a l'affichage autant qu'a la logique.
    /// </summary>
    public static class SafeMode
    {
        private static readonly Dictionary<SafeComponent, string> AutoDisabled = new();
        private static HashSet<SafeComponent> _manuallyDisabled = new();

        /// <summary>Rejoue l'etat manuel persiste au demarrage.</summary>
        public static void LoadManual(IEnumerable<string>? names)
        {
            _manuallyDisabled = new HashSet<SafeComponent>();
            foreach (var name in names ?? Enumerable.Empty<string>())
            {
                if (Enum.TryParse<SafeComponent>(name, ignoreCase: true, out var c))
                    _manuallyDisabled.Add(c);
                else
                    Logger.Log($"Safe Mode : composant inconnu ignoré dans les réglages : '{name}'.", LogLevel.WARN);
            }
        }

        public static IReadOnlyCollection<string> ManualNames =>
            _manuallyDisabled.Select(c => c.ToString()).ToList();

        public static void SetManual(SafeComponent component, bool disabled)
        {
            if (disabled) _manuallyDisabled.Add(component);
            else _manuallyDisabled.Remove(component);
        }

        /// <summary>
        /// Désactive un composant à la suite d'un contrôle raté. La raison est
        /// conservée telle quelle : c'est elle qu'on montre à l'utilisateur, et
        /// « ffmpeg.exe introuvable » lui apprend infiniment plus que
        /// « fonctionnalité indisponible ».
        /// </summary>
        public static void DisableAutomatically(SafeComponent component, string reason)
        {
            AutoDisabled[component] = reason;
            Logger.Log($"Safe Mode : {component} désactivé automatiquement — {reason}", LogLevel.WARN);
        }

        /// <summary>Remet à zéro les désactivations automatiques (nouveau contrôle).</summary>
        public static void ClearAutomatic() => AutoDisabled.Clear();

        public static bool IsEnabled(SafeComponent component) =>
            !_manuallyDisabled.Contains(component) && !AutoDisabled.ContainsKey(component);

        /// <summary>
        /// Le manuel prime sur l'automatique dans l'affichage : si l'utilisateur
        /// a lui-meme decoche la case, lui expliquer que l'application l'a
        /// desactive serait faux et deroutant.
        /// </summary>
        public static SafeReason ReasonFor(SafeComponent component)
        {
            if (_manuallyDisabled.Contains(component)) return SafeReason.Manual;
            if (AutoDisabled.ContainsKey(component)) return SafeReason.Automatic;
            return SafeReason.None;
        }

        /// <summary>Message du contrôle raté, vide si le composant n'est pas désactivé automatiquement.</summary>
        public static string AutomaticReason(SafeComponent component) =>
            AutoDisabled.TryGetValue(component, out var r) ? r : "";

        /// <summary>Composants désactivés par l'application, dans l'ordre de l'enum.</summary>
        public static IReadOnlyList<SafeComponent> AutomaticallyDisabled =>
            Enum.GetValues<SafeComponent>().Where(AutoDisabled.ContainsKey).ToList();

        /// <summary>Vrai si quoi que ce soit est hors service, quelle qu'en soit l'origine.</summary>
        public static bool AnythingDisabled =>
            _manuallyDisabled.Count > 0 || AutoDisabled.Count > 0;
    }
}
