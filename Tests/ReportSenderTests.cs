using System.Text.Json;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Envoi d'un signalement depuis l'application (102.0).
    ///
    /// Le réseau n'est jamais touché ici : ce qui se teste, c'est la forme de
    /// ce qui part, la lecture de ce qui revient, et la traduction des refus.
    /// Le relais lui-même a été éprouvé en ligne sur ses six chemins d'erreur.
    /// </summary>
    public class ReportSenderTests
    {
        [Theory]
        [InlineData("ab", "description bien assez longue pour passer", "report.error.titleShort")]
        [InlineData("   ", "description bien assez longue pour passer", "report.error.titleShort")]
        [InlineData("Un titre correct", "trop court", "report.error.bodyShort")]
        [InlineData("Un titre correct", "   ", "report.error.bodyShort")]
        public void RefusesWhatTheRelayWouldRefuseAnyway(string title, string body, string expected)
        {
            Assert.Equal(expected, ReportSender.Validate(title, body));
        }

        [Fact]
        public void AcceptsAPlausibleReport()
        {
            Assert.Null(ReportSender.Validate(
                "L'enregistrement s'arrête tout seul",
                "Quand je lance un enregistrement en MKV, il s'arrête au bout de deux minutes sans message."));
        }

        [Fact]
        public void TooLongIsRefusedOnBothFields()
        {
            Assert.Equal("report.error.titleLong",
                ReportSender.Validate(new string('t', ReportSender.TitleMax + 1), new string('b', 50)));
            Assert.Equal("report.error.bodyLong",
                ReportSender.Validate("Un titre correct", new string('b', ReportSender.BodyMax + 1)));
        }

        /// <summary>
        /// Les limites doivent rester celles du relais (report-worker/worker.js,
        /// constante LIMITS). Si elles divergent, l'application accepte un texte
        /// que le serveur refusera après un aller-retour — le pire des deux
        /// mondes, puisque l'utilisateur croit avoir envoyé.
        /// </summary>
        [Fact]
        public void LimitsMatchTheRelay()
        {
            Assert.Equal(3, ReportSender.TitleMin);
            Assert.Equal(120, ReportSender.TitleMax);
            Assert.Equal(20, ReportSender.BodyMin);
            Assert.Equal(8000, ReportSender.BodyMax);
        }

        [Fact]
        public void PayloadCarriesTheFieldsTheRelayExpects()
        {
            var json = ReportSender.BuildPayload(
                ReportKind.Feature, "  Un titre  ", "  Une description assez longue.  ", "1.34.1", "Windows 11");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal("feature", root.GetProperty("type").GetString());
            // Rognés ici et pas seulement côté serveur : un titre qui commence
            // par des espaces produirait une issue au titre décalé.
            Assert.Equal("Un titre", root.GetProperty("title").GetString());
            Assert.Equal("Une description assez longue.", root.GetProperty("body").GetString());
            Assert.Equal("1.34.1", root.GetProperty("version").GetString());
            Assert.Equal("Windows 11", root.GetProperty("context").GetString());
        }

        [Theory]
        [InlineData(ReportKind.Bug, "bug")]
        [InlineData(ReportKind.Feature, "feature")]
        [InlineData(ReportKind.Feedback, "feedback")]
        public void KindKeysAreThoseOfTheRelay(ReportKind kind, string expected)
        {
            Assert.Equal(expected, ReportSender.KindKey(kind));
        }

        [Fact]
        public void ReadsTheSuccessResponse()
        {
            var result = ReportSender.ParseResponse(true,
                """{"ok":true,"url":"https://github.com/Tomoushie/ChaturbateRecorder/issues/38","number":38}""");

            Assert.True(result.Success);
            Assert.Equal("https://github.com/Tomoushie/ChaturbateRecorder/issues/38", result.IssueUrl);
        }

        /// <summary>
        /// Un 200 sans URL n'est PAS un succès : sans elle, l'utilisateur n'a
        /// aucun moyen de retrouver son signalement, puisqu'on ne lui a demandé
        /// aucune adresse.
        /// </summary>
        [Theory]
        [InlineData(true, """{"ok":true}""")]
        [InlineData(true, """{"ok":false,"error":"upstream"}""")]
        [InlineData(false, """{"ok":false,"error":"rate_limited"}""")]
        [InlineData(true, "page html d'un intermediaire")]
        public void AnythingElseIsAFailure(bool httpSuccess, string json)
        {
            Assert.False(ReportSender.ParseResponse(httpSuccess, json).Success);
        }

        [Fact]
        public void RelayErrorCodesAreCarriedThrough()
        {
            Assert.Equal("rate_limited", ReportSender.ParseResponse(false, """{"ok":false,"error":"rate_limited"}""").ErrorCode);
            Assert.Equal("unknown", ReportSender.ParseResponse(false, "pas du json").ErrorCode);
        }

        /// <summary>
        /// Chaque code du relais doit aboutir à une phrase traduite. Un code
        /// oublié afficherait sa propre clé — « report.error.upstream » — dans
        /// une fenêtre destinée à quelqu'un qui signale un bug.
        /// </summary>
        [Theory]
        [InlineData("rate_limited")]
        [InlineData("daily_limit")]
        [InlineData("timeout")]
        [InlineData("network")]
        [InlineData("not_configured")]
        [InlineData("too_large")]
        [InlineData("title_too_short")]
        [InlineData("body_too_short")]
        [InlineData("upstream")]
        [InlineData("bad_json")]
        [InlineData("bad_type")]
        [InlineData("not_found")]
        [InlineData("method_not_allowed")]
        [InlineData("unknown")]
        [InlineData("un_code_qui_n_existe_pas_encore")]
        public void EveryRelayCodeHasATranslatedMessage(string code)
        {
            var key = ReportSender.MessageKey(code);
            Assert.NotEqual(key, Localization.Get(key, AppLanguage.French));
            Assert.NotEqual(key, Localization.Get(key, AppLanguage.English));
        }

        /// <summary>
        /// La ligne de contexte est la seule chose qui parte sans avoir été
        /// tapée. Elle ne doit contenir NI chemin de capture, NI proxy, NI nom
        /// de salon : l'issue est publique, et ces trois-là en disent long sur
        /// qui envoie.
        /// </summary>
        [Theory]
        [InlineData(AppLanguage.French, "mode avancé")]
        [InlineData(AppLanguage.English, "advanced mode")]
        public void ContextLeaksNothingPersonal(AppLanguage language, string expectedMode)
        {
            var context = ReportSender.BuildContext(advancedMode: true, language);

            Assert.DoesNotContain(":\\", context);
            Assert.DoesNotContain("/", context);
            Assert.DoesNotContain("http", context, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("proxy", context, System.StringComparison.OrdinalIgnoreCase);
            // Traduit : cette ligne s'affiche dans la fenêtre, une mention
            // française au milieu d'une interface anglaise se voit tout de suite.
            Assert.Contains(expectedMode, context);
        }
    }
}
