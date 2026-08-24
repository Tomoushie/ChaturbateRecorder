using System;
using System.IO;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// La reprise des réglages premium (planificateur, auto-enregistrement)
    /// après un redémarrage de l'application -- jamais éprouvée jusqu'ici :
    /// <see cref="ScheduleTests"/> et <see cref="RoomStoreTests"/> couvrent la
    /// fonction pure et la fusion, aucun des deux n'écrit puis ne relit
    /// <c>rooms.json</c> comme le ferait un vrai démarrage. Même isolation que
    /// <see cref="DataLocationTests"/> : un dossier de données jetable, jamais
    /// le vrai <c>%LocalAppData%</c> du mainteneur.
    /// </summary>
    [Collection("EtatDeProcessus")]
    public class RoomStorePersistenceTests : IDisposable
    {
        private readonly string _dossierNeuf;
        private readonly string _dataDirInitial;

        public RoomStorePersistenceTests()
        {
            _dataDirInitial = AppConfig.DataDir;
            _dossierNeuf = Path.Combine(Path.GetTempPath(), "cbr-schedule-" + Guid.NewGuid().ToString("N"));
            AppConfig.DataDir = _dossierNeuf;
        }

        public void Dispose()
        {
            AppConfig.DataDir = _dataDirInitial;
            try { if (Directory.Exists(_dossierNeuf)) Directory.Delete(_dossierNeuf, recursive: true); }
            catch { /* Dossier temporaire : son sort n'engage rien. */ }
        }

        /// <summary>
        /// LE cas qui compte : une fenêtre horaire configurée, l'application
        /// fermée puis relancée -- une SECONDE instance de <see cref="RoomStore"/>
        /// doit retrouver exactement les mêmes minutes, pas des valeurs par
        /// défaut qui désarmeraient silencieusement le planificateur de
        /// l'acheteur.
        /// </summary>
        [Fact]
        public void UneFenetreHoraireSurvitAUnRedemarrage()
        {
            var url = "https://fr.chaturbate.com/salontest/";

            var avant = new RoomStore();
            avant.Add(url);
            Assert.True(avant.SetSchedule(url, true, 20 * 60, 23 * 60));

            // Une INSTANCE NEUVE : rien à part le fichier ne survit entre les
            // deux, exactement comme au prochain démarrage de l'application.
            var apres = new RoomStore();
            apres.Load();

            var retrouve = apres.Find(url);
            Assert.NotNull(retrouve);
            Assert.True(retrouve!.ScheduleEnabled);
            Assert.Equal(20 * 60, retrouve.ScheduleStartMinutes);
            Assert.Equal(23 * 60, retrouve.ScheduleEndMinutes);
        }

        /// <summary>
        /// Même règle pour l'auto-enregistrement (gratuit) : un redémarrage ne
        /// doit pas désarmer un salon que l'utilisateur avait explicitement
        /// activé.
        /// </summary>
        [Fact]
        public void LAutoEnregistrementSurvitAUnRedemarrage()
        {
            var url = "https://fr.chaturbate.com/autretest/";

            var avant = new RoomStore();
            avant.Add(url, autoRecord: true);

            var apres = new RoomStore();
            apres.Load();

            Assert.True(apres.Find(url)?.AutoRecord);
        }

        /// <summary>
        /// Une fenêtre JAMAIS configurée (salon ajouté avant l'existence du
        /// planificateur, ou jamais ouvert dans <c>ScheduleWindow</c>) doit
        /// survivre comme « jamais configurée » -- pas comme une fenêtre de
        /// longueur nulle, qui ne couvre jamais rien pour une raison
        /// différente (un réglage inachevé, voir <c>ScheduleTests</c>).
        /// </summary>
        [Fact]
        public void UneFenetreJamaisConfigureeResteJamaisConfiguree()
        {
            var url = "https://fr.chaturbate.com/salonsansplanif/";

            var avant = new RoomStore();
            avant.Add(url);
            // Jamais de SetSchedule ici : c'est exactement le salon ajouté
            // avant que le planificateur n'existe.

            var apres = new RoomStore();
            apres.Load();

            var retrouve = apres.Find(url);
            Assert.NotNull(retrouve);
            Assert.False(retrouve!.ScheduleEnabled);
            Assert.Equal(-1, retrouve.ScheduleStartMinutes);
            Assert.Equal(-1, retrouve.ScheduleEndMinutes);
            Assert.False(RoomStore.DansLaFenetreHoraire(
                retrouve.ScheduleStartMinutes, retrouve.ScheduleEndMinutes, maintenant: 12 * 60));
        }

        /// <summary>
        /// « Noms vides » côté planificateur : une URL inconnue du magasin
        /// (salon retiré entretemps, ou jamais ajouté) ne doit ni planter ni
        /// créer une entrée fantôme -- juste refuser proprement, comme
        /// <c>Find</c> le fait déjà pour toute autre opération.
        /// </summary>
        [Theory]
        [InlineData("https://fr.chaturbate.com/salon-jamais-ajoute/")]
        [InlineData("")]
        public void PlanifierUnSalonInconnuNeFaitRienEtNePlanteJamais(string url)
        {
            var store = new RoomStore();

            Assert.False(store.SetSchedule(url, true, 20 * 60, 23 * 60));
            Assert.False(store.SetAutoRecord(url, true));
        }
    }
}
