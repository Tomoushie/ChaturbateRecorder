using System;
using System.IO;
using ChaturbateRecorder.Security;
using Xunit;

namespace ChaturbateRecorder.Security.Tests
{
    public class AclValidatorTests
    {
        [Fact]
        public void TryFindBroadWriteAccess_ReturnsFalseForMissingDirectory()
        {
            var missing = Path.Combine(Path.GetTempPath(), "acl-missing-" + Guid.NewGuid().ToString("N"));
            Assert.False(AclValidator.TryFindBroadWriteAccess(missing, out var details));
            Assert.Equal("", details);
        }

        [Fact]
        public void TryFindBroadWriteAccess_DoesNotThrowOnOwnTempDirectory()
        {
            // Pas d'assertion sur le résultat (dépend des ACL réelles de la machine
            // qui exécute le test) : on vérifie seulement que l'appel est sûr et
            // ne lève jamais d'exception sur un dossier ordinaire existant.
            var dir = Path.Combine(Path.GetTempPath(), "acl-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var exception = Record.Exception(() => AclValidator.TryFindBroadWriteAccess(dir, out _));
                Assert.Null(exception);
            }
            finally
            {
                Directory.Delete(dir);
            }
        }
    }
}
