using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace ChaturbateRecorder.Security
{
    /// <summary>
    /// Vérification TLS explicite d'un serveur distant (pinning optionnel) et
    /// vérification du SAN (Subject Alternative Name).
    ///
    /// Note : .NET valide déjà le nom d'hôte nativement pendant
    /// SslStream.AuthenticateAsClient (remonté via sslPolicyErrors, capté
    /// ci-dessous). La vérification SAN explicite ajoutée ici est une défense
    /// en profondeur, pas un correctif d'une faille béante.
    /// </summary>
    public static class CertificateValidator
    {
        public static bool VerifyRemoteCertificate(
            string hostName, int port, string expectedThumbprint, string expectedIssuer) =>
            VerifyRemoteCertificate(hostName, port, expectedThumbprint, expectedIssuer, out _);

        public static bool VerifyRemoteCertificate(
            string hostName, int port, string expectedThumbprint, string expectedIssuer, out string? reason)
        {
            reason = null;
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
                        // pas ici pour pouvoir renvoyer un message clair en cas d'échec.
                        return true;
                    });

                sslStream.AuthenticateAsClient(hostName);

                if (!chainOk)
                {
                    reason = $"Erreurs de politique SSL pour {hostName} : {chainError}";
                    return false;
                }

                if (sslStream.RemoteCertificate == null)
                {
                    reason = $"Aucun certificat reçu du serveur {hostName}.";
                    return false;
                }

                using var remoteCert = new X509Certificate2(sslStream.RemoteCertificate);

                if (DateTime.Now < remoteCert.NotBefore || DateTime.Now > remoteCert.NotAfter)
                {
                    reason = $"Certificat serveur hors période de validité pour {hostName}.";
                    return false;
                }

                if (!string.IsNullOrEmpty(expectedThumbprint))
                {
                    var actual = remoteCert.Thumbprint?.ToUpperInvariant().Replace(" ", "");
                    var expected = expectedThumbprint.ToUpperInvariant().Replace(" ", "");
                    if (actual != expected)
                    {
                        reason = $"Thumbprint serveur inattendu pour {hostName}. Attendu : {expected} / Obtenu : {actual}";
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(expectedIssuer) && remoteCert.Issuer != expectedIssuer)
                {
                    reason = $"Émetteur serveur inattendu pour {hostName}.";
                    return false;
                }

                if (!VerifySubjectAlternativeName(remoteCert, hostName, out var sanReason))
                {
                    reason = sanReason;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = $"Erreur lors de la vérification TLS de {hostName} : {ex.Message}";
                return false;
            }
        }

        public static bool VerifySubjectAlternativeName(X509Certificate2 cert, string expectedHostName) =>
            VerifySubjectAlternativeName(cert, expectedHostName, out _);

        public static bool VerifySubjectAlternativeName(X509Certificate2 cert, string expectedHostName, out string? reason)
        {
            reason = null;

            X509Extension? sanExtension = null;
            foreach (var ext in cert.Extensions)
            {
                if (ext.Oid?.Value == "2.5.29.17") { sanExtension = ext; break; }
            }

            if (sanExtension == null)
            {
                reason = "Aucune extension SAN (Subject Alternative Name) trouvée sur le certificat.";
                return false;
            }

            List<string> dnsNames;
            try
            {
                dnsNames = ParseDnsSanEntries(sanExtension.RawData);
            }
            catch (Exception ex)
            {
                reason = $"Erreur lors du décodage de l'extension SAN : {ex.Message}";
                return false;
            }

            if (dnsNames.Count == 0)
            {
                reason = "Extension SAN présente mais aucune entrée DNS Name exploitable.";
                return false;
            }

            foreach (var entry in dnsNames)
            {
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

            reason = $"Le nom d'hôte '{expectedHostName}' ne correspond à aucune entrée SAN du certificat.";
            return false;
        }

        /// <summary>
        /// Décode les entrées dNSName de l'extension SAN (OID 2.5.29.17) via un
        /// parsing ASN.1 direct (System.Formats.Asn1), plutôt que via
        /// X509Extension.Format() dont la sortie est LOCALISÉE selon la langue
        /// de l'OS ("DNS Name=" en anglais, "Nom DNS=" en français...).
        /// dNSName est encodé en GeneralName comme [2] IMPLICIT IA5String.
        /// </summary>
        private static List<string> ParseDnsSanEntries(byte[] rawData)
        {
            var names = new List<string>();
            var reader = new AsnReader(rawData, AsnEncodingRules.BER);
            var sequence = reader.ReadSequence();

            while (sequence.HasData)
            {
                var tag = sequence.PeekTag();
                if (tag.TagClass == TagClass.ContextSpecific && tag.TagValue == 2)
                {
                    var dnsName = sequence.ReadCharacterString(UniversalTagNumber.IA5String, tag);
                    names.Add(dnsName);
                }
                else
                {
                    sequence.ReadEncodedValue();
                }
            }

            return names;
        }
    }
}
