using System;
using System.IO;
using System.Security.Cryptography;
using ChaturbateRecorderApp.Security;
using Xunit;

namespace ChaturbateRecorderApp.Tests
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

        private static string WriteTempFile(byte[] content)
        {
            var path = Path.Combine(Path.GetTempPath(), "bv-test-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(path, content);
            return path;
        }
    }
}
