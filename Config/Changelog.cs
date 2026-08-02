namespace ChaturbateRecorderApp.Config
{
    /// <summary>
    /// Historique des versions, affiché en local (aucun serveur requis) via une
    /// boîte de dialogue "Nouveautés" au premier lancement suivant une mise à
    /// jour de version. À compléter à chaque bump de &lt;Version&gt; dans le .csproj.
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
        };
    }
}
