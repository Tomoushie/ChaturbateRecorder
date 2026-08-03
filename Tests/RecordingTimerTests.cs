using System;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Minuteur d'enregistrement (87.0). Seule la logique pure est testable
    /// ici : la conversion du choix en minutes et la mise en forme du temps
    /// restant. Le déclenchement de l'arrêt vit dans MainForm et dépend d'un
    /// timer WinForms, hors de portée d'un test unitaire.
    /// </summary>
    public class RecordingTimerTests
    {
        [Fact]
        public void UnlimitedIsTheFirstPresetAndMeansZero()
        {
            // L'index 0 est la valeur par défaut du menu déroulant : elle doit
            // signifier "aucun minuteur", sinon un enregistrement serait coupé
            // sans que l'utilisateur ait rien demandé.
            Assert.Equal(0, RecordingTimer.PresetMinutes[0]);
            Assert.Equal(0, RecordingTimer.MinutesForIndex(0));
        }

        [Theory]
        [InlineData(1, 15)]
        [InlineData(2, 30)]
        [InlineData(3, 60)]
        [InlineData(4, 120)]
        [InlineData(5, 240)]
        [InlineData(6, 480)]
        public void EachPresetIndexMapsToItsDuration(int index, int expectedMinutes)
        {
            Assert.Equal(expectedMinutes, RecordingTimer.MinutesForIndex(index));
        }

        [Theory]
        [InlineData(-1)]   // ComboBox sans sélection
        [InlineData(7)]    // juste après le dernier preset
        [InlineData(999)]
        public void OutOfRangeIndexFallsBackToUnlimited(int index)
        {
            // En cas de doute on n'arrête rien : couper un enregistrement par
            // erreur est bien pire que de ne pas l'avoir coupé.
            Assert.Equal(0, RecordingTimer.MinutesForIndex(index));
        }

        [Fact]
        public void PresetsAreStrictlyIncreasingAfterUnlimited()
        {
            // Un menu déroulant dont les durées ne sont pas croissantes serait
            // déroutant, et trahirait une insertion au mauvais endroit — ce qui
            // décalerait aussi la correspondance index -> minutes.
            for (var i = 2; i < RecordingTimer.PresetMinutes.Length; i++)
            {
                Assert.True(RecordingTimer.PresetMinutes[i] > RecordingTimer.PresetMinutes[i - 1],
                    $"Preset {i} ({RecordingTimer.PresetMinutes[i]}) n'est pas supérieur au précédent.");
            }
        }

        [Fact]
        public void ComboBoxHasExactlyOneEntryPerPreset()
        {
            // La sélection est convertie en minutes PAR SON INDEX : un libellé
            // ajouté sans preset correspondant (ou l'inverse) décalerait
            // silencieusement toutes les durées.
            var cles = new[]
            {
                "duration.unlimited", "duration.15min", "duration.30min",
                "duration.1h", "duration.2h", "duration.4h", "duration.8h"
            };

            Assert.Equal(RecordingTimer.PresetMinutes.Length, cles.Length);
            foreach (var cle in cles)
                Assert.True(Localization.AllStrings.ContainsKey(cle), $"Clé de durée manquante : '{cle}'.");
        }

        [Theory]
        [InlineData(0, "0 s")]
        [InlineData(-5, "0 s")]        // échéance dépassée : jamais de négatif
        [InlineData(-3600, "0 s")]
        public void NonPositiveRemainingShowsZero(int seconds, string expected)
        {
            Assert.Equal(expected, RecordingTimer.FormatRemaining(TimeSpan.FromSeconds(seconds)));
        }

        [Theory]
        [InlineData(1, "1 s")]
        [InlineData(45, "45 s")]
        [InlineData(59, "59 s")]
        public void UnderOneMinuteShowsSeconds(int seconds, string expected)
        {
            Assert.Equal(expected, RecordingTimer.FormatRemaining(TimeSpan.FromSeconds(seconds)));
        }

        [Theory]
        [InlineData(60, "1 min")]
        [InlineData(90, "1 min")]      // minutes tronquées, comme un décompte classique
        [InlineData(119, "1 min")]
        [InlineData(120, "2 min")]
        [InlineData(3540, "59 min")]
        public void UnderOneHourShowsMinutes(int seconds, string expected)
        {
            Assert.Equal(expected, RecordingTimer.FormatRemaining(TimeSpan.FromSeconds(seconds)));
        }

        [Theory]
        [InlineData(3600, "1 h")]
        [InlineData(7200, "2 h")]
        [InlineData(5400, "1 h 30 min")]
        [InlineData(28800, "8 h")]
        public void OneHourAndAboveShowsHours(int seconds, string expected)
        {
            Assert.Equal(expected, RecordingTimer.FormatRemaining(TimeSpan.FromSeconds(seconds)));
        }

        [Fact]
        public void SecondsRoundUpSoTheLastOneIsNeverShownAsZero()
        {
            // L'arrondi au supérieur porte sur les secondes : tant que
            // l'échéance n'est pas atteinte il doit rester au moins "1 s",
            // sinon le décompte afficherait "0 s" avant l'arrêt effectif.
            Assert.Equal("1 s", RecordingTimer.FormatRemaining(TimeSpan.FromMilliseconds(1)));
            Assert.Equal("1 s", RecordingTimer.FormatRemaining(TimeSpan.FromMilliseconds(999)));
            Assert.Equal("2 s", RecordingTimer.FormatRemaining(TimeSpan.FromMilliseconds(1001)));
        }

        [Fact]
        public void TheDisplayNeverJumpsBackwardsAsTimePasses()
        {
            // Parcourt une échéance d'une heure seconde par seconde : le texte
            // ne doit jamais annoncer plus de temps restant qu'à l'instant
            // précédent, ce qu'un mélange d'arrondis mal placé provoquerait.
            var precedent = TimeSpan.FromHours(1);
            var texteprecedent = RecordingTimer.FormatRemaining(precedent);

            for (var s = 3599; s >= 0; s--)
            {
                var actuel = TimeSpan.FromSeconds(s);
                var texte = RecordingTimer.FormatRemaining(actuel);
                Assert.True(actuel < precedent, "Le parcours doit être décroissant.");
                precedent = actuel;
                texteprecedent = texte;
            }

            Assert.Equal("0 s", texteprecedent);
        }

        [Fact]
        public void EveryPresetFormatsWithoutThrowing()
        {
            foreach (var minutes in RecordingTimer.PresetMinutes)
            {
                var texte = RecordingTimer.FormatRemaining(TimeSpan.FromMinutes(minutes));
                Assert.False(string.IsNullOrWhiteSpace(texte));
            }
        }
    }
}
