using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ChaturbateRecorder.Security
{
    /// <summary>
    /// Vérification d'intégrité de binaires externes : hash SHA256, signature
    /// Authenticode, chaîne de certification, et pinning CA optionnel.
    /// </summary>
    public static class BinaryVerifier
    {
        public static bool VerifyFileHash(string filePath, string expectedHashHex) =>
            VerifyFileHash(filePath, expectedHashHex, out _);

        public static bool VerifyFileHash(string filePath, string expectedHashHex, out string? reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(expectedHashHex))
            {
                reason = $"Aucun hash attendu fourni pour '{filePath}' : vérification refusée par sécurité.";
                return false;
            }

            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = sha256.ComputeHash(stream);
                var actualHex = Convert.ToHexString(hashBytes);
                return string.Equals(actualHex, expectedHashHex.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                reason = $"Erreur lors du calcul du hash du fichier : {ex.Message}";
                return false;
            }
        }

        public static bool VerifyTrustedBinary(
            string filePath, string expectedSha256, bool requireAuthenticode,
            string expectedSignerThumbprint, string expectedSignerSubject) =>
            VerifyTrustedBinary(filePath, expectedSha256, requireAuthenticode, expectedSignerThumbprint, expectedSignerSubject, out _);

        public static bool VerifyTrustedBinary(
            string filePath, string expectedSha256, bool requireAuthenticode,
            string expectedSignerThumbprint, string expectedSignerSubject, out string? reason)
        {
            reason = null;

            if (!File.Exists(filePath))
            {
                reason = $"Binaire introuvable : {filePath}";
                return false;
            }

            if (!VerifyFileHash(filePath, expectedSha256))
            {
                reason = $"Hachage SHA256 invalide pour {filePath}";
                return false;
            }

            if (!requireAuthenticode) return true;

            try
            {
#pragma warning disable SYSLIB0057
                using var signerCert = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
#pragma warning restore SYSLIB0057

                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);

                if (!chain.Build(signerCert))
                {
                    reason = $"Échec de la chaîne de certification pour {filePath}";
                    return false;
                }

                foreach (var status in chain.ChainStatus)
                {
                    if (status.Status == X509ChainStatusFlags.Revoked
                        || status.Status == X509ChainStatusFlags.RevocationStatusUnknown
                        || status.Status == X509ChainStatusFlags.OfflineRevocation
                        || status.Status == X509ChainStatusFlags.UntrustedRoot
                        || status.Status == X509ChainStatusFlags.PartialChain
                        || status.Status == X509ChainStatusFlags.NotTimeValid
                        || status.Status == X509ChainStatusFlags.NotSignatureValid)
                    {
                        reason = $"Statut de chaîne problématique pour {filePath} : {status.Status}";
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(expectedSignerThumbprint))
                {
                    var actual = signerCert.Thumbprint?.ToUpperInvariant().Replace(" ", "");
                    var expected = expectedSignerThumbprint.ToUpperInvariant().Replace(" ", "");
                    if (actual != expected)
                    {
                        reason = $"Thumbprint du signataire inattendu pour {filePath}";
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(expectedSignerSubject) && signerCert.Subject != expectedSignerSubject)
                {
                    reason = $"Sujet du signataire inattendu pour {filePath}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = $"Erreur lors de la vérification Authenticode de {filePath} : {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Pinning CA local : compare le certificat signataire du binaire à un
        /// thumbprint/issuer attendu. Note : ceci épingle le certificat FEUILLE
        /// du signataire, pas une CA racine au sens strict.
        /// </summary>
        public static bool VerifyCaPinning(string filePath, string expectedThumbprint, string expectedIssuer) =>
            VerifyCaPinning(filePath, expectedThumbprint, expectedIssuer, out _);

        public static bool VerifyCaPinning(string filePath, string expectedThumbprint, string expectedIssuer, out string? reason)
        {
            reason = null;
            try
            {
                if (string.IsNullOrEmpty(expectedThumbprint))
                {
                    reason = $"Aucun thumbprint CA attendu fourni pour {filePath} : vérification refusée par sécurité.";
                    return false;
                }

#pragma warning disable SYSLIB0057
                using var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
#pragma warning restore SYSLIB0057

                var actual = cert.Thumbprint?.ToUpperInvariant().Replace(" ", "");
                var expected = expectedThumbprint.ToUpperInvariant().Replace(" ", "");
                if (actual != expected)
                {
                    reason = $"Thumbprint CA invalide pour {filePath}. Attendu : {expected} / Obtenu : {actual}";
                    return false;
                }

                if (!string.IsNullOrEmpty(expectedIssuer) && cert.Issuer != expectedIssuer)
                {
                    reason = $"Émetteur CA inattendu pour {filePath}.";
                    return false;
                }

                if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter)
                {
                    reason = $"Certificat CA hors période de validité pour {filePath}.";
                    return false;
                }

                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);

                if (!chain.Build(cert))
                {
                    reason = $"Échec de construction de la chaîne CA pour {filePath}.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = $"Erreur lors de la vérification CA pour {filePath} : {ex.Message}";
                return false;
            }
        }
    }
}
