using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SentinelGuard
{
    /// <summary>
    /// Explicit TLS verification of a remote server (optional pinning) and
    /// Subject Alternative Name (SAN) validation.
    ///
    /// Note: .NET already validates the host name natively during
    /// SslStream.AuthenticateAsClient (surfaced through sslPolicyErrors, caught
    /// below). The explicit SAN check added here is defence in depth, not a fix
    /// for a gaping hole.
    /// </summary>
    public static class CertificateValidator
    {
        /// <summary>
        /// Opens a TLS connection to <paramref name="hostName"/> and pins the
        /// certificate it presents, on top of the platform's own chain
        /// validation. Use it to detect an intercepting proxy before sending
        /// anything sensitive.
        /// </summary>
        /// <param name="hostName">Host to connect to.</param>
        /// <param name="port">TCP port, usually 443.</param>
        /// <param name="expectedThumbprint">
        /// Expected certificate thumbprint, or empty to skip that comparison.
        /// </param>
        /// <param name="expectedIssuer">
        /// Expected issuer, or empty to skip that comparison.
        /// </param>
        /// <returns><see langword="true"/> if the presented certificate matches.</returns>
        /// <remarks>
        /// Pinning breaks when the server legitimately rotates its certificate.
        /// Plan how you will update the pinned values before enabling this.
        /// </remarks>
        public static bool VerifyRemoteCertificate(
            string hostName, int port, string expectedThumbprint, string expectedIssuer) =>
            VerifyRemoteCertificate(hostName, port, expectedThumbprint, expectedIssuer, out _);

        /// <summary>
        /// Same as
        /// <see cref="VerifyRemoteCertificate(string, int, string, string)"/>,
        /// but also reports why verification failed.
        /// </summary>
        /// <param name="hostName">Host to connect to.</param>
        /// <param name="port">TCP port, usually 443.</param>
        /// <param name="expectedThumbprint">Expected thumbprint, or empty to skip.</param>
        /// <param name="expectedIssuer">Expected issuer, or empty to skip.</param>
        /// <param name="reason">
        /// On failure, a human-readable explanation; <see langword="null"/> on success.
        /// </param>
        /// <returns><see langword="true"/> if the presented certificate matches.</returns>
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
                    reason = $"No certificate received from server {hostName}.";
                    return false;
                }

                using var remoteCert = new X509Certificate2(sslStream.RemoteCertificate);

                if (DateTime.Now < remoteCert.NotBefore || DateTime.Now > remoteCert.NotAfter)
                {
                    reason = $"Server certificate outside its validity period for {hostName}.";
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
                    reason = $"Unexpected server issuer for {hostName}.";
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
                reason = $"Error while verifying TLS for {hostName}: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Checks that a certificate's Subject Alternative Name covers
        /// <paramref name="expectedHostName"/>, including wildcard entries.
        /// </summary>
        /// <param name="cert">The certificate to inspect.</param>
        /// <param name="expectedHostName">The host name it must cover.</param>
        /// <returns><see langword="true"/> if the SAN matches the host.</returns>
        /// <remarks>
        /// The SAN extension is decoded from raw ASN.1 rather than through
        /// <c>X509Extension.Format()</c> on purpose: that method returns text
        /// localised by the OS, so parsing it silently fails on a non-English
        /// Windows. This was a real bug, found and fixed by the test suite.
        /// </remarks>
        public static bool VerifySubjectAlternativeName(X509Certificate2 cert, string expectedHostName) =>
            VerifySubjectAlternativeName(cert, expectedHostName, out _);

        /// <summary>
        /// Same as
        /// <see cref="VerifySubjectAlternativeName(X509Certificate2, string)"/>,
        /// but also reports why the name did not match.
        /// </summary>
        /// <param name="cert">The certificate to inspect.</param>
        /// <param name="expectedHostName">The host name it must cover.</param>
        /// <param name="reason">
        /// On failure, a human-readable explanation; <see langword="null"/> on success.
        /// </param>
        /// <returns><see langword="true"/> if the SAN matches the host.</returns>
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
                reason = "No Subject Alternative Name (SAN) extension found on the certificate.";
                return false;
            }

            List<string> dnsNames;
            try
            {
                dnsNames = ParseDnsSanEntries(sanExtension.RawData);
            }
            catch (Exception ex)
            {
                reason = $"Error while decoding the SAN extension: {ex.Message}";
                return false;
            }

            if (dnsNames.Count == 0)
            {
                reason = "SAN extension present but no usable DNS Name entry.";
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

            reason = $"Host name '{expectedHostName}' matches no SAN entry on the certificate.";
            return false;
        }

        /// <summary>
        /// Decodes the dNSName entries of the SAN extension (OID 2.5.29.17) through
        /// direct ASN.1 parsing (System.Formats.Asn1) rather than through
        /// X509Extension.Format(), whose output is LOCALISED to the language of the
        /// OS ("DNS Name=" in English, "Nom DNS=" in French, and so on).
        /// dNSName is encoded in GeneralName as [2] IMPLICIT IA5String.
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
