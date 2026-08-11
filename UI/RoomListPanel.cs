using System;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// La liste défilante qui porte les cartes de salon (97.0).
    ///
    /// **Elle dimensionne ses enfants AVANT de décider s'il lui faut un
    /// ascenseur**, et c'est toute sa raison d'être. Un `FlowLayoutPanel`
    /// ordinaire calcule ses ascenseurs pendant sa passe de mise en page, à
    /// partir de la largeur QU'ONT ENCORE ses enfants. Ajuster ces largeurs
    /// après coup, depuis `SizeChanged`, arrive donc systématiquement un temps
    /// trop tard : en rétrécissant la fenêtre, les cartes gardent brièvement
    /// leur ancienne largeur, dépassent, et un ascenseur HORIZONTAL apparaît le
    /// temps d'une image avant de disparaître.
    ///
    /// Le défaut est invisible sur une capture — il ne dure que pendant le
    /// glissement de la souris, et s'efface au relâchement. Signalé en usage
    /// réel par le mainteneur, qui n'a pas pu le photographier.
    /// </summary>
    internal sealed class RoomListPanel : FlowLayoutPanel
    {
        /// <summary>
        /// Largeur minimale d'une carte. En dessous, ses boutons d'action se
        /// chevaucheraient : mieux vaut alors un ascenseur horizontal assumé
        /// qu'une carte illisible.
        /// </summary>
        internal const int LargeurMiniCarte = 320;

        internal static int LargeurUtile(int largeurPanneau) => Math.Max(
            LargeurMiniCarte,
            // La place de l'ascenseur vertical est réservée en permanence : la
            // calculer d'après sa présence ferait dépendre la largeur de ce
            // qu'elle provoque.
            largeurPanneau - SystemInformation.VerticalScrollBarWidth - 6);

        protected override void OnLayout(LayoutEventArgs e)
        {
            var largeur = LargeurUtile(Width);
            foreach (Control enfant in Controls)
                if (enfant.Width != largeur) enfant.Width = largeur;

            base.OnLayout(e);
        }
    }
}
