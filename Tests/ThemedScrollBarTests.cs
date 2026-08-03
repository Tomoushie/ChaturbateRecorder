using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Géométrie de l'ascenseur dessiné à la main (UI/ThemedScrollBar).
    /// Testée directement parce qu'elle est la seule partie du contrôle qui
    /// puisse être fausse en silence : un curseur mal placé se remarque à
    /// l'œil, mais un curseur qui n'atteint jamais tout à fait le bas, ou qui
    /// disparaît sur un contenu très long, se voit seulement dans des cas
    /// limites qu'on ne pense pas à essayer à la main.
    /// </summary>
    public class ThemedScrollBarTests
    {
        private const int Track = 300;

        [Fact]
        public void NoScrollingNeededFillsTheWholeTrack()
        {
            // Contenu plus court que la zone visible : rien à faire défiler.
            var (top, height) = ThemedScrollBar.ComputeThumb(Track, maximum: 200, largeChange: 300, value: 0);

            Assert.Equal(0, top);
            Assert.Equal(Track, height);
        }

        /// <summary>
        /// La hauteur du curseur dit quelle proportion du contenu est visible :
        /// la moitié visible, la moitié de la piste.
        /// </summary>
        [Fact]
        public void ThumbHeightIsProportionalToTheVisibleShareOfTheContent()
        {
            Assert.Equal(Track / 2, ThemedScrollBar.ComputeThumb(Track, 600, 300, 0).Height);
            Assert.Equal(Track / 4, ThemedScrollBar.ComputeThumb(Track, 1200, 300, 0).Height);
        }

        /// <summary>
        /// Sur un contenu très long, un curseur strictement proportionnel
        /// ferait quelques pixels de haut et deviendrait insaisissable.
        /// </summary>
        [Fact]
        public void ThumbNeverShrinksBelowAGrabbableHeight()
        {
            var (_, height) = ThemedScrollBar.ComputeThumb(Track, maximum: 100_000, largeChange: 300, value: 0);

            Assert.True(height >= 24, $"Curseur de {height}px : trop petit pour être attrapé à la souris.");
        }

        [Fact]
        public void ThumbStartsAtTheTopAndEndsFlushWithTheBottom()
        {
            const int max = 900, page = 300;
            var height = ThemedScrollBar.ComputeThumb(Track, max, page, 0).Height;

            Assert.Equal(0, ThemedScrollBar.ComputeThumb(Track, max, page, 0).Top);

            // Défilement au maximum : le bas du curseur doit toucher le bas de
            // la piste, sinon l'utilisateur croit qu'il reste du contenu.
            var atEnd = ThemedScrollBar.ComputeThumb(Track, max, page, max - page);
            Assert.Equal(Track, atEnd.Top + height);
        }

        [Fact]
        public void ValueBeyondTheRangeIsClampedInsteadOfOverflowing()
        {
            const int max = 900, page = 300;
            var height = ThemedScrollBar.ComputeThumb(Track, max, page, 0).Height;

            Assert.Equal(0, ThemedScrollBar.ComputeThumb(Track, max, page, -500).Top);
            Assert.Equal(Track - height, ThemedScrollBar.ComputeThumb(Track, max, page, 99_999).Top);
        }

        /// <summary>
        /// Le glisser du curseur repose sur la réciproque du placement : si les
        /// deux ne s'accordent pas, le curseur "fuit" sous le pointeur.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(150)]
        [InlineData(300)]
        [InlineData(600)]
        public void DraggingTheThumbRoundTripsBackToTheSameValue(int value)
        {
            const int max = 900, page = 300;

            var top = ThemedScrollBar.ComputeThumb(Track, max, page, value).Top;
            var roundTripped = ThemedScrollBar.ValueFromThumbTop(Track, max, page, top);

            // Tolérance d'un pixel de piste : le placement arrondit en pixels,
            // et une piste de 300px ne peut pas distinguer 600 valeurs.
            var pixelWorth = (max - page) / (double)Track;
            Assert.True(System.Math.Abs(roundTripped - value) <= pixelWorth + 1,
                $"Valeur {value} -> curseur {top}px -> valeur {roundTripped}.");
        }

        [Fact]
        public void DraggingBeyondTheTrackStaysWithinRange()
        {
            const int max = 900, page = 300;

            Assert.Equal(0, ThemedScrollBar.ValueFromThumbTop(Track, max, page, -80));
            Assert.Equal(max - page, ThemedScrollBar.ValueFromThumbTop(Track, max, page, Track + 80));
        }

        /// <summary>
        /// Cas dégénérés rencontrés au tout premier affichage, avant que la
        /// fenêtre ait une taille : ils ne doivent pas lever de division par
        /// zéro.
        /// </summary>
        [Fact]
        public void DegenerateSizesDoNotThrow()
        {
            Assert.Equal((0, 0), ThemedScrollBar.ComputeThumb(0, 900, 300, 42));
            Assert.Equal((0, 10), ThemedScrollBar.ComputeThumb(10, 900, 300, 42));
            Assert.Equal(0, ThemedScrollBar.ValueFromThumbTop(0, 900, 300, 42));
            Assert.Equal(0, ThemedScrollBar.ValueFromThumbTop(Track, 300, 300, 42));
        }
    }
}
