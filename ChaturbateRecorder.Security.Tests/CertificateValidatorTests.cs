using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ChaturbateRecorder.Security;
using Xunit;

namespace ChaturbateRecorder.Security.Tests
{
    public class CertificateValidatorTests
    {
        private static X509Certificate2 CreateSelfSignedCert(params string[] sanDnsNames)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var name in sanDnsNames)
                sanBuilder.AddDnsName(name);
            request.CertificateExtensions.Add(sanBuilder.Build());

            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        [Fact]
        public void VerifySubjectAlternativeName_AcceptsExactMatch()
        {
            using var cert = CreateSelfSignedCert("chaturbate.com");
            Assert.True(CertificateValidator.VerifySubjectAlternativeName(cert, "chaturbate.com"));
        }

        [Fact]
        public void VerifySubjectAlternativeName_AcceptsWildcardMatch()
        {
            using var cert = CreateSelfSignedCert("*.chaturbate.com");
            Assert.True(CertificateValidator.VerifySubjectAlternativeName(cert, "fr.chaturbate.com"));
        }

        [Fact]
        public void VerifySubjectAlternativeName_WildcardDoesNotMatchBareDomain()
        {
            // *.chaturbate.com ne doit matcher qu'un sous-domaine, pas le domaine nu.
            using var cert = CreateSelfSignedCert("*.chaturbate.com");
            Assert.False(CertificateValidator.VerifySubjectAlternativeName(cert, "chaturbate.com"));
        }

        [Fact]
        public void VerifySubjectAlternativeName_RejectsUnrelatedHost()
        {
            using var cert = CreateSelfSignedCert("chaturbate.com");
            Assert.False(CertificateValidator.VerifySubjectAlternativeName(cert, "evil.com"));
        }

        [Fact]
        public void VerifySubjectAlternativeName_RejectsWhenNoSanExtension()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=NoSan", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

            Assert.False(CertificateValidator.VerifySubjectAlternativeName(cert, "chaturbate.com"));
        }

        [Fact]
        public void VerifySubjectAlternativeName_OutReason_IsPopulatedOnRejection()
        {
            using var cert = CreateSelfSignedCert("chaturbate.com");
            Assert.False(CertificateValidator.VerifySubjectAlternativeName(cert, "evil.com", out var reason));
            Assert.False(string.IsNullOrEmpty(reason));
        }
    }
}
