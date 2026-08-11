using System.Drawing;
using System.Windows.Forms;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Géométrie des boutons d'action d'une carte (97.0).
    ///
    /// **Le point de tout ceci est que la carte se réagence SEULE.** Tant que
    /// ses boutons étaient placés depuis l'extérieur, il fallait reparcourir la
    /// liste après chaque redimensionnement — donc toujours un temps trop tard,
    /// ce qui faisait clignoter un ascenseur horizontal pendant le glissement
    /// de la fenêtre. Défaut signalé en usage réel, et impossible à
    /// photographier : il s'efface au relâchement du clic.
    ///
    /// Un test plutôt qu'une capture, parce qu'aucune capture ne peut le voir.
    /// </summary>
    public class RoomCardActionsTests
    {
        private const int Marge = 14;

        private static (RoomCard Carte, Control P, Control O, Control R) Monter(int largeur)
        {
            var carte = new RoomCard { Width = largeur };
            var p = new Button { Size = new Size(104, 26) };
            var o = new Button { Size = new Size(30, 26) };
            var r = new Button { Size = new Size(30, 26) };
            carte.SetActions(p, o, r);
            return (carte, p, o, r);
        }

        [Fact]
        public void TheActionsSitAgainstTheRightEdge()
        {
            var (carte, p, o, r) = Monter(600);
            using (carte)
            {
                Assert.Equal(600 - Marge, r.Right);
                Assert.True(o.Right < r.Left, "« ouvrir » chevauche la corbeille");
                Assert.True(p.Right < o.Left, "le bouton principal chevauche « ouvrir »");
            }
        }

        /// <summary>
        /// La régression que la modification corrige : élargir la carte doit
        /// replacer ses boutons DANS LA FOULÉE, sans que personne d'autre ait à
        /// le demander.
        /// </summary>
        [Fact]
        public void WideningTheCardMovesItsActionsImmediately()
        {
            var (carte, _, _, r) = Monter(600);
            using (carte)
            {
                carte.Width = 900;
                Assert.Equal(900 - Marge, r.Right);

                carte.Width = 420;
                Assert.Equal(420 - Marge, r.Right);
            }
        }

        /// <summary>
        /// Les boutons restent centrés sur la bande COMPACTE quand la carte
        /// s'étend pour un enregistrement : sans ça, ils descendraient au milieu
        /// de la barre de progression.
        /// </summary>
        [Fact]
        public void TheActionsStayOnTheCompactBandWhenTheCardExpands()
        {
            var (carte, p, _, _) = Monter(600);
            using (carte)
            {
                var avant = p.Top;
                carte.Height = RoomCard.ExpandedHeight;

                Assert.Equal(avant, p.Top);
                Assert.True(p.Bottom <= RoomCard.CompactHeight,
                    $"les boutons débordent de la bande compacte ({p.Bottom} > {RoomCard.CompactHeight})");
            }
        }

        /// <summary>
        /// La largeur utile réserve la place de l'ascenseur en permanence, et
        /// ne descend jamais sous le plancher — en dessous, les boutons se
        /// chevaucheraient.
        /// </summary>
        [Fact]
        public void TheUsableWidthReservesTheScrollBarAndHasAFloor()
        {
            Assert.True(RoomListPanel.LargeurUtile(1000) < 1000,
                "la place de l'ascenseur vertical n'est pas réservée");
            Assert.Equal(RoomListPanel.LargeurMiniCarte, RoomListPanel.LargeurUtile(50));
        }
    }
}
