using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// <see cref="RoomStore.DansLaFenetreHoraire"/> — le planificateur premium,
    /// entièrement testable sans salon, sans licence, sans horloge réelle :
    /// une fonction pure sur trois entiers.
    /// </summary>
    public class ScheduleTests
    {
        [Theory]
        [InlineData(20 * 60, 23 * 60, 21 * 60, true)] // 21h, dans 20h-23h
        [InlineData(20 * 60, 23 * 60, 19 * 60, false)] // 19h, avant
        [InlineData(20 * 60, 23 * 60, 23 * 60, false)] // 23h pile, borne EXCLUSIVE
        [InlineData(20 * 60, 23 * 60, 20 * 60, true)] // 20h pile, borne INCLUSIVE
        public void FenetreOrdinaireDansLaMemeJournee(int debut, int fin, int maintenant, bool attendu)
        {
            Assert.Equal(attendu, RoomStore.DansLaFenetreHoraire(debut, fin, maintenant));
        }

        [Theory]
        [InlineData(22 * 60, 2 * 60, 23 * 60, true)] // 23h, dans 22h->02h
        [InlineData(22 * 60, 2 * 60, 1 * 60, true)] // 01h, toujours dedans (traverse minuit)
        [InlineData(22 * 60, 2 * 60, 12 * 60, false)] // midi, en dehors
        public void FenetreQuiTraverseMinuit(int debut, int fin, int maintenant, bool attendu)
        {
            Assert.Equal(attendu, RoomStore.DansLaFenetreHoraire(debut, fin, maintenant));
        }

        [Fact]
        public void JamaisConfigureNeCouvreJamaisRien()
        {
            Assert.False(RoomStore.DansLaFenetreHoraire(-1, -1, 12 * 60));
        }

        /// <summary>
        /// Un début == une fin ne veut PAS dire « toute la journée » : un
        /// réglage inachevé (les deux champs encore à leur valeur par défaut
        /// identique) ne doit pas se comporter comme une fenêtre ouverte.
        /// </summary>
        [Fact]
        public void DebutEgalFinNeCouvreJamaisRien()
        {
            Assert.False(RoomStore.DansLaFenetreHoraire(10 * 60, 10 * 60, 10 * 60));
        }
    }
}
