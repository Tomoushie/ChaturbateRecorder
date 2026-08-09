using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SentinelGuard
{
    /// <summary>
    /// Integrity checks on external executables: SHA-256 hash, Authenticode
    /// signature, certificate chain, and optional CA pinning.
    /// </summary>
    public static class BinaryVerifier
    {
        /// <summary>
        /// Computes the SHA-256 of a file and compares it, case-insensitively,
        /// to <paramref name="expectedHashHex"/>.
        /// </summary>
        /// <param name="filePath">Path to the file to hash.</param>
        /// <param name="expectedHashHex">Expected SHA-256, as a hex string.</param>
        /// <returns>
        /// <see langword="true"/> only if the file exists, is readable, and its
        /// hash matches exactly.
        /// </returns>
        public static bool VerifyFileHash(string filePath, string expectedHashHex) =>
            VerifyFileHash(filePath, expectedHashHex, out _);

        /// <summary>
        /// Same as <see cref="VerifyFileHash(string, string)"/>, but also
        /// reports why verification failed.
        /// </summary>
        /// <param name="filePath">Path to the file to hash.</param>
        /// <param name="expectedHashHex">Expected SHA-256, as a hex string.</param>
        /// <param name="reason">
        /// On failure, a human-readable explanation; <see langword="null"/> on success.
        /// </param>
        /// <returns><see langword="true"/> if the hash matches.</returns>
        public static bool VerifyFileHash(string filePath, string expectedHashHex, out string? reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(expectedHashHex))
            {
                reason = $"No expected hash provided for '{filePath}': refusing to verify (fail-closed).";
                return false;
            }

            var actualHex = ComputeSha256(filePath, out reason);
            if (actualHex == null) return false;

            if (!string.Equals(actualHex, expectedHashHex.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Hash inattendu pour '{filePath}' : {actualHex} au lieu de {expectedHashHex.Trim()}.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Computes the SHA-256 of a file, without comparing it to anything.
        /// </summary>
        /// <param name="filePath">Path to the file to hash.</param>
        /// <returns>
        /// The hash as an uppercase hex string, or <see langword="null"/> if the
        /// file could not be read.
        /// </returns>
        /// <remarks>
        /// Needed for trust-on-first-use: pinning a hash is only possible once
        /// you can read the current one. Without it, an application that pins a
        /// fixed hash blocks its own users the day the tool they rely on ships a
        /// new build — the exact failure this method was added to fix.
        /// </remarks>
        public static string? ComputeSha256(string filePath) => ComputeSha256(filePath, out _);

        /// <summary>
        /// Same as <see cref="ComputeSha256(string)"/>, but also reports why the
        /// file could not be hashed.
        /// </summary>
        /// <param name="filePath">Path to the file to hash.</param>
        /// <param name="reason">
        /// On failure, a human-readable explanation; <see langword="null"/> on success.
        /// </param>
        /// <returns>The hash as an uppercase hex string, or <see langword="null"/>.</returns>
        public static string? ComputeSha256(string filePath, out string? reason)
        {
            reason = null;

            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                return Convert.ToHexString(sha256.ComputeHash(stream));
            }
            catch (Exception ex)
            {
                reason = $"Erreur lors du calcul du hash du fichier : {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Full check on an external executable before running it: SHA-256, and
        /// optionally its Authenticode signature, signer thumbprint and signer
        /// subject.
        /// </summary>
        /// <param name="filePath">Path to the executable.</param>
        /// <param name="expectedSha256">
        /// Expected SHA-256 as hex. Pass an empty string to skip the hash check
        /// and rely on the signature alone.
        /// </param>
        /// <param name="requireAuthenticode">
        /// When <see langword="true"/>, the file must carry a valid Authenticode
        /// signature. Beware: public builds of many tools (yt-dlp, ffmpeg…) are
        /// not signed at all, so enabling this will reject them.
        /// </param>
        /// <param name="expectedSignerThumbprint">
        /// Expected signing certificate thumbprint, or empty to accept any signer.
        /// </param>
        /// <param name="expectedSignerSubject">
        /// Expected signing certificate subject, or empty to accept any subject.
        /// </param>
        /// <returns><see langword="true"/> if every requested check passed.</returns>
        public static bool VerifyTrustedBinary(
            string filePath, string expectedSha256, bool requireAuthenticode,
            string expectedSignerThumbprint, string expectedSignerSubject) =>
            VerifyTrustedBinary(filePath, expectedSha256, requireAuthenticode, expectedSignerThumbprint, expectedSignerSubject, out _);

        /// <summary>
        /// Same as
        /// <see cref="VerifyTrustedBinary(string, string, bool, string, string)"/>,
        /// but also reports which check failed.
        /// </summary>
        /// <param name="filePath">Path to the executable.</param>
        /// <param name="expectedSha256">Expected SHA-256 as hex, or empty to skip.</param>
        /// <param name="requireAuthenticode">Whether a valid signature is mandatory.</param>
        /// <param name="expectedSignerThumbprint">Expected signer thumbprint, or empty.</param>
        /// <param name="expectedSignerSubject">Expected signer subject, or empty.</param>
        /// <param name="reason">
        /// On failure, a human-readable explanation; <see langword="null"/> on success.
        /// </param>
        /// <returns><see langword="true"/> if every requested check passed.</returns>
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
                    reason = $"Certificate chain validation failed for {filePath}";
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
                        reason = $"Problematic chain status for {filePath}: {status.Status}";
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
                reason = $"Error while verifying the Authenticode signature of {filePath}: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Local CA pinning: compares the certificate that signed the binary to an
        /// expected thumbprint and issuer. Note that this pins the signer's LEAF
        /// certificate, not a root CA in the strict sense.
        /// </summary>
        /// <param name="filePath">Path to the signed executable.</param>
        /// <param name="expectedThumbprint">Expected certificate thumbprint.</param>
        /// <param name="expectedIssuer">Expected certificate issuer.</param>
        /// <returns><see langword="true"/> if the pinned certificate matches.</returns>
        public static bool VerifyCaPinning(string filePath, string expectedThumbprint, string expectedIssuer) =>
            VerifyCaPinning(filePath, expectedThumbprint, expectedIssuer, out _);

        /// <summary>
        /// Same as <see cref="VerifyCaPinning(string, string, string)"/>, but
        /// also reports why pinning failed.
        /// </summary>
        /// <param name="filePath">Path to the signed executable.</param>
        /// <param name="expectedThumbprint">Expected certificate thumbprint.</param>
        /// <param name="expectedIssuer">Expected certificate issuer.</param>
        /// <param name="reason">
        /// On failure, a human-readable explanation; <see langword="null"/> on success.
        /// </param>
        /// <returns><see langword="true"/> if the pinned certificate matches.</returns>
        public static bool VerifyCaPinning(string filePath, string expectedThumbprint, string expectedIssuer, out string? reason)
        {
            reason = null;
            try
            {
                if (string.IsNullOrEmpty(expectedThumbprint))
                {
                    reason = $"No expected CA thumbprint provided for {filePath}: refusing to verify (fail-closed).";
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
                    reason = $"Unexpected CA issuer for {filePath}.";
                    return false;
                }

                if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter)
                {
                    reason = $"CA certificate outside its validity period for {filePath}.";
                    return false;
                }

                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);

                if (!chain.Build(cert))
                {
                    reason = $"Could not build the CA chain for {filePath}.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = $"Error while checking the CA for {filePath}: {ex.Message}";
                return false;
            }
        }
    }
}
