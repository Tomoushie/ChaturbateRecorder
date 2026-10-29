using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Config;

namespace ChaturbateRecorderApp.Services
{
    internal enum IssueActivation
    {
        Aucune,
        /// <summary>Aucun service configuré : l'application ne propose pas ce chemin.</summary>
        Desactive,
        /// <summary>Réseau, DNS, délai dépassé — le service n'a pas répondu.</summary>
        Reseau,
        /// <summary>Le service a répondu et refusé la demande (clé invalide, plafond…).</summary>
        Refusee,
        /// <summary>Le service est en panne ou mal configuré.</summary>
        Serveur,
        /// <summary>Réponse 200 mais illisible : mieux vaut ne rien installer.</summary>
        ReponseInvalide,
    }

    internal sealed class ResultatActivation
    {
        internal IssueActivation Issue { get; init; }
        internal string LicenceJson { get; init; } = "";
        internal string Nom { get; init; } = "";
        /// <summary>Code machine renvoyé par le Worker ("invalid_key", "transfer_required"…).</summary>
        internal string Code { get; init; } = "";
        /// <summary>Détail technique pour le journal, jamais affiché tel quel.</summary>
        internal string Detail { get; init; } = "";
    }

    /// <summary>
    /// Activation en ligne (2.2.0) : envoie la download key itch.io et
    /// l'identifiant d'installation au Worker, qui vérifie l'achat et renvoie
    /// un licence.key signé.
    ///
    /// **La vérification reste hors ligne** : ce service ne sert qu'une fois.
    /// Une fois `licence.key` écrit, l'application ne rappelle plus jamais le
    /// Worker pour vérifier quoi que ce soit.
    ///
    /// **La download key est un secret** : elle n'est jamais journalisée, ni
    /// conservée après la requête. Le Worker la hache avant stockage.
    /// </summary>
    internal static class OnlineActivation
    {
        private static readonly HttpClient Client = new()
        {
            // Assez long pour un réseau mobile, assez court pour ne pas laisser
            // la fenêtre bloquée sans explication.
            Timeout = TimeSpan.FromSeconds(20),
        };

        internal static async Task<ResultatActivation> ActiverAsync(
            string downloadKey, string machineId, CancellationToken annulation = default)
        {
            var endpoint = (AppConfig.ActivationEndpoint ?? "").Trim();
            if (endpoint.Length == 0) return new ResultatActivation { Issue = IssueActivation.Desactive };

            if (string.IsNullOrWhiteSpace(downloadKey) || string.IsNullOrWhiteSpace(machineId))
                return new ResultatActivation { Issue = IssueActivation.Refusee, Code = "invalid_request" };

            var corps = JsonSerializer.Serialize(new
            {
                downloadKey = downloadKey.Trim(),
                machineId = machineId.Trim(),
            });

            using var requete = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(corps, Encoding.UTF8, "application/json"),
            };
            requete.Headers.UserAgent.ParseAdd("ChaturbateRecorder/2.3.0");

            HttpResponseMessage reponse;
            try
            {
                reponse = await Client.SendAsync(requete, annulation);
            }
            catch (OperationCanceledException) when (annulation.IsCancellationRequested)
            {
                return new ResultatActivation { Issue = IssueActivation.Reseau, Detail = "annulé" };
            }
            catch (Exception ex)
            {
                return new ResultatActivation { Issue = IssueActivation.Reseau, Detail = ex.Message };
            }

            using (reponse)
            {
                string texte;
                try
                {
                    texte = await reponse.Content.ReadAsStringAsync(annulation);
                }
                catch (Exception ex)
                {
                    return new ResultatActivation { Issue = IssueActivation.Reseau, Detail = ex.Message };
                }

                try
                {
                    using var document = JsonDocument.Parse(texte);
                    return Lire(reponse, document.RootElement);
                }
                catch (JsonException)
                {
                    return new ResultatActivation
                    {
                        Issue = IssueActivation.ReponseInvalide,
                        Detail = texte.Length > 200 ? texte[..200] : texte,
                    };
                }
            }
        }

        private static ResultatActivation Lire(HttpResponseMessage reponse, JsonElement racine)
        {
            if (reponse.IsSuccessStatusCode)
            {
                if (!racine.TryGetProperty("licence", out var licence)
                    || licence.ValueKind != JsonValueKind.String)
                {
                    return new ResultatActivation { Issue = IssueActivation.ReponseInvalide };
                }

                var nom = racine.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
                    ? n.GetString() ?? ""
                    : "";

                return new ResultatActivation
                {
                    Issue = IssueActivation.Aucune,
                    LicenceJson = licence.GetString() ?? "",
                    Nom = nom,
                };
            }

            var code = racine.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String
                ? e.GetString() ?? ""
                : "";

            var issue = reponse.StatusCode is HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.InternalServerError
                or HttpStatusCode.BadGateway
                ? IssueActivation.Serveur
                : IssueActivation.Refusee;

            return new ResultatActivation
            {
                Issue = issue,
                Code = code,
                Detail = $"HTTP {(int)reponse.StatusCode}",
            };
        }

        /// <summary>
        /// Traduit un code du Worker en clé de la table de chaînes. Fonction
        /// PURE pour être testable sans réseau : le mapper d'erreurs est
        /// exactement l'endroit où une faute de frappe afficherait une clé
        /// brute à un acheteur.
        /// </summary>
        internal static string CleErreur(string? code) => code switch
        {
            "invalid_key" => "premium.onlineInvalidKey",
            "invalid_request" => "premium.onlineInvalidKey",
            "transfer_required" => "premium.onlineTransfer",
            "rate_limited" => "premium.onlineRateLimited",
            "itch_unavailable" => "premium.onlineUnavailable",
            "signing_unavailable" => "premium.onlineUnavailable",
            "origin_forbidden" => "premium.onlineUnavailable",
            _ => "premium.onlineUnavailable",
        };
    }
}
