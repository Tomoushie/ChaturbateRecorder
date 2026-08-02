using System.Collections.Generic;

namespace ChaturbateRecorderApp.UI
{
    public enum AppLanguage { French, English }

    /// <summary>
    /// Traductions de l'UI principale (20.0) : uniquement les libellés fixes
    /// posés dans InitializeComponent (labels, boutons, cases à cocher,
    /// en-têtes de colonnes, items de ComboBox, bascule de mode). Les
    /// messages d'erreur/confirmations, notifications toast, logs, le guide
    /// de démarrage (TutorialForm) et l'historique des nouveautés (Changelog)
    /// restent en français pour l'instant — hors périmètre de ce premier
    /// passage de traduction, ils sont générés dynamiquement à des dizaines
    /// d'endroits différents du code.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, (string Fr, string En)> Strings = new()
        {
            ["theme.label"] = ("Thème :", "Theme:"),
            ["theme.light"] = ("Clair", "Light"),
            ["theme.dark"] = ("Sombre", "Dark"),
            ["language.label"] = ("Langue :", "Language:"),

            ["button.checkUpdate"] = ("Rechercher une mise à jour", "Check for updates"),
            ["button.tutorial"] = ("Guide de démarrage", "Getting started guide"),
            ["mode.switchToSimple"] = ("Mode simple", "Simple mode"),
            ["mode.switchToAdvanced"] = ("Mode avancé", "Advanced mode"),

            ["panel.record"] = ("Enregistrement", "Recording"),
            ["label.url"] = ("URL Chaturbate :", "Chaturbate URL:"),
            ["button.start"] = ("Démarrer", "Start"),
            ["button.stopAll"] = ("Tout arrêter", "Stop all"),
            ["button.addFavorite"] = ("+ Favori", "+ Favorite"),

            ["label.quality"] = ("Qualité source :", "Source quality:"),
            ["quality.best"] = ("Meilleure qualité (recommandé)", "Best quality (recommended)"),
            ["quality.medium"] = ("Qualité moyenne (720p max)", "Medium quality (720p max)"),
            ["quality.worst"] = ("Qualité minimale (économie)", "Minimum quality (saves space)"),

            ["label.codec"] = ("Codec de sortie :", "Output codec:"),
            ["codec.copy"] = ("Copie sans réencodage (recommandé, rapide)", "Copy without re-encoding (recommended, fast)"),
            ["codec.h264"] = ("H.264 (libx264 — compatibilité universelle)", "H.264 (libx264 — universal compatibility)"),
            ["codec.h265"] = ("H.265 (libx265 — fichier plus léger)", "H.265 (libx265 — smaller file)"),

            ["label.format"] = ("Format de sortie :", "Output format:"),
            ["format.mp4"] = ("MP4 (recommandé)", "MP4 (recommended)"),
            ["format.mkv"] = ("MKV (plus robuste)", "MKV (more robust)"),
            ["format.mov"] = ("MOV", "MOV"),

            ["label.saveDir"] = ("Dossier de sauvegarde :", "Save folder:"),
            ["button.browse"] = ("Parcourir...", "Browse..."),
            ["label.cookies"] = ("Cookies (optionnel) :", "Cookies (optional):"),
            ["label.proxy"] = ("Proxy SOCKS5/HTTP (optionnel) :", "SOCKS5/HTTP proxy (optional):"),
            ["checkbox.autoReconnect"] = ("Reconnexion automatique si le live se termine de façon inattendue", "Automatically reconnect if the live ends unexpectedly"),

            ["panel.progress"] = ("Enregistrements en cours", "Active recordings"),

            ["panel.history"] = ("Historique des enregistrements", "Recording history"),
            ["column.file"] = ("Fichier", "File"),
            ["column.size"] = ("Taille", "Size"),
            ["column.duration"] = ("Durée", "Duration"),
            ["column.date"] = ("Date", "Date"),
            ["button.refresh"] = ("Actualiser", "Refresh"),
            ["button.openFolder"] = ("Ouvrir dossier", "Open folder"),

            ["panel.favorites"] = ("Favoris", "Favorites"),
            ["button.load"] = ("Charger", "Load"),
            ["button.removeFavorite"] = ("Supprimer favori", "Remove favorite"),

            ["panel.donate"] = ("Soutenir le projet", "Support the project"),
            ["button.donate"] = ("Faire un don (PayPal)", "Donate (PayPal)"),
            ["button.website"] = ("Site web", "Website"),
            ["label.donate"] = ("Scanne le QR code avec ton téléphone, ou clique sur le bouton.", "Scan the QR code with your phone, or click the button."),

            ["panel.logs"] = ("Logs", "Logs"),
        };

        public static string Get(string key, AppLanguage lang)
        {
            if (!Strings.TryGetValue(key, out var pair)) return key;
            return lang == AppLanguage.English ? pair.En : pair.Fr;
        }
    }
}
