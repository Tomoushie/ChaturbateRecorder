using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.Security
{
    /// <summary>
    /// Vérification d'intégrité des binaires externes (yt-dlp.exe, ffmpeg.exe) :
    /// hash SHA256, signature Authenticode, chaîne de certification, et
    /// pinning CA optionnel.
    /// Équivalent de Test-FileHash / Test-TrustedBinary / Test-CACertificate
    /// côté PowerShell.
    /// </summary>
    public static class BinaryVerifier
    {
        public static bool VerifyFileHash(string filePath, string expectedHashHex)
        {
            if (string.IsNullOrWhiteSpace(expectedHashHex))
            {
                Logger.Log($"Aucun hash attendu fourni pour '{filePath}' : vérification refusée par sécurité.", LogLevel.ERROR);
                return false;
            }

            var actualHex = ComputeSha256(filePath);
            return actualHex != null && string.Equals(actualHex, expectedHashHex.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Calcule le hash SHA256 d'un fichier, sans le comparer à quoi que ce
        /// soit. Utilisé pour afficher/enregistrer un hash "de confiance à la
        /// première utilisation" (voir Services/TrustedBinaryStore.cs).
        /// </summary>
        public static string? ComputeSha256(string filePath)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                return Convert.ToHexString(sha256.ComputeHash(stream));
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors du calcul du hash du fichier : {ex.Message}", LogLevel.ERROR);
                return null;
            }
        }

        public static bool VerifyTrustedBinary(
            string filePath, string expectedSha256, bool requireAuthenticode,
            string expectedSignerThumbprint, string expectedSignerSubject)
        {
            if (!File.Exists(filePath))
            {
                Logger.Log($"Binaire introuvable : {filePath}", LogLevel.ERROR);
                return false;
            }

            if (!VerifyFileHash(filePath, expectedSha256))
            {
                Logger.Log($"Hachage SHA256 invalide pour {filePath}", LogLevel.ERROR);
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
                    Logger.Log($"Échec de la chaîne de certification pour {filePath}", LogLevel.ERROR);
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
                        Logger.Log($"Statut de chaîne problématique pour {filePath} : {status.Status}", LogLevel.ERROR);
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(expectedSignerThumbprint))
                {
                    var actual = signerCert.Thumbprint?.ToUpperInvariant().Replace(" ", "");
                    var expected = expectedSignerThumbprint.ToUpperInvariant().Replace(" ", "");
                    if (actual != expected)
                    {
                        Logger.Log($"Thumbprint du signataire inattendu pour {filePath}", LogLevel.ERROR);
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(expectedSignerSubject) && signerCert.Subject != expectedSignerSubject)
                {
                    Logger.Log($"Sujet du signataire inattendu pour {filePath}", LogLevel.ERROR);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la vérification Authenticode de {filePath} : {ex.Message}", LogLevel.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Pinning CA local : compare le certificat signataire du binaire à un
        /// thumbprint/issuer attendu. Note : ceci épingle le certificat FEUILLE
        /// du signataire, pas une CA racine au sens strict — même sémantique
        /// que Test-CACertificate côté PowerShell.
        /// </summary>
        public static bool VerifyCaPinning(string filePath, string expectedThumbprint, string expectedIssuer)
        {
            try
            {
                if (string.IsNullOrEmpty(expectedThumbprint))
                {
                    Logger.Log($"Aucun thumbprint CA attendu fourni pour {filePath} : vérification refusée par sécurité.", LogLevel.ERROR);
                    return false;
                }

#pragma warning disable SYSLIB0057
                using var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
#pragma warning restore SYSLIB0057

                var actual = cert.Thumbprint?.ToUpperInvariant().Replace(" ", "");
                var expected = expectedThumbprint.ToUpperInvariant().Replace(" ", "");
                if (actual != expected)
                {
                    Logger.Log($"Thumbprint CA invalide pour {filePath}. Attendu : {expected} / Obtenu : {actual}", LogLevel.ERROR);
                    return false;
                }

                if (!string.IsNullOrEmpty(expectedIssuer) && cert.Issuer != expectedIssuer)
                {
                    Logger.Log($"Émetteur CA inattendu pour {filePath}.", LogLevel.ERROR);
                    return false;
                }

                if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter)
                {
                    Logger.Log($"Certificat CA hors période de validité pour {filePath}.", LogLevel.ERROR);
                    return false;
                }

                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);

                if (!chain.Build(cert))
                {
                    Logger.Log($"Échec de construction de la chaîne CA pour {filePath}.", LogLevel.ERROR);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la vérification CA pour {filePath} : {ex.Message}", LogLevel.ERROR);
                return false;
            }
        }
    }
}
