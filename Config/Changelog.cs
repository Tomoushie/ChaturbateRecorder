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
        };
    }
}
