using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Carte de salon (97.0 étape 2b). Trois choses s'y testent sans écran :
    /// la traduction d'un état en couleur, la hauteur qu'elle prend, et la
    /// zone cliquable de son interrupteur.
    /// </summary>
    public class RoomCardTests
    {
        private static readonly ThemeManager.Palette Sombre = ThemeManager.GetPalette(AppTheme.Dark);
        private static readonly ThemeManager.Palette Clair = ThemeManager.GetPalette(AppTheme.Light);

        /// <summary>
        /// Une couleur d'état qui ment est pire que pas de couleur du tout :
        /// c'est ce qu'on lit en premier, avant même le libellé.
        /// </summary>
        [Fact]
        public void EachStateGetsTheColourItsMeaningCalls()
        {
            Assert.Equal(Sombre.Success, RoomCard.StateColor(RoomRowState.Live, Sombre));
            Assert.Equal(Sombre.Accent, RoomCard.StateColor(RoomRowState.Recording, Sombre));
            Assert.Equal(Sombre.Warning, RoomCard.StateColor(RoomRowState.Reconnecting, Sombre));
            Assert.Equal(Sombre.Danger, RoomCard.StateColor(RoomRowState.Failed, Sombre));
            Assert.Equal(Sombre.FgMuted, RoomCard.StateColor(RoomRowState.Idle, Sombre));
        }

        /// <summary>
        /// « Introuvable » est un échec DÉFINITIF, pas une panne passagère.
        /// Le confondre avec « hors ligne » ferait attendre indéfiniment une
        /// faute de frappe — exactement le défaut que RoomStatus.NotFound avait
        /// été créé pour éviter en 40.0.
        /// </summary>
        [Fact]
        public void NotFoundIsNotDressedAsAnOrdinaryOfflineRoom()
        {
            Assert.NotEqual(RoomCard.StateColor(RoomRowState.Idle, Sombre),
                            RoomCard.StateColor(RoomRowState.NotFound, Sombre));
        }

        [Theory]
        [InlineData(RoomRowState.Recording, true)]
        [InlineData(RoomRowState.Reconnecting, true)]
        [InlineData(RoomRowState.Live, false)]
        [InlineData(RoomRowState.Idle, false)]
        [InlineData(RoomRowState.Finished, false)]
        [InlineData(RoomRowState.Failed, false)]
        [InlineData(RoomRowState.NotFound, false)]
        [InlineData(RoomRowState.Unknown, false)]
        public void OnlyStatesWithSomethingToShowTakeExtraHeight(RoomRowState etat, bool attendu)
        {
            // Sans cette règle, vingt salons hors ligne occuperaient la hauteur
            // de vingt enregistrements en cours.
            Assert.Equal(attendu, RoomCard.IsExpanded(etat));
        }

        /// <summary>
        /// L'interrupteur doit rester dans la carte et ne jamais chevaucher la
        /// zone des boutons d'action : un interrupteur qui ne réagit pas là où
        /// il est dessiné se prend pour une panne.
        /// </summary>
        [Theory]
        [InlineData(600, 200)]
        [InlineData(900, 250)]
        [InlineData(1400, 250)]
        public void TheToggleStaysInsideTheCardAndClearOfTheActions(int largeur, int actions)
        {
            var rect = RoomCard.ToggleBounds(largeur, RoomCard.CompactHeight, actions);

            Assert.True(rect.Left > 0, "l'interrupteur sort par la gauche");
            Assert.True(rect.Right <= largeur - actions, "l'interrupteur empiète sur les boutons");
            Assert.True(rect.Top >= 0 && rect.Bottom <= RoomCard.CompactHeight, "l'interrupteur sort en hauteur");
        }

        /// <summary>
        /// Une carte étendue garde son interrupteur à la même place qu'une carte
        /// compacte : il ne doit pas glisser vers le bas quand l'enregistrement
        /// démarre, sinon la souris rate sa cible au moment précis où l'état
        /// change.
        /// </summary>
        [Fact]
        public void TheToggleDoesNotMoveWhenTheCardExpands()
        {
            Assert.Equal(
                RoomCard.ToggleBounds(800, RoomCard.CompactHeight, 250),
                RoomCard.ToggleBounds(800, RoomCard.ExpandedHeight, 250));
        }

        /// <summary>
        /// Les deux nouvelles couleurs doivent être RÉELLEMENT différentes d'un
        /// thème à l'autre : reprendre le vert du thème sombre sur une carte
        /// quasi blanche tombe sous le seuil de lisibilité (mesuré à 3,97).
        /// </summary>
        [Fact]
        public void StateColoursDifferBetweenThemes()
        {
            Assert.NotEqual(Sombre.Success, Clair.Success);
            Assert.NotEqual(Sombre.Warning, Clair.Warning);
        }
    }
}
