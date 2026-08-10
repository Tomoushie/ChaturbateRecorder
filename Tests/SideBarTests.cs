using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Géométrie de la barre de navigation (97.0).
    ///
    /// C'est la seule arithmétique de ce contrôle, et une erreur d'un pixel y
    /// sélectionnerait la mauvaise section près des bords — un défaut qu'on
    /// remarque à l'usage mais jamais sur une capture, puisqu'il faut cliquer
    /// pour le voir.
    /// </summary>
    public class SideBarTests
    {
        private const int Top = 12;
        private const int Hauteur = 44;

        [Theory]
        [InlineData(0, -1)]              // au-dessus de la première entrée
        [InlineData(11, -1)]             // dernier pixel de la marge haute
        [InlineData(12, 0)]              // premier pixel de la première entrée
        [InlineData(55, 0)]              // dernier pixel de la première entrée
        [InlineData(56, 1)]              // premier pixel de la deuxième
        [InlineData(187, 3)]             // dernier pixel de la quatrième
        [InlineData(188, -1)]            // sous la dernière entrée
        [InlineData(4000, -1)]           // bien plus bas que la liste
        public void MapsAClickToTheRightSection(int y, int attendu)
        {
            Assert.Equal(attendu, SideBar.IndexAt(y, count: 4));
        }

        [Fact]
        public void BoundariesAreContiguous()
        {
            // Aucun pixel perdu entre deux entrées : le dernier de l'une et le
            // premier de la suivante doivent se toucher.
            for (var i = 0; i < 4; i++)
            {
                Assert.Equal(i, SideBar.IndexAt(Top + i * Hauteur, 4));
                Assert.Equal(i, SideBar.IndexAt(Top + (i + 1) * Hauteur - 1, 4));
            }
        }

        [Fact]
        public void AnEmptyBarSelectsNothing()
        {
            Assert.Equal(-1, SideBar.IndexAt(50, count: 0));
        }
    }
}
