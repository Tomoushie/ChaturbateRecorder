using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    public enum ReportKind { Bug, Feature, Feedback }

    /// <summary>
    /// Issue du dépôt d'un envoi. <see cref="IssueUrl"/> n'est renseignée qu'en
    /// cas de succès ; <see cref="ErrorCode"/> qu'en cas d'échec.
    /// </summary>
    public sealed class ReportResult
    {
        public required bool Success { get; init; }
        public string IssueUrl { get; init; } = "";
        public string ErrorCode { get; init; } = "";

        public static ReportResult Ok(string url) => new() { Success = true, IssueUrl = url };
        public static ReportResult Failed(string code) => new() { Success = false, ErrorCode = code };
    }

    /// <summary>
    /// Envoie un signalement au relais (102.0), qui crée l'issue GitHub à la
    /// place de l'utilisateur — celui-ci n'a donc besoin d'aucun compte.
    ///
    /// **Les bornes sont recopiées de celles du relais, volontairement.** Les
    /// vérifier ici aussi n'est pas une duplication inutile : sans elles,
    /// quelqu'un qui tape trois caractères apprendrait son erreur après un
    /// aller-retour réseau et un message traduit par le serveur. Le relais les
    /// applique de son côté parce qu'il ne peut faire confiance à personne ;
    /// l'application les applique pour répondre tout de suite.
    ///
    /// **Aucune donnée n'est ajoutée dans le dos de l'utilisateur** : la
    /// fenêtre affiche EXACTEMENT ce qui part, contexte compris. Une issue est
    /// publique, et un rapport sur un enregistreur de cams peut contenir un nom
    /// de salon ou un chemin de fichier ; rien ne doit s'y glisser à son insu.
    /// </summary>
    public static class ReportSender
    {
        public const int TitleMin = 3;
        public const int TitleMax = 120;
        public const int BodyMin = 20;
        public const int BodyMax = 8000;

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// L'envoi depuis l'application est-il disponible ? Faux si aucun relais
        /// n'est configuré : la fenêtre n'offre alors que le chemin GitHub.
        /// </summary>
        public static bool IsConfigured =>
            AppConfig.ReportEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        public static string KindKey(ReportKind kind) => kind switch
        {
            ReportKind.Bug => "bug",
            ReportKind.Feature => "feature",
            _ => "feedback",
        };

        /// <summary>
        /// Motif de refus, ou <c>null</c> si l'envoi peut partir. Renvoie la
        /// CLÉ de traduction et non une phrase : l'appelant est une fenêtre
        /// bilingue.
        /// </summary>
        public static string? Validate(string title, string body)
        {
            var t = (title ?? "").Trim();
            var b = (body ?? "").Trim();

            if (t.Length < TitleMin) return "report.error.titleShort";
            if (t.Length > TitleMax) return "report.error.titleLong";
            if (b.Length < BodyMin) return "report.error.bodyShort";
            if (b.Length > BodyMax) return "report.error.bodyLong";
            return null;
        }

        /// <summary>
        /// Ligne de contexte jointe au signalement. **Volontairement pauvre** :
        /// système, présence des deux binaires, mode d'interface. Ni chemin de
        /// capture, ni nom de salon, ni proxy — l'issue est publique, et ces
        /// trois-là en disent long sur qui envoie.
        /// </summary>
        public static string BuildContext(bool advancedMode, UI.AppLanguage language)
        {
            string L(string key) => UI.Localization.Get(key, language);

            var parts = new List<string>
            {
                Environment.OSVersion.VersionString,
                L(SafeMode.IsEnabled(SafeComponent.Ffmpeg) ? "report.context.ffmpegOn" : "report.context.ffmpegOff"),
                L(advancedMode ? "report.context.advanced" : "report.context.simple"),
            };
            return string.Join(" · ", parts);
        }

        public static string BuildPayload(ReportKind kind, string title, string body, string version, string context) =>
            JsonSerializer.Serialize(new
            {
                type = KindKey(kind),
                title = (title ?? "").Trim(),
                body = (body ?? "").Trim(),
                version = version ?? "",
                context = context ?? "",
            });

        /// <summary>
        /// **Ne lève jamais.** Un envoi qui échoue doit produire un message et
        /// laisser la fenêtre ouverte avec le texte saisi — perdre ce que
        /// quelqu'un vient d'écrire parce que le réseau a hoqueté serait la
        /// pire façon de traiter un signalement.
        /// </summary>
        public static async Task<ReportResult> SendAsync(
            ReportKind kind, string title, string body, string version, string context,
            CancellationToken cancellation = default)
        {
            if (!IsConfigured) return ReportResult.Failed("not_configured");

            var invalid = Validate(title, body);
            if (invalid != null) return ReportResult.Failed("invalid");

            try
            {
                using var http = new HttpClient { Timeout = Timeout };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("ChaturbateRecorder-Report");

                var payload = BuildPayload(kind, title, body, version, context);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await http
                    .PostAsync(AppConfig.ReportEndpoint, content, cancellation)
                    .ConfigureAwait(false);

                var json = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
                return ParseResponse(response.IsSuccessStatusCode, json);
            }
            catch (TaskCanceledException)
            {
                Logger.Log("Signalement : délai dépassé en contactant le relais.", LogLevel.WARN);
                return ReportResult.Failed("timeout");
            }
            catch (Exception ex)
            {
                Logger.Log($"Signalement : envoi impossible ({ex.Message}).", LogLevel.WARN);
                return ReportResult.Failed("network");
            }
        }

        /// <summary>
        /// Séparée de l'envoi pour être testable sans réseau : c'est ici que se
        /// joue la correspondance entre les codes du relais et ce que lit
        /// l'utilisateur.
        /// </summary>
        public static ReportResult ParseResponse(bool httpSuccess, string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (httpSuccess
                    && root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True
                    && root.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
                {
                    return ReportResult.Ok(url.GetString() ?? "");
                }

                var code = root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String
                    ? err.GetString() ?? "unknown"
                    : "unknown";
                return ReportResult.Failed(code);
            }
            catch (JsonException)
            {
                // Réponse illisible : le relais est peut-être remplacé par une
                // page d'erreur d'intermédiaire. Ce n'est pas « tout va bien ».
                return ReportResult.Failed("unknown");
            }
        }

        /// <summary>
        /// Traduit un code de refus en clé de message. Les codes inconnus
        /// retombent sur un message générique plutôt que de s'afficher tels
        /// quels : "upstream" ne veut rien dire pour qui signale un bug.
        /// </summary>
        public static string MessageKey(string errorCode) => errorCode switch
        {
            "rate_limited" => "report.error.rateLimited",
            "daily_limit" => "report.error.dailyLimit",
            "timeout" => "report.error.timeout",
            "network" => "report.error.network",
            "not_configured" => "report.error.notConfigured",
            "too_large" => "report.error.bodyLong",
            "title_too_short" => "report.error.titleShort",
            "body_too_short" => "report.error.bodyShort",
            _ => "report.error.generic",
        };
    }
}
