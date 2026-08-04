using System.Collections.Generic;
using System.Linq;

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
    /// français — son intérêt est surtout archivistique, et une annonce ne
    /// remonte en pratique que de quelques versions (voir GetChangesSince).
    /// Un rendu multi-versions peut donc mélanger les deux langues : c'est le
    /// repli voulu, chaque version étant résolue indépendamment par GetChanges.
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
            ("1.17.0", new[]
            {
                "Traduction étendue : les messages d'erreur, les dialogues, les notifications, le guide de démarrage et l'annonce des nouveautés suivent désormais la langue choisie, au lieu de rester en français quel que soit le réglage.",
                "Les lignes de la zone \"Enregistrements en cours\" (statut, bouton Stop / Retirer / Annuler, bouton Ouvrir) suivent également la langue choisie, y compris lorsqu'elle est changée pendant un enregistrement.",
                "Le panneau de diagnostic et le rapport d'erreur restent volontairement en français : ils sont destinés à être collés dans un ticket, où des rapports en deux langues compliqueraient le dépouillement.",
            }),
            ("1.18.0", new[]
            {
                "Le dialogue \"Nouveautés\" annonce désormais toutes les versions franchies depuis la dernière utilisation, regroupées par version, au lieu de la seule version installée. Passer directement de la 1.14.0 à la 1.18.0 ne fait donc plus disparaître les nouveautés intermédiaires.",
                "\"Nouveautés\" s'affiche maintenant dans une fenêtre redimensionnable qui défile, au lieu d'une boîte de dialogue système qui tronquait le texte au-delà de la hauteur de l'écran. Son ascenseur suit le thème clair ou sombre, ce que ne fait pas un ascenseur Windows classique.",
            }),
            ("1.19.0", new[]
            {
                "Nouveau bouton \"Sponsoriser (GitHub)\" dans le panneau \"Soutenir le projet\" : ouvre la page GitHub Sponsors du projet, en complément du don PayPal déjà proposé.",
                "Correctif : sur chaque ligne de la zone \"Enregistrements en cours\", le bas des lettres des boutons \"Ouvrir\" et \"Stop\" était coupé — les boutons étaient trop courts d'environ 6 pixels pour la police, jambages compris. Défaut présent depuis l'origine.",
                "Correctif : en anglais, le bouton \"Remove\" d'un enregistrement terminé s'affichait tronqué en \"Remov\" après un changement de thème, faute de largeur suffisante. Les deux boutons de la ligne ont été élargis.",
                "Correctif : le même défaut de hauteur touchait les boutons de la barre du haut (\"Paramètres\", \"Mode simple\", \"Guide de démarrage\", \"Rechercher une mise à jour\", \"Signaler un bug\", \"Diagnostic\"), dont le bas des lettres à jambage était lui aussi coupé. Ces boutons ont été rehaussés et les trois rangées repositionnées.",
                "Correctif : les lignes de la zone \"Enregistrements en cours\" ne prenaient pas le thème à leur création — leurs boutons gardaient l'apparence Windows par défaut au lieu de la couleur d'accent, et une ligne ajoutée en thème sombre restait claire jusqu'au changement de thème suivant.",
            }),
            ("1.19.1", new[]
            {
                "Correctif : la couleur de la barre de progression selon l'état (bleu en cours, vert terminé, rouge erreur, gris arrêté), annoncée en 1.5.0, ne s'est en réalité jamais affichée — Windows impose sa propre couleur aux barres de progression dès que les styles visuels sont actifs, et les quatre états apparaissaient donc tous en vert. La barre est désormais dessinée par l'application : les quatre états se distinguent enfin, et l'effet de pulsation au démarrage d'un enregistrement devient visible lui aussi.",
                "La piste de la barre de progression suit désormais le thème clair ou sombre, au lieu de rester blanche sur fond sombre comme le fait une barre Windows classique.",
            }),
            ("1.20.0", new[]
            {
                "Minuteur d'arrêt automatique : un nouveau menu \"Durée maximale\" (mode avancé) permet de choisir au démarrage combien de temps un enregistrement doit durer — 15 minutes, 30 minutes, 1, 2, 4 ou 8 heures — au terme desquelles il s'arrête tout seul. Le réglage vaut par enregistrement, comme la qualité ou le format, et reste sur \"Illimité\" par défaut.",
                "Le temps restant s'affiche sur la ligne de l'enregistrement concerné et se met à jour en continu. Rien n'apparaît pour les enregistrements sans minuteur.",
                "Une reconnexion automatique ne repousse pas l'échéance : une durée maximale de 2 heures désigne bien 2 heures de temps écoulé, et non 2 heures par tentative.",
            }),
            ("1.21.0", new[]
            {
                "Recherche automatique des mises à jour : l'application vérifie désormais d'elle-même toutes les heures si une nouvelle version est disponible, au lieu d'attendre un clic sur \"Rechercher une mise à jour\".",
                "Quand une version est trouvée, une notification apparaît dans la zone de notification ; cliquer dessus propose l'installation, avec le même avertissement qu'avant si des enregistrements sont en cours. Chaque version n'est annoncée qu'une fois.",
                "Nouvelle case \"Rechercher automatiquement les mises à jour\" dans les Paramètres, cochée par défaut : la décocher désactive immédiatement le seul appel réseau que l'application effectue d'elle-même.",
                "Une recherche automatique qui échoue (hors ligne, coupure réseau) n'affiche plus rien du tout et se contente d'une ligne de log — seul le bouton, où une réponse est attendue, signale encore les erreurs.",
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
            ["1.17.0"] = new[]
            {
                "Extended translation: error messages, dialogs, notifications, the getting started guide and the what's-new announcement now follow the selected language, instead of staying in French whatever the setting.",
                "Rows in the \"Active recordings\" area (status, Stop / Remove / Cancel button, Open button) also follow the selected language, including when it is changed while a recording is running.",
                "The diagnostics panel and the crash report deliberately stay in French: they are meant to be pasted into an issue, where reports in two languages would make triage harder.",
            },
            ["1.18.0"] = new[]
            {
                "The \"What's new\" dialog now announces every version crossed since the application was last used, grouped by version, instead of the installed version only. Going straight from 1.14.0 to 1.18.0 therefore no longer hides the intermediate changes.",
                "\"What's new\" is now shown in a resizable, scrolling window, instead of a system dialog box that truncated any text taller than the screen. Its scroll bar follows the light or dark theme, which a standard Windows scroll bar does not.",
            },
            ["1.19.0"] = new[]
            {
                "New \"Sponsor (GitHub)\" button in the \"Support the project\" panel: it opens the project's GitHub Sponsors page, alongside the PayPal donation already offered.",
                "Fix: on every row of the \"Active recordings\" area, the bottom of the letters on the \"Open\" and \"Stop\" buttons was cut off — the buttons were about 6 pixels too short for the font, descenders included. This defect had been there from the start.",
                "Fix: in English, the \"Remove\" button of a finished recording was truncated to \"Remov\" after a theme change, for lack of width. Both buttons on the row have been widened.",
                "Fix: the same height defect affected the buttons of the top bar (\"Settings\", \"Simple mode\", \"Getting started guide\", \"Check for updates\", \"Report a bug\", \"Diagnostics\"), where the bottom of descending letters was cut off too. Those buttons have been made taller and the three rows repositioned.",
                "Fix: rows in the \"Active recordings\" area did not pick up the theme when created — their buttons kept the default Windows look instead of the accent colour, and a row added while the dark theme was active stayed light until the next theme change.",
            },
            ["1.19.1"] = new[]
            {
                "Fix: the progress bar's state colour (blue while recording, green when finished, red on error, grey when stopped), announced back in 1.5.0, was in fact never displayed — Windows forces its own colour on progress bars as soon as visual styles are enabled, so all four states showed up green. The bar is now drawn by the application itself: the four states are finally distinguishable, and the pulse effect when a recording starts becomes visible too.",
                "The progress bar's track now follows the light or dark theme, instead of staying white on a dark background as a standard Windows bar does.",
            },
            ["1.20.0"] = new[]
            {
                "Automatic stop timer: a new \"Maximum duration\" menu (advanced mode) lets you choose, when starting a recording, how long it should run — 15 minutes, 30 minutes, 1, 2, 4 or 8 hours — after which it stops on its own. The setting applies per recording, like quality or format, and stays on \"Unlimited\" by default.",
                "The remaining time is shown on the row of the recording concerned and updates continuously. Nothing appears for recordings without a timer.",
                "An automatic reconnection does not push the deadline back: a maximum duration of 2 hours means 2 hours of elapsed time, not 2 hours per attempt.",
            },
            ["1.21.0"] = new[]
            {
                "Automatic update check: the application now checks on its own every hour whether a new version is available, instead of waiting for a click on \"Check for updates\".",
                "When a version is found, a notification appears in the notification area; clicking it offers to install, with the same warning as before if recordings are in progress. Each version is announced only once.",
                "New \"Automatically check for updates\" option in Settings, enabled by default: unchecking it immediately disables the only network call the application makes on its own.",
                "A failed automatic check (offline, network outage) no longer displays anything at all and just writes a log line — only the button, where an answer is expected, still reports errors.",
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

        /// <summary>
        /// Nouveautés à annoncer à quelqu'un qui passe de
        /// <paramref name="sinceVersion"/> (dernière version qu'il a réellement
        /// vue) à <paramref name="upToVersion"/> : toutes les entrées
        /// strictement postérieures à la première et jusqu'à la seconde
        /// incluse, de la plus récente à la plus ancienne.
        ///
        /// N'afficher que l'entrée de la version courante perdait tout
        /// l'intermédiaire, et ce n'est pas théorique : rien n'oblige un
        /// utilisateur à installer chaque release, et le projet en publie
        /// souvent plusieurs de suite. Qui mettait à jour depuis la 1.14.0
        /// n'apprenait jamais l'existence du Crash Reporter (1.15.0) ni du mode
        /// Diagnostic (1.16.0), pourtant présents dans le binaire qu'il venait
        /// d'installer.
        ///
        /// Repli volontaire vers la seule entrée de <paramref name="upToVersion"/>
        /// quand la plage est vide : borne basse absente ou illisible
        /// (settings.json édité à la main) et retour à une version antérieure.
        /// Déverser tout l'historique dans ces cas-là serait pire que de ne
        /// rien dire. Tableau vide seulement quand ce repli ne trouve rien non
        /// plus, c'est-à-dire quand upToVersion n'a aucune entrée — noter
        /// qu'une version installée sans entrée annonce quand même ce qui la
        /// précède tant que la borne basse, elle, est exploitable.
        ///
        /// Chaque version est traduite indépendamment : une annonce couvrant
        /// des versions d'avant et d'après 1.16.0 est donc légitimement mixte
        /// en anglais (voir le commentaire de classe).
        /// </summary>
        public static (string Version, string[] Changes)[] GetChangesSince(
            string? sinceVersion, string upToVersion, bool english)
        {
            var announced = Entries
                .Where(e => IsAnnounced(e.Version, sinceVersion, upToVersion))
                .OrderByDescending(e => e.Version, VersionOrder)
                .ToArray();

            if (announced.Length == 0)
                announced = Entries.Where(e => e.Version == upToVersion).ToArray();

            return announced
                .Select(e => (e.Version, Changes: GetChanges(e.Version, english)))
                .ToArray();
        }

        private static bool IsAnnounced(string version, string? sinceVersion, string upToVersion)
        {
            // Une borne illisible ne doit pas ouvrir la plage en grand : on
            // laisse GetChangesSince retomber sur la seule version courante.
            if (!System.Version.TryParse(sinceVersion, out _)) return false;
            if (!System.Version.TryParse(upToVersion, out _)) return false;

            return CompareVersions(version, sinceVersion) > 0
                && CompareVersions(version, upToVersion) <= 0;
        }

        private static readonly IComparer<string> VersionOrder =
            Comparer<string>.Create((a, b) => CompareVersions(a, b));

        /// <summary>
        /// Comparaison numérique de versions — "1.9.0" est antérieure à
        /// "1.10.0", ce qu'une comparaison de chaînes affirme exactement à
        /// l'envers. Une version illisible est classée avant toutes les autres
        /// plutôt que de faire lever la comparaison.
        /// Exposé aux tests : voir ChangelogTests.
        /// </summary>
        internal static int CompareVersions(string? a, string? b)
        {
            var parsedA = System.Version.TryParse(a, out var va) ? va : null;
            var parsedB = System.Version.TryParse(b, out var vb) ? vb : null;

            if (parsedA == null) return parsedB == null ? 0 : -1;
            if (parsedB == null) return 1;

            return parsedA.CompareTo(parsedB);
        }

        /// <summary>Exposé aux tests : voir ChangelogTests.</summary>
        internal static IReadOnlyDictionary<string, string[]> AllEnglishChanges => EnglishChanges;
    }
}
