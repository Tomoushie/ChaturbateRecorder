using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Règles de décision de la recherche de mise à jour (79.0). Seule la
    /// partie hors réseau est testée : l'appel à l'API GitHub lui-même n'a rien
    /// de testable sans connexion, alors que la comparaison de versions et la
    /// règle anti-répétition sont exactement ce qui peut être faux sans se
    /// voir — une vérification qui tourne toutes les heures notifierait alors
    /// soit en boucle, soit jamais.
    /// </summary>
    public class UpdateCheckerTests
    {
        [Theory]
        [InlineData("1.20.0", "1.19.1", true)]
        [InlineData("1.19.1", "1.19.1", false)]
        [InlineData("1.19.0", "1.19.1", false)]
        // Le piège du tri lexicographique : "1.9.0" > "1.10.0" en comparaison
        // de chaînes, alors que 1.10.0 est la version la plus récente.
        [InlineData("1.10.0", "1.9.0", true)]
        [InlineData("1.9.0", "1.10.0", false)]
        [InlineData("2.0.0", "1.99.99", true)]
        public void IsNewerComparesVersionsNumerically(string latest, string current, bool expected)
        {
            Assert.Equal(expected, UpdateChecker.IsNewer(latest, current));
        }

        [Fact]
        public void FirstDetectionAlwaysNotifies()
        {
            Assert.True(UpdateChecker.ShouldNotify("1.20.0", null));
            Assert.True(UpdateChecker.ShouldNotify("1.20.0", ""));
            Assert.True(UpdateChecker.ShouldNotify("1.20.0", "   "));
        }

        /// <summary>
        /// Le cas qui motive tout le mécanisme : sans cette règle, une version
        /// déjà signalée redéclencherait une notification à chaque passage
        /// horaire tant que l'utilisateur n'installe pas la mise à jour.
        /// </summary>
        [Fact]
        public void AlreadyNotifiedVersionDoesNotNotifyAgain()
        {
            Assert.False(UpdateChecker.ShouldNotify("1.20.0", "1.20.0"));
        }

        /// <summary>
        /// Symétrique du précédent : mémoriser un simple booléen "déjà prévenu"
        /// aurait fait taire toutes les releases suivantes jusqu'au prochain
        /// démarrage de l'application.
        /// </summary>
        [Fact]
        public void NewerVersionNotifiesEvenAfterAPreviousNotification()
        {
            Assert.True(UpdateChecker.ShouldNotify("1.21.0", "1.20.0"));
            Assert.True(UpdateChecker.ShouldNotify("1.20.0", "1.9.0"));
        }

        /// <summary>
        /// Rétrogradation côté serveur (release retirée, puis "latest" qui
        /// repointe sur une version antérieure — vu pour de vrai en 38.0 avec
        /// make_latest) : ne pas re-notifier pour une version plus ancienne que
        /// celle déjà annoncée.
        /// </summary>
        [Fact]
        public void OlderVersionThanAlreadyNotifiedStaysSilent()
        {
            Assert.False(UpdateChecker.ShouldNotify("1.19.0", "1.20.0"));
        }
    }
}
