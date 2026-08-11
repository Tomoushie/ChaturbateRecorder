using System.Drawing;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Rendu de la vignette premium sur la carte de salon (97.0).
    ///
    /// **Pourquoi peindre plutôt que raisonner** : rien dans le code ne dit si
    /// une image est effectivement dessinée, ni à quel endroit. Une propriété
    /// posée mais jamais peinte, ou peinte sous le texte, compile et passe tous
    /// les autres tests. Et ce défaut ne toucherait QUE les acheteurs — le seul
    /// public dont on ne verra jamais l'écran.
    ///
    /// La capture de la vignette elle-même (yt-dlp + ffmpeg sur un direct) reste
    /// non éprouvée ici : elle demande un salon en ligne.
    /// </summary>
    public class RoomCardPreviewTests
    {
        // Position attendue : marge 14 + pictogramme 18 + écart 12 = 44, et
        // 36 px de haut centrés sur la bande compacte de 60.
        private const int CentreX = 44 + 32;
        private const int CentreY = 60 / 2;

        private static readonly Color Temoin = Color.FromArgb(255, 0, 255);

        private static Bitmap Peindre(bool avecVignette)
        {
            using var carte = new RoomCard
            {
                Width = 600,
                RoomName = "salon_demo",
                StateLabel = "En ligne",
                Palette = ThemeManager.GetPalette(AppTheme.Light),
            };

            if (avecVignette)
            {
                var vignette = new Bitmap(64, 36);
                using (var g = Graphics.FromImage(vignette))
                    g.Clear(Temoin);
                // La carte devient propriétaire et la libérera.
                carte.Preview = vignette;
            }

            var rendu = new Bitmap(carte.Width, carte.Height);
            carte.DrawToBitmap(rendu, new Rectangle(0, 0, carte.Width, carte.Height));
            return rendu;
        }

        [Fact]
        public void ThePreviewIsActuallyDrawnWhereItIsExpected()
        {
            using var rendu = Peindre(avecVignette: true);

            var pixel = rendu.GetPixel(CentreX, CentreY);
            Assert.True(pixel.R > 200 && pixel.G < 60 && pixel.B > 200,
                $"aucune vignette peinte en ({CentreX},{CentreY}) — pixel lu : {pixel}");
        }

        /// <summary>
        /// Le contrôle inverse, sans lequel le premier ne prouverait rien : si
        /// la couleur témoin s'y trouvait déjà pour une autre raison, le test
        /// passerait au vert même sans vignette.
        /// </summary>
        [Fact]
        public void WithoutAPreviewThatSpotShowsTheCardSurface()
        {
            using var rendu = Peindre(avecVignette: false);

            var pixel = rendu.GetPixel(CentreX, CentreY);
            Assert.False(pixel.R > 200 && pixel.G < 60 && pixel.B > 200,
                $"couleur témoin trouvée sans vignette en ({CentreX},{CentreY}) : {pixel}");
        }

        /// <summary>
        /// Le nom du salon ne doit pas se retrouver SOUS la vignette : le texte
        /// commence après elle. On vérifie qu'à hauteur du nom, juste à droite
        /// de la vignette, on n'est plus sur la couleur témoin.
        /// </summary>
        [Fact]
        public void ThePreviewDoesNotSwallowTheRoomName()
        {
            using var rendu = Peindre(avecVignette: true);

            // 44 + 64 = 108 : bord droit de la vignette. Deux pixels après.
            var pixel = rendu.GetPixel(110, CentreY);
            Assert.False(pixel.R > 200 && pixel.G < 60 && pixel.B > 200,
                $"la vignette déborde au-delà de sa place, en x=110 : {pixel}");
        }

        /// <summary>
        /// Remplacer la vignette libère la précédente. Sans ça, un
        /// rafraîchissement toutes les deux minutes sur vingt salons ferait
        /// fuir des centaines de bitmaps en une soirée.
        /// </summary>
        [Fact]
        public void ReplacingThePreviewDisposesTheOldOne()
        {
            using var carte = new RoomCard { Width = 600 };

            var premiere = new Bitmap(64, 36);
            carte.Preview = premiere;
            carte.Preview = new Bitmap(64, 36);

            // Une image libérée lève dès qu'on interroge sa taille.
            Assert.ThrowsAny<System.Exception>(() => _ = premiere.Width);
        }
    }
}
