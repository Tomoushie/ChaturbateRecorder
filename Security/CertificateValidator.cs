using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.Security
{
    /// <summary>
    /// Vérification TLS explicite du serveur distant (pinning optionnel) et
    /// vérification du SAN (Subject Alternative Name).
    /// Équivalent de Test-RemoteCertificate / Test-CertificateSAN côté PowerShell.
    ///
    /// Note : .NET valide déjà le nom d'hôte nativement pendant
    /// SslStream.AuthenticateAsClient (remonté via sslPolicyErrors, capté
    /// ci-dessous). La vérification SAN explicite ajoutée ici est une défense
    /// en profondeur avec un log détaillé, pas un correctif d'une faille béante.
    /// </summary>
    public static class CertificateValidator
    {
        public static bool VerifyRemoteCertificate(
            string hostName, int port, string expectedThumbprint, string expectedIssuer)
        {
            try
            {
                using var tcpClient = new TcpClient();
                tcpClient.Connect(hostName, port);

                bool chainOk = true;
                string chainError = "";

                using var sslStream = new SslStream(tcpClient.GetStream(), false,
                    (sender, certificate, chain, errors) =>
                    {
                        if (errors != SslPolicyErrors.None)
                        {
                            chainOk = false;
                            chainError = errors.ToString();
                        }
                        // On inspecte nous-mêmes le certificat ci-dessous ; on ne bloque
                        // pas ici pour pouvoir logguer un message clair en cas d'échec.
                        return true;
                    });

                sslStream.AuthenticateAsClient(hostName);

                if (!chainOk)
                {
                    Logger.Log($"Erreurs de politique SSL pour {hostName} : {chainError}", LogLevel.ERROR);
                    return false;
                }

                if (sslStream.RemoteCertificate == null)
                {
                    Logger.Log($"Aucun certificat reçu du serveur {hostName}.", LogLevel.ERROR);
                    return false;
                }

                using var remoteCert = new X509Certificate2(sslStream.RemoteCertificate);

                if (DateTime.Now < remoteCert.NotBefore || DateTime.Now > remoteCert.NotAfter)
                {
                    Logger.Log($"Certificat serveur hors période de validité pour {hostName}.", LogLevel.ERROR);
                    return false;
                }

                if (!string.IsNullOrEmpty(expectedThumbprint))
                {
                    var actual = remoteCert.Thumbprint?.ToUpperInvariant().Replace(" ", "");
                    var expected = expectedThumbprint.ToUpperInvariant().Replace(" ", "");
                    if (actual != expected)
                    {
                        Logger.Log($"Thumbprint serveur inattendu pour {hostName}. Attendu : {expected} / Obtenu : {actual}", LogLevel.ERROR);
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(expectedIssuer) && remoteCert.Issuer != expectedIssuer)
                {
                    Logger.Log($"Émetteur serveur inattendu pour {hostName}.", LogLevel.ERROR);
                    return false;
                }

                if (!VerifySubjectAlternativeName(remoteCert, hostName))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la vérification TLS de {hostName} : {ex.Message}", LogLevel.ERROR);
                return false;
            }
        }

        private static bool VerifySubjectAlternativeName(X509Certificate2 cert, string expectedHostName)
        {
            X509Extension? sanExtension = null;
            foreach (var ext in cert.Extensions)
            {
                if (ext.Oid?.Value == "2.5.29.17") { sanExtension = ext; break; }
            }

            if (sanExtension == null)
            {
                Logger.Log("Aucune extension SAN (Subject Alternative Name) trouvée sur le certificat.", LogLevel.ERROR);
                return false;
            }

            // Format(false) renvoie une chaîne du type "DNS Name=chaturbate.com, DNS Name=*.chaturbate.com"
            var sanText = sanExtension.Format(false);
            var matches = Regex.Matches(sanText, @"DNS Name=([^,;]+)");

            if (matches.Count == 0)
            {
                Logger.Log("Extension SAN présente mais aucune entrée DNS Name exploitable.", LogLevel.ERROR);
                return false;
            }

            foreach (Match m in matches)
            {
                var entry = m.Groups[1].Value.Trim();

                if (string.Equals(entry, expectedHostName, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (entry.StartsWith("*."))
                {
                    var suffix = entry.Substring(1); // ".domaine.com"
                    if (expectedHostName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                        expectedHostName.Length > suffix.Length)
                        return true;
                }
            }

            Logger.Log($"Le nom d'hôte '{expectedHostName}' ne correspond à aucune entrée SAN du certificat.", LogLevel.ERROR);
            return false;
        }
    }
}
