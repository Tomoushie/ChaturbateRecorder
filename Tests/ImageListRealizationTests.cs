using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le piège d'ImageList qui a fait planter la v1.35.0 au lancement, à deux
    /// reprises (journaux de crash du 2026-08-10, 19:54 et 21:50).
    ///
    /// **Pourquoi un test plutôt qu'une relecture** : le défaut est une COURSE.
    /// Il ne se produit que si l'historique finit de se rafraîchir avant que sa
    /// ListView ait obtenu son handle, ce qui dépend de la vitesse du disque et
    /// du nombre de fichiers. Il ne se voit ni à la compilation, ni sur une
    /// capture, ni à tous les lancements — trois raisons pour lesquelles il a
    /// survécu jusqu'à une version publiée.
    ///
    /// Ces deux tests figent le MÉCANISME, pas le symptôme : le premier prouve
    /// que la faute est bien là où on la croit, le second que le garde-fou la
    /// supprime. Sans le premier, le second passerait au vert même si la cause
    /// réelle était ailleurs.
    /// </summary>
    public class ImageListRealizationTests
    {
        private static ImageList NeuveNonRealisee() => new()
        {
            ImageSize = new Size(48, 27),
            ColorDepth = ColorDepth.Depth32Bit,
        };

        /// <summary>
        /// La faute, reproduite. Tant que le handle natif n'existe pas,
        /// <c>Images.Add</c> ne recopie rien : elle garde la référence et ne la
        /// matérialise qu'à la création du handle. Libérer le bitmap juste après
        /// l'avoir ajouté laisse donc une image morte dans la liste, et c'est
        /// Windows qui la découvre — bien plus tard, en créant le handle de la
        /// ListView, d'où une pile d'appels qui ne cite aucun code de
        /// l'application.
        /// </summary>
        [Fact]
        public void DisposingABitmapBeforeTheListIsRealisedPoisonsIt()
        {
            using var images = NeuveNonRealisee();

            var bitmap = new Bitmap(48, 27);
            images.Images.Add(bitmap);
            bitmap.Dispose();

            // C'est exactement ce que fait ListView.OnHandleCreated en
            // appelant RealizeProperties.
            Assert.ThrowsAny<ArgumentException>(() => _ = images.Handle);
        }

        /// <summary>
        /// Le garde-fou : réaliser la liste AVANT d'y ajouter. Add recopie alors
        /// l'image dans le handle natif immédiatement, et le bitmap d'origine ne
        /// sert plus à rien — le libérer est non seulement sûr, mais nécessaire
        /// (cinquante bitmaps fuiraient à chaque rafraîchissement).
        /// </summary>
        [Fact]
        public void RealisingTheListFirstMakesTheDisposeSafe()
        {
            using var images = NeuveNonRealisee();

            _ = images.Handle;

            var bitmap = new Bitmap(48, 27);
            images.Images.Add(bitmap);
            bitmap.Dispose();

            // Ni l'accès au handle ni la lecture de l'image ne doivent broncher.
            Assert.NotEqual(IntPtr.Zero, images.Handle);
            Assert.Single(images.Images);
            using var relue = images.Images[0];
            Assert.Equal(new Size(48, 27), relue.Size);
        }

        /// <summary>
        /// Le cas réel du rafraîchissement : vider puis re-remplir une liste déjà
        /// réalisée. <c>Clear</c> ne doit pas « dé-réaliser » le handle, sans quoi
        /// le garde-fou ne tiendrait que pour le premier passage et le défaut
        /// reviendrait au second — la forme la plus coûteuse d'un correctif, celle
        /// qu'on croit acquise.
        /// </summary>
        [Fact]
        public void ClearingARealisedListKeepsItSafeForTheNextRefresh()
        {
            using var images = NeuveNonRealisee();
            _ = images.Handle;

            for (var passage = 0; passage < 3; passage++)
            {
                images.Images.Clear();

                var bitmap = new Bitmap(48, 27);
                images.Images.Add(bitmap);
                bitmap.Dispose();

                Assert.Single(images.Images);
                using var relue = images.Images[0];
                Assert.Equal(new Size(48, 27), relue.Size);
            }
        }
    }
}
