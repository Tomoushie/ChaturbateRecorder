using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Ce qu'une carte de salon montre de son état, et rien d'autre.
    ///
    /// Ces deux règles vivaient dans le contrôle dessiné à la main
    /// (<c>UI/RoomCard.cs</c>), déjà isolées « pour être vérifiables sans
    /// afficher de fenêtre ». Elles le restent ici, et pour la même raison :
    /// écrites en XAML sous forme de déclencheurs, elles ne seraient plus
    /// testables, et une erreur y ferait afficher « en ligne » en rouge.
    /// </summary>
    public static class RoomCardVisuals
    {
        /// <summary>
        /// Clé du pinceau de l'état. On rend une CLÉ et non une couleur : le
        /// modèle de vue et ses tests n'ont ainsi aucune dépendance à WPF, et
        /// la couleur reste celle du thème courant, fondu compris.
        /// </summary>
        public static string StateBrushKey(RoomRowState state) => state switch
        {
            RoomRowState.Live => ThemeManager.BrushSuccess,
            RoomRowState.Recording => ThemeManager.BrushAccent,
            RoomRowState.Reconnecting => ThemeManager.BrushWarning,
            RoomRowState.Failed => ThemeManager.BrushDanger,
            // « Inexistant » est un échec DÉFINITIF, pas une panne passagère :
            // il mérite la couleur d'alerte, sans quoi une faute de frappe se
            // confondrait avec un salon simplement hors ligne.
            RoomRowState.NotFound => ThemeManager.BrushDanger,
            _ => ThemeManager.BrushFgMuted,
        };

        /// <summary>Hauteur d'une carte au repos.</summary>
        public const double CompactHeight = 60;

        /// <summary>Hauteur d'une carte qui a une progression à montrer.</summary>
        public const double ExpandedHeight = 92;

        /// <summary>
        /// La carte ne s'étend QUE pour les états qui ont une progression
        /// mesurable. « Terminé » et « échec » n'ont rien à montrer : les
        /// garder hauts gaspillerait la place au moment même où on parcourt sa
        /// liste. Les cartes fixes des logiciels concurrents ne laissent voir
        /// que quatre salons.
        /// </summary>
        public static bool IsExpanded(RoomRowState state) =>
            state is RoomRowState.Recording or RoomRowState.Reconnecting;

        /// <summary>Hauteur effective, dérivée de l'état.</summary>
        public static double HeightFor(RoomRowState state) =>
            IsExpanded(state) ? ExpandedHeight : CompactHeight;
    }
}
