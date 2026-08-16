using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Safe Mode (29.0 / 2.2) : desactiver une fonctionnalite defaillante, le
    /// dire, et continuer a tourner.
    ///
    /// SafeMode est un etat GLOBAL statique, donc chaque test le remet a zero
    /// avant de s'executer — sans quoi l'ordre d'execution changerait les
    /// resultats, et un test vert ne prouverait plus rien.
    /// </summary>
    public class SafeModeTests
    {
        private static void Reset()
        {
            SafeMode.ClearAutomatic();
            SafeMode.LoadManual(null);
        }

        [Fact]
        public void EverythingIsEnabledByDefault()
        {
            Reset();
            Assert.True(SafeMode.IsEnabled(SafeComponent.Ffmpeg));
            Assert.True(SafeMode.IsEnabled(SafeComponent.Cookies));
            Assert.False(SafeMode.AnythingDisabled);
        }

        [Fact]
        public void AManualToggleDisablesAndIsReportedAsManual()
        {
            Reset();
            SafeMode.SetManual(SafeComponent.Proxy, true);

            Assert.False(SafeMode.IsEnabled(SafeComponent.Proxy));
            Assert.Equal(SafeReason.Manual, SafeMode.ReasonFor(SafeComponent.Proxy));
            Assert.True(SafeMode.AnythingDisabled);
        }

        [Fact]
        public void AnAutomaticDisableKeepsItsReasonForDisplay()
        {
            Reset();
            SafeMode.DisableAutomatically(SafeComponent.Ffmpeg, "ffmpeg.exe introuvable");

            Assert.False(SafeMode.IsEnabled(SafeComponent.Ffmpeg));
            Assert.Equal(SafeReason.Automatic, SafeMode.ReasonFor(SafeComponent.Ffmpeg));
            Assert.Equal("ffmpeg.exe introuvable", SafeMode.AutomaticReason(SafeComponent.Ffmpeg));
        }

        /// <summary>
        /// Le garde-fou qui compte pour l'affichage : si l'utilisateur a
        /// lui-meme decoche la case, lui dire que l'application a desactive le
        /// composant serait faux — il croirait a un dysfonctionnement.
        /// </summary>
        [Fact]
        public void ManualWinsOverAutomaticWhenBothApply()
        {
            Reset();
            SafeMode.SetManual(SafeComponent.Cookies, true);
            SafeMode.DisableAutomatically(SafeComponent.Cookies, "fichier illisible");

            Assert.Equal(SafeReason.Manual, SafeMode.ReasonFor(SafeComponent.Cookies));
            Assert.False(SafeMode.IsEnabled(SafeComponent.Cookies));
        }

        /// <summary>
        /// Un nouveau controle doit pouvoir reactiver ce que l'application avait
        /// desactive — sinon un ffmpeg replace resterait hors service jusqu'au
        /// prochain redemarrage.
        /// </summary>
        [Fact]
        public void ClearAutomaticReenablesButLeavesManualUntouched()
        {
            Reset();
            SafeMode.SetManual(SafeComponent.Watch, true);
            SafeMode.DisableAutomatically(SafeComponent.Ffmpeg, "absent");

            SafeMode.ClearAutomatic();

            Assert.True(SafeMode.IsEnabled(SafeComponent.Ffmpeg));
            Assert.False(SafeMode.IsEnabled(SafeComponent.Watch));
        }

        [Fact]
        public void ManualStateSurvivesASaveLoadRoundTrip()
        {
            Reset();
            SafeMode.SetManual(SafeComponent.Proxy, true);
            SafeMode.SetManual(SafeComponent.MultiStream, true);

            var saved = SafeMode.ManualNames;
            Reset();
            SafeMode.LoadManual(saved);

            Assert.False(SafeMode.IsEnabled(SafeComponent.Proxy));
            Assert.False(SafeMode.IsEnabled(SafeComponent.MultiStream));
            Assert.True(SafeMode.IsEnabled(SafeComponent.Cookies));
        }

        /// <summary>
        /// Un reglage ecrit par une version ulterieure, ou corrompu a la main,
        /// ne doit pas empecher le demarrage : on ignore ce qu'on ne comprend
        /// pas plutot que de lever.
        /// </summary>
        [Fact]
        public void AnUnknownComponentNameIsIgnoredRatherThanThrowing()
        {
            Reset();
            SafeMode.LoadManual(new[] { "Proxy", "TeleportationQuantique", "" });

            Assert.False(SafeMode.IsEnabled(SafeComponent.Proxy));
            Assert.True(SafeMode.IsEnabled(SafeComponent.Ffmpeg));
        }

        [Fact]
        public void AutomaticallyDisabledListsOnlyWhatTheAppTurnedOff()
        {
            Reset();
            SafeMode.SetManual(SafeComponent.Watch, true);
            SafeMode.DisableAutomatically(SafeComponent.Ffmpeg, "absent");
            SafeMode.DisableAutomatically(SafeComponent.Cookies, "illisible");

            Assert.Equal(
                new[] { SafeComponent.Ffmpeg, SafeComponent.Cookies },
                SafeMode.AutomaticallyDisabled);
        }
    }
}
