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
            Assert.Equal(RoomStatus.Online, RoomStatusChecker.Classify(0, "", ""));
        }

        [Fact]
        public void TheRealOfflineMessageIsRecognised()
        {
            Assert.Equal(RoomStatus.Offline, RoomStatusChecker.Classify(1, "", RealOfflineStderr));
        }

        // --- 40.0 : autres plateformes. Sorties relevées sur le vrai yt-dlp le
        // 2026-08-09, avant d'écrire la prise en charge.

        [Theory]
        [InlineData("ERROR: [twitch:stream] twitch: The channel is not currently live")]
        [InlineData("ERROR: [tiktok:live] tiktok: The channel is not currently live")]
        public void TwitchAndTikTokOfflineMessagesAreRecognised(string stderr)
        {
            // Sans ces marqueurs, la surveillance retombait en Unknown sur ces
            // deux plateformes — et ne déclenchait donc JAMAIS rien, sans que
            // rien ne le signale (le mode de panne de la v1.26.1).
            Assert.Equal(RoomStatus.Offline, RoomStatusChecker.Classify(1, "", stderr));
        }

        [Fact]
        public void ANonExistentChannelIsNotConfusedWithAnOfflineOne()
        {
            // Twitch le dit, Chaturbate non. Là où l'information existe, il faut
            // s'en servir : attendre indéfiniment une faute de frappe est le
            // défaut que la v1.25.0 avait dû documenter faute de pouvoir le
            // corriger.
            const string stderr =
                "ERROR: [twitch:stream] ce_salon_nexiste_pas: ce_salon_nexiste_pas does not exist";
            Assert.Equal(RoomStatus.NotFound, RoomStatusChecker.Classify(1, "", stderr));
        }

        [Theory]
        [InlineData("live_status=not_live")]
        [InlineData("live_status=was_live")]
        [InlineData("live_status=post_live")]
        [InlineData("live_status=is_upcoming")]
        public void YouTubeVideosThatAreNotLiveAreNotOnline(string stdout)
        {
            // LE piège de 40.0, mesuré : YouTube rend le code 0 même sur une
            // vidéo ordinaire. S'en tenir au code de sortie aurait fait
            // « surveiller » une VOD et lancer un enregistrement immédiat.
            Assert.Equal(RoomStatus.Offline, RoomStatusChecker.Classify(0, stdout, ""));
        }

        [Fact]
        public void AnActualLiveIsOnline()
        {
            Assert.Equal(RoomStatus.Online, RoomStatusChecker.Classify(0, "live_status=is_live", ""));
        }

        [Fact]
        public void AMissingLiveStatusFallsBackToTheExitCode()
        {
            // yt-dlp imprime "NA" quand l'extracteur ne renseigne pas le champ.
            // Le traiter comme « pas en direct » casserait Chaturbate, qui
            // fonctionnait très bien sur le seul code de sortie.
            Assert.Equal(RoomStatus.Online, RoomStatusChecker.Classify(0, "live_status=NA", ""));
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
            Assert.Equal(RoomStatus.Unknown, RoomStatusChecker.Classify(1, "", stderr));
        }

        [Fact]
        public void NullStandardErrorDoesNotThrow()
        {
            Assert.Equal(RoomStatus.Unknown, RoomStatusChecker.Classify(1, null!, null!));
        }

        /// <summary>
        /// yt-dlp écrit son message avec la casse de l'extracteur ; ne pas
        /// dépendre d'une casse exacte, qui a déjà changé entre versions.
        /// </summary>
        [Fact]
        public void OfflineDetectionIsCaseInsensitive()
        {
            Assert.Equal(RoomStatus.Offline,
                RoomStatusChecker.Classify(1, "", "ERROR: room IS CURRENTLY OFFLINE"));
        }

        /// <summary>
        /// Un code de sortie non nul accompagné du marqueur reste Offline quel
        /// que soit ce code : yt-dlp ne garantit pas 1 en particulier.
        /// </summary>
        [Fact]
        public void OfflineWinsOverTheParticularExitCode()
        {
            Assert.Equal(RoomStatus.Offline, RoomStatusChecker.Classify(2, "", RealOfflineStderr));
            Assert.Equal(RoomStatus.Offline, RoomStatusChecker.Classify(101, "", RealOfflineStderr));
        }
    }
}
