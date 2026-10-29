using System.Linq;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Activation en ligne (2.2.0) : ce qui est testable sans réseau.
    ///
    /// Le Worker et le .NET ont chacun leurs tests pour la signature ; ici on
    /// couvre la seule partie qui décide de ce qu'un acheteur LIT quand le
    /// service refuse. Une faute de frappe dans cette table afficherait une clé
    /// brute (« premium.onlineInvalidKey ») au milieu d'une fenêtre, au pire
    /// moment — juste après un achat.
    /// </summary>
    public class OnlineActivationTests
    {
        [Theory]
        [InlineData("invalid_key", "premium.onlineInvalidKey")]
        [InlineData("invalid_request", "premium.onlineInvalidKey")]
        [InlineData("transfer_required", "premium.onlineTransfer")]
        [InlineData("rate_limited", "premium.onlineRateLimited")]
        [InlineData("itch_unavailable", "premium.onlineUnavailable")]
        [InlineData("signing_unavailable", "premium.onlineUnavailable")]
        [InlineData("origin_forbidden", "premium.onlineUnavailable")]
        public void ChaqueCodeDuWorkerALaCleAttendue(string code, string cle)
        {
            Assert.Equal(cle, OnlineActivation.CleErreur(code));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("code_inconnu_du_futur")]
        public void UnCodeInconnuTombeSurLeMessageGenerique(string? code)
        {
            Assert.Equal("premium.onlineUnavailable", OnlineActivation.CleErreur(code));
        }

        /// <summary>
        /// Toutes les clés retournées par le mapper doivent exister : sans ce
        /// test, une clé renommée côté Localization.cs ne casserait que le jour
        /// où un serveur répond — jamais en intégration continue.
        /// </summary>
        [Fact]
        public void ToutesLesClesDuMapperExistentEnFrancaisEtEnAnglais()
        {
            var codes = new[]
            {
                "invalid_key", "invalid_request", "transfer_required",
                "rate_limited", "itch_unavailable", "signing_unavailable",
                "origin_forbidden", "inconnu",
            };

            foreach (var cle in codes.Select(OnlineActivation.CleErreur).Distinct())
            {
                Assert.True(Localization.AllStrings.ContainsKey(cle), $"Clé de traduction absente : '{cle}'.");

                var paire = Localization.AllStrings[cle];
                Assert.False(string.IsNullOrWhiteSpace(paire.Fr));
                Assert.False(string.IsNullOrWhiteSpace(paire.En));
            }
        }
    }
}
