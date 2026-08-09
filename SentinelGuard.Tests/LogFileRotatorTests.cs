using System;
using System.IO;
using System.Linq;
using SentinelGuard;
using Xunit;

namespace SentinelGuard.Tests
{
    public class LogFileRotatorTests : IDisposable
    {
        private readonly string _dir;

        public LogFileRotatorTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "sentinelguard-rotator-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string WriteFile(string name, int bytes, DateTime? lastWrite = null)
        {
            var path = Path.Combine(_dir, name);
            File.WriteAllBytes(path, new byte[bytes]);
            if (lastWrite.HasValue) File.SetLastWriteTime(path, lastWrite.Value);
            return path;
        }

        [Fact]
        public void RotatesAFileOverTheThreshold()
        {
            var path = WriteFile("session.jsonl", 2048);

            Assert.True(LogFileRotator.RotateIfTooLarge(path, 1024));

            // Le nom d'origine doit être LIBRE : l'appelant rouvre aussitôt un
            // fichier vide dessous, c'est tout l'intérêt de la manoeuvre.
            Assert.False(File.Exists(path));
            Assert.Single(Directory.GetFiles(_dir));
            Assert.StartsWith("session-", Path.GetFileName(Directory.GetFiles(_dir)[0]), StringComparison.Ordinal);
        }

        [Fact]
        public void LeavesASmallFileAlone()
        {
            var path = WriteFile("session.jsonl", 100);

            Assert.False(LogFileRotator.RotateIfTooLarge(path, 1024, out var reason));
            Assert.True(File.Exists(path));
            Assert.False(string.IsNullOrEmpty(reason));
        }

        [Fact]
        public void ZeroThresholdDisablesRotation()
        {
            var path = WriteFile("session.jsonl", 5000);

            // 0 = « pas de limite ». Traiter cette valeur comme un seuil ferait
            // tourner le fichier à chaque écriture.
            Assert.False(LogFileRotator.RotateIfTooLarge(path, 0));
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void MissingFileIsNotAnError()
        {
            Assert.False(LogFileRotator.RotateIfTooLarge(Path.Combine(_dir, "absent.log"), 10, out var reason));
            Assert.False(string.IsNullOrEmpty(reason));
        }

        [Fact]
        public void LockedFileIsReportedRatherThanThrown()
        {
            var path = WriteFile("locked.log", 4096);
            using var handle = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);

            // Cas le plus fréquent en production : le fichier est encore ouvert
            // en écriture. Ça ne doit jamais remonter en exception jusqu'à
            // l'appelant, qui est en train d'écrire une ligne de log.
            Assert.False(LogFileRotator.RotateIfTooLarge(path, 1024, out var reason));
            Assert.False(string.IsNullOrEmpty(reason));
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void PurgeDeletesOnlyOldFiles()
        {
            WriteFile("recent.log", 10, DateTime.Now.AddDays(-1));
            WriteFile("old.log", 10, DateTime.Now.AddDays(-40));

            var deleted = LogFileRotator.PurgeOlderThan(_dir, 30);

            Assert.Equal(1, deleted);
            Assert.Equal(new[] { "recent.log" }, Directory.GetFiles(_dir).Select(Path.GetFileName).ToArray());
        }

        [Fact]
        public void PurgeWithZeroAgeDeletesNothing()
        {
            WriteFile("old.log", 10, DateTime.Now.AddYears(-2));

            // Garde-fou volontaire : un réglage mal renseigné (0) ne doit pas
            // vider le dossier de logs, il doit ne rien faire.
            Assert.Equal(0, LogFileRotator.PurgeOlderThan(_dir, 0));
            Assert.Single(Directory.GetFiles(_dir));
        }

        [Fact]
        public void PurgeOnAMissingDirectoryIsHarmless()
        {
            Assert.Equal(0, LogFileRotator.PurgeOlderThan(Path.Combine(_dir, "nope"), 30));
        }
    }
}
