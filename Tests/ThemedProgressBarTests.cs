using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Géométrie de la barre de progression dessinée à la main
    /// (UI/ThemedProgressBar), testée pour la même raison que celle de
    /// ThemedScrollBar : une barre au tiers de sa course se voit à l'œil, mais
    /// une barre qui n'atteint jamais tout à fait le bout à 100%, ou qui
    /// disparaît complètement à 1%, ne se remarque que dans des cas limites
    /// qu'on ne pense pas à essayer à la main.
    ///
    /// Rappel du contexte : cette barre remplace la ProgressBar native, dont
    /// la couleur d'état (3.4) ne pouvait pas fonctionner — PBM_SETBARCOLOR
    /// est ignoré dès que les styles visuels sont actifs.
    /// </summary>
    public class ThemedProgressBarTests
    {
        private const int Track = 350;

        [Theory]
        [InlineData(0, 0)]
        [InlineData(50, Track / 2)]
        [InlineData(100, Track)]
        public void FillWidthFollowsTheValueOverTheWholeTrack(int value, int expected)
        {
            Assert.Equal(expected, ThemedProgressBar.ComputeFillWidth(Track, 0, 100, value));
        }

        /// <summary>
        /// À 100%, la barre doit couvrir la piste au pixel près : un pixel
        /// manquant au bout laisse croire que l'enregistrement n'est pas fini.
        /// </summary>
        [Fact]
        public void FullValueFillsTheTrackExactly()
        {
            Assert.Equal(Track, ThemedProgressBar.ComputeFillWidth(Track, 0, 100, 100));
            Assert.Equal(0, ThemedProgressBar.ComputeFillWidth(Track, 0, 100, 0));
        }

        /// <summary>
        /// Une valeur hors bornes ne doit pas produire une barre qui déborde du
        /// contrôle (ou une largeur négative, que GDI+ refuserait).
        /// </summary>
        [Fact]
        public void OutOfRangeValuesAreClampedInsteadOfOverflowing()
        {
            Assert.Equal(Track, ThemedProgressBar.ComputeFillWidth(Track, 0, 100, 999));
            Assert.Equal(0, ThemedProgressBar.ComputeFillWidth(Track, 0, 100, -999));
        }

        [Fact]
        public void NonZeroMinimumIsTakenAsTheOrigin()
        {
            // 75 sur une plage 50..100, c'est la moitié de la piste, pas 75%.
            Assert.Equal(Track / 2, ThemedProgressBar.ComputeFillWidth(Track, 50, 100, 75));
            Assert.Equal(0, ThemedProgressBar.ComputeFillWidth(Track, 50, 100, 50));
        }

        /// <summary>
        /// Cas dégénérés rencontrés au tout premier affichage, avant que la
        /// fenêtre ait une taille, ou sur une plage vide : ils ne doivent pas
        /// lever de division par zéro.
        /// </summary>
        [Fact]
        public void DegenerateSizesAndRangesDoNotThrow()
        {
            Assert.Equal(0, ThemedProgressBar.ComputeFillWidth(0, 0, 100, 42));
            Assert.Equal(0, ThemedProgressBar.ComputeFillWidth(-5, 0, 100, 42));
            Assert.Equal(0, ThemedProgressBar.ComputeFillWidth(Track, 100, 100, 100));
            Assert.Equal(0, ThemedProgressBar.ComputeFillWidth(Track, 100, 0, 42));
        }

        /// <summary>
        /// Le segment du mode indéterminé doit entrer par la gauche et sortir
        /// par la droite. S'il était simplement placé à "phase * largeur", il
        /// apparaîtrait déjà entier au bord gauche et disparaîtrait d'un coup au
        /// bord droit au lieu de glisser.
        /// </summary>
        [Fact]
        public void MarqueeSegmentSlidesInFromTheLeftAndOutToTheRight()
        {
            var atStart = ThemedProgressBar.ComputeMarqueeSegment(Track, 0.0);
            Assert.Equal(0, atStart.Left);
            Assert.Equal(0, atStart.Width);

            // Juste après le départ : un bout de segment est visible au bord
            // gauche, pas encore sa largeur complète.
            var entering = ThemedProgressBar.ComputeMarqueeSegment(Track, 0.05);
            Assert.Equal(0, entering.Left);
            Assert.InRange(entering.Width, 1, (int)(Track * 0.35) - 1);

            // En fin de cycle : le segment sort par la droite, donc il touche le
            // bord droit sans le dépasser.
            var leaving = ThemedProgressBar.ComputeMarqueeSegment(Track, 0.95);
            Assert.Equal(Track, leaving.Left + leaving.Width);
        }

        [Fact]
        public void MarqueeSegmentNeverLeavesTheTrack()
        {
            for (var i = 0; i <= 100; i++)
            {
                var (left, width) = ThemedProgressBar.ComputeMarqueeSegment(Track, i / 100.0);

                Assert.InRange(left, 0, Track);
                Assert.InRange(width, 0, Track);
                Assert.InRange(left + width, 0, Track);
            }
        }

        /// <summary>
        /// La phase s'incrémente indéfiniment (elle n'est jamais remise à zéro
        /// par le timer) : elle doit donc boucler d'elle-même, sinon le segment
        /// se fige hors piste au bout de quelques secondes d'enregistrement.
        /// </summary>
        [Theory]
        [InlineData(0.42)]
        [InlineData(0.75)]
        public void MarqueePhaseWrapsAroundAcrossCycles(double phase)
        {
            var first = ThemedProgressBar.ComputeMarqueeSegment(Track, phase);

            Assert.Equal(first, ThemedProgressBar.ComputeMarqueeSegment(Track, phase + 1));
            Assert.Equal(first, ThemedProgressBar.ComputeMarqueeSegment(Track, phase + 137));
        }

        [Fact]
        public void MarqueeOnAnUnsizedControlDoesNotThrow()
        {
            Assert.Equal((0, 0), ThemedProgressBar.ComputeMarqueeSegment(0, 0.5));
            Assert.Equal((0, 0), ThemedProgressBar.ComputeMarqueeSegment(-5, 0.5));
        }
    }
}
