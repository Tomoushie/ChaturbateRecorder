using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Classification de l'état d'un salon (88.0 / 4.3 « Surveillance »).
    ///
    /// Les sorties utilisées ici ne sont pas inventées : elles ont été relevées
    /// sur le vrai yt-dlp contre le vrai site le 2026-08-05, avant d'écrire une
    /// ligne de cette fonctionnalité — précisément la vérification qui avait
    /// manqué à l'épisode 92.0.
    /// </summary>
    public class RoomStatusCheckerTests
    {
        /// <summary>
        /// Sortie d'erreur relevée telle quelle pour un salon qui ne diffuse pas.
        /// </summary>
        private const string RealOfflineStderr =
            "ERROR: [Chaturbate] krissone: Room is currently offline";

        /// <summary>
        /// Mesuré, pas supposé : un salon effectivement en diffusion rend
        /// **rc=0 avec une sortie d'erreur vide** (relevé le 2026-08-05 sur un
        /// salon en ligne, après avoir relevé le cas hors ligne sur deux
        /// autres). Les deux branches du classifieur sont donc constatées.
        /// </summary>
        [Fact]
        public void ExitCodeZeroMeansOnline()
        {
            Assert.Equal(RoomStatus.Online, RoomStatusChecker.Classify(0, ""));
        }

        [Fact]
        public void TheRealOfflineMessageIsRecognised()
        {
            Assert.Equal(RoomStatus.Offline, RoomStatusChecker.Classify(1, RealOfflineStderr));
        }

        /// <summary>
        /// Le garde-fou central : tout échec dont on ne comprend pas la cause
        /// doit rester Unknown. Le traiter comme Offline ferait attendre
        /// indéfiniment un salon banni ; le traiter comme Online déclencherait
        /// un enregistrement sur une panne réseau.
        /// </summary>
        [Theory]
        [InlineData("ERROR: unable to download webpage: <urlopen error [Errno 11001] getaddrinfo failed>")]
        [InlineData("ERROR: [Chaturbate] room: This room has been banned")]
        [InlineData("ERROR: [Chaturbate] room: Video unavailable in your country")]
        [InlineData("")]
        public void AnyOtherFailureStaysUnknown(string stderr)
        {
            Assert.Equal(RoomStatus.Unknown, RoomStatusChecker.Classify(1, stderr));
        }

        [Fact]
        public void NullStandardErrorDoesNotThrow()
        {
            Assert.Equal(RoomStatus.Unknown, RoomStatusChecker.Classify(1, null!));
        }

        /// <summary>
        /// yt-dlp écrit son message avec la casse de l'extracteur ; ne pas
        /// dépendre d'une casse exacte, qui a déjà changé entre versions.
        /// </summary>
        [Fact]
        public void OfflineDetectionIsCaseInsensitive()
        {
            Assert.Equal(RoomStatus.Offline,
                RoomStatusChecker.Classify(1, "ERROR: room IS CURRENTLY OFFLINE"));
        }

        /// <summary>
        /// Un code de sortie non nul accompagné du marqueur reste Offline quel
        /// que soit ce code : yt-dlp ne garantit pas 1 en particulier.
        /// </summary>
        [Fact]
        public void OfflineWinsOverTheParticularExitCode()
        {
            Assert.Equal(RoomStatus.Offline, RoomStatusChecker.Classify(2, RealOfflineStderr));
            Assert.Equal(RoomStatus.Offline, RoomStatusChecker.Classify(101, RealOfflineStderr));
        }
    }
}
