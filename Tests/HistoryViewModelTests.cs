using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.ViewModels;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// <see cref="ChaturbateRecorderApp.Services.CaptureFinalizer.ExtraireDuree"/> —
    /// fonction PURE, même raison que les autres analyses isolées de ce
    /// service (<c>DetecterQualiteAsync</c>, <c>ConstruireNomIntelligent</c>).
    /// </summary>
    public class ExtraireDureeTests
    {
        [Fact]
        public void LitUneDureeOrdinaire()
        {
            var duree = Services.CaptureFinalizer.ExtraireDuree(
                "  Duration: 00:12:34.56, start: 0.000000, bitrate: 512 kb/s");

            Assert.Equal(new TimeSpan(0, 0, 12, 34, 560), duree);
        }

        [Theory]
        [InlineData("Duration: N/A, start: N/A, bitrate: N/A")]
        [InlineData("frame=  120 fps=25 q=-1.0 size=N/A time=00:00:04.80 bitrate=N/A")]
        [InlineData("")]
        public void RendNullSansDureeReconnaissable(string sortieFfmpeg)
        {
            Assert.Null(Services.CaptureFinalizer.ExtraireDuree(sortieFfmpeg));
        }
    }

    /// <summary>
    /// Le tri et le filtre de <see cref="HistoryViewModel"/> (Galerie, Premium
    /// II) — même isolation que <c>HistorySalonTests</c>
    /// (<c>AppConfig.CaptureDir</c> redirigé). La détection de durée en
    /// arrière-plan n'est PAS testée ici (demanderait un vrai ffmpeg contre un
    /// vrai clip, déjà fait pour <c>ExtraireDuree</c> ci-dessus) : ces fichiers
    /// de test ne sont que du texte, leur durée reste toujours inconnue, ce qui
    /// suffit à éprouver le tri « valeurs connues d'abord, inconnues ensuite »
    /// pour le salon — le même motif de code que pour la durée.
    /// </summary>
    [Collection("EtatDeProcessus")]
    public class HistoryViewModelTests : IDisposable
    {
        private readonly string _dossier;
        private readonly string _captureDirInitial;

        public HistoryViewModelTests()
        {
            _captureDirInitial = AppConfig.CaptureDir;
            _dossier = Path.Combine(Path.GetTempPath(), "cbr-galerie-vm-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dossier);
            AppConfig.CaptureDir = _dossier;
        }

        public void Dispose()
        {
            AppConfig.CaptureDir = _captureDirInitial;
            try { Directory.Delete(_dossier, recursive: true); }
            catch { /* Dossier temporaire : son sort n'engage rien. */ }
        }

        private void CreerFichier(string nom, DateTime derniereEcriture, string? salon = null)
        {
            var chemin = Path.Combine(_dossier, nom);
            File.WriteAllText(chemin, "contenu");
            File.SetLastWriteTime(chemin, derniereEcriture);
            if (salon != null)
            {
                File.WriteAllText(
                    Path.Combine(_dossier, Path.GetFileNameWithoutExtension(nom) + Services.CaptureFinalizer.ExtensionSidecarSalon),
                    salon);
            }
        }

        [Fact]
        public async Task TrieParDateDecroissanteParDefaut()
        {
            CreerFichier("a.mp4", new DateTime(2026, 1, 1));
            CreerFichier("b.mp4", new DateTime(2026, 6, 1));

            var vm = new HistoryViewModel();
            await vm.RafraichirCommand.ExecuteAsync(null);

            Assert.Equal(new[] { "b.mp4", "a.mp4" }, vm.Elements.Select(e => e.Nom).ToArray());
        }

        [Fact]
        public async Task LeFiltreDeSalonEstInsensibleALaCasseEtPartiel()
        {
            CreerFichier("a.mp4", new DateTime(2026, 1, 1), salon: "MollyFlwers");
            CreerFichier("b.mp4", new DateTime(2026, 1, 2), salon: "AutreSalon");

            var vm = new HistoryViewModel();
            await vm.RafraichirCommand.ExecuteAsync(null);
            vm.FiltreSalon = "molly";

            Assert.Equal(new[] { "a.mp4" }, vm.Elements.Select(e => e.Nom).ToArray());
        }

        /// <summary>
        /// LE cas qui compte : un salon INCONNU (pas de sidecar, nom déjà
        /// personnalisé) ne doit pas se comporter comme "avant l'alphabet" —
        /// il doit rester en FIN de liste, jamais mélangé aux salons connus.
        /// </summary>
        [Fact]
        public async Task LeTriParSalonRepousseLesSalonsInconnusEnFin()
        {
            CreerFichier("zebra.mp4", new DateTime(2026, 1, 1), salon: "zebra");
            CreerFichier("connu.mp4", new DateTime(2026, 1, 2), salon: "alpha");
            CreerFichier("inconnu.mp4", new DateTime(2026, 1, 3)); // pas de sidecar, nom non reconnaissable -> Salon null

            var vm = new HistoryViewModel();
            await vm.RafraichirCommand.ExecuteAsync(null);
            vm.Tri = TriHistorique.Salon;

            Assert.Equal(new[] { "connu.mp4", "zebra.mp4", "inconnu.mp4" }, vm.Elements.Select(e => e.Nom).ToArray());
        }

        [Fact]
        public async Task BasculerEnGalerieEtRevenirEnListeChangeLeMode()
        {
            var vm = new HistoryViewModel();
            await vm.RafraichirCommand.ExecuteAsync(null);

            Assert.Equal(ModeAffichageHistorique.Liste, vm.Mode);

            vm.AfficherEnGalerieCommand.Execute(null);
            Assert.Equal(ModeAffichageHistorique.Galerie, vm.Mode);

            vm.AfficherEnListeCommand.Execute(null);
            Assert.Equal(ModeAffichageHistorique.Liste, vm.Mode);
        }
    }
}
