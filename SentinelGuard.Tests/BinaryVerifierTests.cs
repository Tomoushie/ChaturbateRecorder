using System;
using System.IO;
using System.Security.Cryptography;
using SentinelGuard;
using Xunit;

namespace SentinelGuard.Tests
{
    public class BinaryVerifierTests
    {
        [Fact]
        public void VerifyFileHash_AcceptsMatchingHash()
        {
            var path = WriteTempFile("contenu de test pour hash"u8.ToArray());
            try
            {
                var expected = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
                Assert.True(BinaryVerifier.VerifyFileHash(path, expected));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void VerifyFileHash_IsCaseInsensitive()
        {
            var path = WriteTempFile("autre contenu"u8.ToArray());
            try
            {
                var expected = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
                Assert.True(BinaryVerifier.VerifyFileHash(path, expected));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void VerifyFileHash_RejectsWrongHash()
        {
            var path = WriteTempFile("contenu original"u8.ToArray());
            try
            {
                var wrongHash = Convert.ToHexString(SHA256.HashData("contenu different"u8.ToArray()));
                Assert.False(BinaryVerifier.VerifyFileHash(path, wrongHash));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void VerifyFileHash_RejectsEmptyExpectedHash_FailClosed()
        {
            var path = WriteTempFile("peu importe"u8.ToArray());
            try
            {
                // Comportement volontaire (fail-closed) : un hash attendu vide ne
                // doit jamais être traité comme "vérification désactivée".
                Assert.False(BinaryVerifier.VerifyFileHash(path, ""));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void VerifyFileHash_ReturnsFalseForMissingFile()
        {
            var missing = Path.Combine(Path.GetTempPath(), "bv-missing-" + Guid.NewGuid().ToString("N") + ".exe");
            Assert.False(BinaryVerifier.VerifyFileHash(missing, "AABBCC"));
        }

        [Fact]
        public void VerifyFileHash_OutReason_IsPopulatedOnRejection()
        {
            var path = WriteTempFile("peu importe"u8.ToArray());
            try
            {
                Assert.False(BinaryVerifier.VerifyFileHash(path, "", out var reason));
                Assert.False(string.IsNullOrEmpty(reason));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void VerifyFileHash_OutReason_NamesBothHashes()
        {
            var path = WriteTempFile("contenu original"u8.ToArray());
            try
            {
                var wrongHash = Convert.ToHexString(SHA256.HashData("contenu different"u8.ToArray()));
                Assert.False(BinaryVerifier.VerifyFileHash(path, wrongHash, out var reason));

                // Un « hash incorrect » sans les deux valeurs oblige l'appelant à
                // les recalculer à la main pour savoir s'il tient une mise à jour
                // légitime de l'outil ou un fichier remplacé.
                Assert.Contains(wrongHash, reason, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), reason!,
                    StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ComputeSha256_MatchesTheReferenceImplementation()
        {
            var path = WriteTempFile("contenu a empreinter"u8.ToArray());
            try
            {
                var expected = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
                Assert.Equal(expected, BinaryVerifier.ComputeSha256(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ComputeSha256_ReturnsNullRatherThanThrowing()
        {
            var missing = Path.Combine(Path.GetTempPath(), "bv-missing-" + Guid.NewGuid().ToString("N") + ".exe");

            // C'est la brique de la confiance à la première utilisation : elle
            // est appelée sur un fichier que l'application n'a pas encore
            // approuvé, donc éventuellement absent. Lever ferait remonter une
            // exception jusqu'à un dialogue d'approbation.
            Assert.Null(BinaryVerifier.ComputeSha256(missing, out var reason));
            Assert.False(string.IsNullOrEmpty(reason));
        }

        private static string WriteTempFile(byte[] content)
        {
            var path = Path.Combine(Path.GetTempPath(), "bv-test-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(path, content);
            return path;
        }
    }
}
