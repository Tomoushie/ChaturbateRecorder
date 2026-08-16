using System;
using System.Linq;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Modèle unifié des salons (97.0 étape 2).
    ///
    /// **Ce qui est éprouvé ici est le seul endroit où les données de quelqu'un
    /// peuvent être perdues** : la fusion des deux anciens fichiers. Et la
    /// dérivation d'état, qui décide de ce que voit l'utilisateur — une erreur
    /// y afficherait « hors ligne » sur un enregistrement en cours.
    /// </summary>
    public class RoomStoreTests
    {
        private static readonly DateTime Maintenant = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

        // ---- Normalisation ------------------------------------------------

        [Theory]
        [InlineData("https://fr.chaturbate.com/someroom/", "https://fr.chaturbate.com/someroom")]
        [InlineData("https://FR.Chaturbate.COM/someroom", "https://fr.chaturbate.com/someroom")]
        [InlineData("  https://www.twitch.tv/streamer  ", "https://www.twitch.tv/streamer")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void NormalisesUrlsThatDesignateTheSameRoom(string? entree, string attendu)
        {
            Assert.Equal(attendu, RoomStore.Normalize(entree));
        }

        /// <summary>
        /// Deux entrées pour un même salon donneraient deux cartes, donc deux
        /// enregistrements simultanés du même flux — bande passante doublée et
        /// deux fichiers concurrents sur le même nom de base.
        /// </summary>
        [Fact]
        public void TheSameRoomWrittenTwoWaysIsOneEntry()
        {
            var rooms = RoomStore.Merge(
                new[] { "https://fr.chaturbate.com/someroom/" },
                new[] { "https://fr.chaturbate.com/someroom" },
                Maintenant);

            Assert.Single(rooms);
        }

        // ---- Fusion des deux anciens fichiers ------------------------------

        /// <summary>
        /// LA règle à ne pas casser. `WatchListManager` documentait la décision
        /// du mainteneur : être favori ne doit PAS déclencher une surveillance.
        /// Sans ce test, une refonte future ferait sonder le site pour chaque
        /// favori.
        /// </summary>
        [Fact]
        public void AMigratedFavouriteIsNotMonitored()
        {
            var rooms = RoomStore.Merge(
                new[] { "https://www.twitch.tv/a", "https://www.twitch.tv/b" },
                Array.Empty<string>(),
                Maintenant);

            Assert.Equal(2, rooms.Count);
            Assert.All(rooms, r => Assert.False(r.AutoRecord));
        }

        [Fact]
        public void AMigratedMonitoredRoomKeepsItsMonitoring()
        {
            var rooms = RoomStore.Merge(
                Array.Empty<string>(),
                new[] { "https://www.twitch.tv/a" },
                Maintenant);

            Assert.True(Assert.Single(rooms).AutoRecord);
        }

        /// <summary>
        /// Présent des deux côtés : la surveillance l'emporte. Perdre une
        /// surveillance active est un vrai dommage ; en gagner une par la
        /// fusion n'en est pas un.
        /// </summary>
        [Fact]
        public void PresentInBothFilesTheMonitoringWins()
        {
            var rooms = RoomStore.Merge(
                new[] { "https://www.twitch.tv/a" },
                new[] { "https://www.twitch.tv/a" },
                Maintenant);

            Assert.True(Assert.Single(rooms).AutoRecord);
        }

        [Fact]
        public void NobodyIsLostInTheMerge()
        {
            var rooms = RoomStore.Merge(
                new[] { "https://www.twitch.tv/a", "https://www.twitch.tv/b" },
                new[] { "https://www.twitch.tv/b", "https://fr.chaturbate.com/c" },
                Maintenant);

            // Assertion sur l'ENSEMBLE et non sur un décompte : « combien sont
            // surveillés » ne dit pas LESQUELS, et c'est en écrivant un
            // décompte que je m'étais trompé — b est surveillé parce qu'il
            // figure dans les deux fichiers, c parce qu'il n'est que surveillé.
            Assert.Equal(3, rooms.Count);
            Assert.False(rooms.Single(r => r.Url.EndsWith("/a")).AutoRecord);
            Assert.True(rooms.Single(r => r.Url.EndsWith("/b")).AutoRecord);
            Assert.True(rooms.Single(r => r.Url.EndsWith("/c")).AutoRecord);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void BlankEntriesAreDropped(string? vide)
        {
            Assert.Empty(RoomStore.Merge(new[] { vide }, null, Maintenant));
        }

        [Fact]
        public void TwoAbsentFilesGiveAnEmptyList()
        {
            Assert.Empty(RoomStore.Merge(null, null, Maintenant));
        }

        // ---- Dérivation de l'état affiché ---------------------------------

        /// <summary>
        /// **L'enregistrement prime sur le sondage.** Un sondage peut échouer
        /// pendant que la capture reçoit des données ; afficher « inconnu » ou
        /// « hors ligne » sur un enregistrement qui tourne ferait croire à une
        /// panne et pousserait à l'arrêter.
        /// </summary>
        [Theory]
        [InlineData(RoomStatus.Online)]
        [InlineData(RoomStatus.Offline)]
        [InlineData(RoomStatus.Unknown)]
        [InlineData(RoomStatus.NotFound)]
        public void ARunningRecordingBeatsAnyRoomStatus(RoomStatus statut)
        {
            Assert.Equal(RoomRowState.Recording,
                RoomStore.Resolve(statut, DownloadState.Running, reconnexionPrevue: false));
        }

        [Fact]
        public void APlannedReconnectionShowsAsReconnecting()
        {
            Assert.Equal(RoomRowState.Reconnecting,
                RoomStore.Resolve(RoomStatus.Offline, DownloadState.Stopped, reconnexionPrevue: true));
        }

        [Theory]
        [InlineData(RoomStatus.Online, null, RoomRowState.Live)]
        [InlineData(RoomStatus.Offline, null, RoomRowState.Idle)]
        [InlineData(RoomStatus.Unknown, null, RoomRowState.Unknown)]
        [InlineData(RoomStatus.NotFound, null, RoomRowState.NotFound)]
        [InlineData(RoomStatus.Offline, DownloadState.Completed, RoomRowState.Finished)]
        [InlineData(RoomStatus.Offline, DownloadState.Stopped, RoomRowState.Finished)]
        [InlineData(RoomStatus.Offline, DownloadState.Failed, RoomRowState.Failed)]
        [InlineData(RoomStatus.Offline, DownloadState.Idle, RoomRowState.Idle)]
        public void ResolvesEveryOtherCombination(RoomStatus statut, DownloadState? job, RoomRowState attendu)
        {
            Assert.Equal(attendu, RoomStore.Resolve(statut, job, reconnexionPrevue: false));
        }

        /// <summary>
        /// `Unknown` ne doit JAMAIS se confondre avec `Live` : c'est la règle
        /// qui empêche la surveillance de déclencher un enregistrement sur un
        /// réseau coupé, et elle a déjà été payée en v1.26.1.
        /// </summary>
        [Fact]
        public void UnknownIsNeverLive()
        {
            Assert.NotEqual(RoomRowState.Live,
                RoomStore.Resolve(RoomStatus.Unknown, null, reconnexionPrevue: false));
        }
    }
}
