using System.Collections.Generic;

namespace ChaturbateRecorderApp.Config
{
    /// <summary>
    /// Historique des versions, affiché en local (aucun serveur requis) via une
    /// boîte de dialogue "Nouveautés" au premier lancement suivant une mise à
    /// jour de version. À compléter à chaque bump de &lt;Version&gt; dans le .csproj.
    ///
    /// Traduction (24.0) : Entries reste la source canonique en français, et
    /// EnglishChanges ne couvre que les versions à partir de la version courante
    /// au moment de ce passage (1.16.0). L'historique antérieur reste en
    /// français — son intérêt est surtout archivistique, et le dialogue
    /// "Nouveautés" n'affiche de toute façon qu'une seule version : celle qui
    /// vient d'être installée.
    /// </summary>
    public static class Changelog
    {
        public static readonly (string Version, string[] Changes)[] Entries =
        {
            ("1.0.0", new[]
            {
                "Migration du projet vers .NET 10.",
                "Barre de progression en mode indéterminé pendant l'enregistrement d'un live.",
                "Sélection de la qualité source (meilleure / moyenne 720p / minimale).",
                "Choix du codec de sortie (copie / H.264 / H.265, réencodé après coup sans bloquer l'UI).",
                "Choix du conteneur de sortie (MP4 / MKV / MOV).",
                "Choix du dossier de sauvegarde des enregistrements.",
                "Vérification du dossier d'exécution (réseau, temporaire, compressé).",
                "Détection des ACL permissives sur les dossiers sensibles.",
                "Vérification du hash du QR code de don.",
            }),
            ("1.1.0", new[]
            {
                "Enregistrement multi-stream : plusieurs lives en parallèle sans ouvrir plusieurs instances.",
                "Bouton \"Ouvrir\" par enregistrement (accès direct à la page du stream).",
                "Import de cookies navigateur (cookies.txt) pour accéder au contenu réservé à un compte connecté.",
                "Champ proxy SOCKS5/HTTP (confidentialité de l'IP vis-à-vis du site distant).",
                "Icône d'application.",
                "Publication sur GitHub (dépôt public + première release).",
            }),
            ("1.2.0", new[]
            {
                "Bouton \"Rechercher une mise à jour\" (vérifie et installe les nouvelles releases GitHub).",
                "Site web du projet (GitHub Pages) et bouton \"Site web\" dans l'appli.",
                "Guide de démarrage pas-à-pas au premier lancement, réutilisable via \"Guide de démarrage\".",
            }),
            ("1.3.0", new[]
            {
                "Watchdog anti-freeze sur yt-dlp/ffmpeg : arrêt automatique et log explicite si plus aucune sortie pendant un délai configurable (120s par défaut).",
                "Génération de miniature déplacée hors du thread UI (ne bloque plus l'interface pendant l'extraction ffmpeg).",
                "Logs structurés au format JSON (JSONL), avec horodatage ISO 8601 et source auto-détectée.",
            }),
            ("1.4.0", new[]
            {
                "Rotation automatique des logs : suppression des fichiers de plus de 14 jours au démarrage, et rotation (renommage horodaté) au-delà de 20 Mo par fichier.",
            }),
            ("1.5.0", new[]
            {
                "Icônes vectorielles sur les boutons principaux (rendu SVG à la volée, nettes à toute résolution).",
                "Mode simple / mode avancé : bouton de bascule masquant qualité/codec/format, dossier, cookies/proxy, thème, guide, mises à jour, favoris, don et logs.",
                "Notifications (zone de notification) : enregistrement terminé, erreur d'enregistrement, favori ajouté.",
                "Barre de progression : couleur dynamique selon l'état (bleu en cours, vert terminé, rouge erreur, gris arrêté) et effet de pulsation au démarrage.",
            }),
            ("1.6.0", new[]
            {
                "Reconnexion automatique (optionnelle, par enregistrement) : si le live se termine de façon inattendue, nouvelle tentative après un délai configurable, jusqu'à un nombre maximal de tentatives, avec possibilité d'annuler.",
                "Historique des enregistrements : liste des vidéos capturées (nom, taille, durée si ffprobe.exe est présent, date), avec bouton \"Ouvrir dossier\".",
            }),
            ("1.7.0", new[]
            {
                "Fenêtre redimensionnable (agrandir/réduire/maximiser) : le contenu défile automatiquement si la fenêtre devient plus petite que sa taille naturelle, au lieu d'être coupé.",
            }),
            ("1.8.0", new[]
            {
                "Suite de tests xUnit (sandbox URL, sandbox chemins, hash binaire, parsing progression, correspondance SAN TLS).",
                "Correctif : la vérification TLS (pinning serveur) comparait le nom d'hôte à du texte localisé par l'OS et ne fonctionnait donc jamais correctement sur un Windows non-anglais. Remplacé par un décodage ASN.1 direct, indépendant de la langue.",
            }),
            ("1.9.0", new[]
            {
                "Nouveau format de release \"portable\" : exécutable autonome (runtime .NET inclus, aucune installation requise sur la machine cible), en plus du format habituel dépendant du runtime.",
            }),
            ("1.10.0", new[]
            {
                "Modernisation de l'interface (palette Windows 11) : boutons en couleur d'accent sans bordure, coins arrondis, police Segoe UI, titres de section en gras, espacement généreux entre les sections (20px).",
                "Correctif : le texte de plusieurs boutons (\"Démarrer\", \"Tout arrêter\", \"Parcourir...\", \"Supprimer favori\", \"Rechercher une mise à jour\") était tronqué — largeurs ajustées.",
                "Correctif thème sombre : le fond de certains panneaux restait clair au lieu de suivre le thème actif.",
            }),
            ("1.11.0", new[]
            {
                "Sections de l'interface : les cadres classiques (GroupBox) sont remplacés par des panneaux à bordure arrondie et ombre légère, façon carte \"Fluent\".",
                "Barre de progression : remplissage animé (au lieu d'un saut instantané) à la fin d'un enregistrement.",
                "Boutons : couleurs distinctes au survol et à l'appui de la souris.",
            }),
            ("1.12.0", new[]
            {
                "Fondu d'ouverture au démarrage de l'application, et clignotement léger lors du changement de mode simple/avancé.",
                "Transition animée (au lieu d'un changement instantané) lors du passage entre thème clair et thème sombre.",
                "Boutons : la couleur au survol et à l'appui change désormais en douceur plutôt que d'un coup.",
            }),
            ("1.13.0", new[]
            {
                "Sélecteur de langue (Français / English) : traduit les libellés, boutons, cases à cocher et en-têtes de colonnes de l'interface principale. Choix mémorisé entre les lancements.",
            }),
            ("1.13.1", new[]
            {
                "Correctif : la miniature (et le réencodage) d'un enregistrement pouvait être associée au mauvais fichier lorsqu'un même salon était enregistré plusieurs fois (le fichier était retrouvé par \"le plus récent portant ce nom\", ambigu ; il est maintenant identifié par son nom exact, fixé au démarrage de l'enregistrement).",
                "Correctif : élargir la fenêtre ne redimensionnait pas le contenu (panneaux, listes, champs) qui restait figé à sa largeur d'origine.",
                "Correctif : les boutons \"Site web\", \"Ouvrir dossier\" et \"Supprimer favori\" avaient leur texte rogné.",
            }),
            ("1.14.0", new[]
            {
                "Nouvelle fenêtre \"Paramètres\" : thème, langue, dossier de sauvegarde, cookies, proxy et reconnexion automatique par défaut (désormais mémorisée) y sont regroupés, séparément de la fenêtre principale.",
                "Fermer la fenêtre principale la réduit maintenant dans la zone de notification au lieu de quitter l'application — les enregistrements en cours continuent en arrière-plan. Menu clic droit sur l'icône : Ouvrir / Paramètres / Fermer.",
                "Bouton \"Signaler un bug\" qui ouvre un ticket GitHub pré-rempli (version, système, dossier de capture).",
                "Le site du projet (tomoushie.github.io/ChaturbateRecorder) est désormais disponible en français et en anglais, via un bouton de bascule.",
            }),
            ("1.14.1", new[]
            {
                "Correctif : \"Rechercher une mise à jour\" pouvait télécharger la mauvaise variante (standard/portable) quand une release en propose plusieurs — l'exécutable en cours est maintenant détecté pour choisir le bon ZIP.",
            }),
            ("1.15.0", new[]
            {
                "Crash Reporter : les erreurs inattendues (thread principal ou non) sont désormais capturées, journalisées dans un fichier de rapport dédié, et affichées dans un dialogue proposant d'ouvrir le dossier des logs ou de redémarrer proprement l'application.",
            }),
            ("1.15.1", new[]
            {
                "Correctif : le hash attendu de yt-dlp.exe/ffmpeg.exe étant figé à la version testée par le mainteneur, toute mise à jour de ces outils (fréquente pour yt-dlp) bloquait le démarrage d'un enregistrement. Un dialogue propose désormais explicitement de faire confiance à la nouvelle version détectée ; ce choix est mémorisé.",
            }),
            ("1.16.0", new[]
            {
                "Nouveau bouton \"Diagnostic\" (mode avancé) : panneau affichant les versions (.NET, application, yt-dlp, ffmpeg), l'intégrité des binaires externes, l'état des ACL et du dossier d'exécution, et la joignabilité réseau — copiable en un clic pour un rapport de bug.",
            }),
        };

        /// <summary>
        /// Traductions anglaises, à partir de 1.16.0 uniquement (voir le
        /// commentaire de classe). Une version absente ici retombe
        /// silencieusement sur son entrée française : c'est le comportement
        /// voulu pour l'historique ancien, jamais une erreur.
        /// </summary>
        private static readonly Dictionary<string, string[]> EnglishChanges = new()
        {
            ["1.16.0"] = new[]
            {
                "New \"Diagnostics\" button (advanced mode): a panel showing versions (.NET, application, yt-dlp, ffmpeg), the integrity of external binaries, the state of the ACLs and of the execution folder, and network reachability — copied in one click for a bug report.",
            },
        };

        /// <summary>
        /// Retourne les nouveautés d'une version dans la langue demandée, avec
        /// repli sur le français si cette version n'a pas de traduction.
        /// Prend un bool plutôt qu'un AppLanguage à dessein : Config est la
        /// couche basse (référencée par Security/Services) et n'a pas à
        /// dépendre de UI.
        /// </summary>
        public static string[] GetChanges(string version, bool english)
        {
            if (english && EnglishChanges.TryGetValue(version, out var translated))
                return translated;

            foreach (var entry in Entries)
                if (entry.Version == version)
                    return entry.Changes;

            return System.Array.Empty<string>();
        }

        /// <summary>Exposé aux tests : voir ChangelogTests.</summary>
        internal static IReadOnlyDictionary<string, string[]> AllEnglishChanges => EnglishChanges;
    }
}
