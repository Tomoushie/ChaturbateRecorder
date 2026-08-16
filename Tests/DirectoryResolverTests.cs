using System;
using System.IO;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Repli des dossiers de travail au demarrage.
    ///
    /// Ces tests existent a cause d'un plantage reel : un utilisateur ayant tout
    /// installe correctement voyait « erreur fatale » au lancement, parce que le
    /// dossier de capture par defaut designait un disque qu'il n'avait pas.
    /// </summary>
    public class DirectoryResolverTests : IDisposable
    {
        private readonly string _root =
            Path.Combine(Path.GetTempPath(), "cr-tests-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
            catch { /* nettoyage au mieux */ }
        }

        /// <summary>Une lettre de lecteur qui n'existe pas — le cas rapporte.</summary>
        private const string MissingDrive = @"Q:\definitely-not-here\videos";

        [Fact]
        public void AReachableFolderIsCreatedAndReturnedUnchanged()
        {
            var wanted = Path.Combine(_root, "capture");

            var result = DirectoryResolver.EnsureOrFallback(wanted, Path.Combine(_root, "fb"), _root, out var fellBack);

            Assert.Equal(wanted, result);
            Assert.False(fellBack);
            Assert.True(Directory.Exists(wanted));
        }

        /// <summary>
        /// LE cas du plantage : avant, cette situation levait
        /// DirectoryNotFoundException et tuait l'application au demarrage.
        /// </summary>
        [Fact]
        public void AMissingDriveFallsBackInsteadOfThrowing()
        {
            var fallback = Path.Combine(_root, "fallback");

            var result = DirectoryResolver.EnsureOrFallback(MissingDrive, fallback, _root, out var fellBack);

            Assert.Equal(fallback, result);
            Assert.True(fellBack);
            Assert.True(Directory.Exists(fallback));
        }

        /// <summary>
        /// Un reglage vide ou blanc ne doit pas etre traite comme un chemin :
        /// Directory.CreateDirectory("") leve ArgumentException, ce qui aurait
        /// produit le meme plantage par une autre porte.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AnEmptySettingFallsBack(string? wanted)
        {
            var fallback = Path.Combine(_root, "fallback");

            var result = DirectoryResolver.EnsureOrFallback(wanted, fallback, _root, out var fellBack);

            Assert.Equal(fallback, result);
            Assert.True(fellBack);
        }

        /// <summary>
        /// Dernier recours : si meme le repli est injoignable, on rend le
        /// dossier de l'application, qui existe forcement puisqu'on s'y execute.
        /// Sans ce troisieme niveau, un profil utilisateur indisponible
        /// ramenerait le plantage d'origine.
        /// </summary>
        [Fact]
        public void WhenEvenTheFallbackFailsTheLastResortIsReturned()
        {
            var result = DirectoryResolver.EnsureOrFallback(
                MissingDrive, @"Q:\also-not-here", _root, out var fellBack);

            Assert.Equal(_root, result);
            Assert.True(fellBack);
        }

        /// <summary>
        /// Les valeurs par defaut ne doivent plus contenir de chemin propre a
        /// une machine. Ce test echouerait si quelqu'un remettait un jour
        /// « E:\... » en dur, ce qui est precisement ce qui s'est passe.
        /// </summary>
        [Fact]
        public void DefaultFoldersLiveUnderTheCurrentUserProfile()
        {
            var capture = AppConfig.DefaultCaptureDir();
            var logs = AppConfig.DefaultLogDir();

            Assert.DoesNotContain(@"E:\Streamlink", capture, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"E:\Streamlink", logs, StringComparison.OrdinalIgnoreCase);

            // Chacun doit etre creable ici et maintenant, sur n'importe quelle
            // machine qui execute la suite de tests.
            Assert.True(DirectoryResolver.EnsureOrFallback(capture, capture, capture, out var f1) == capture && !f1);
            Assert.True(DirectoryResolver.EnsureOrFallback(logs, logs, logs, out var f2) == logs && !f2);
        }
    }
}
