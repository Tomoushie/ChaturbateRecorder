using System;
using System.IO;
using System.Linq;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le nom de salon d'une capture, pour la Galerie (Premium II) : tri et
    /// filtre par salon ne doivent PAS re-parser un nom de fichier que le
    /// nommage intelligent a pu rendre arbitraire (voir <c>SmartNamingTests</c>).
    /// </summary>
    public class CaptureFinalizerSalonTests
    {
        [Fact]
        public void ExtraitLeSalonDUnNomBaseOrdinaire()
        {
            Assert.Equal("mollyflwers", CaptureFinalizer.ExtraireNomSalon("mollyflwers-2026-08-17_20-15-35"));
        }

        /// <summary>Même raison que côté nommage intelligent : un salon accentué ne doit pas être mangé.</summary>
        [Fact]
        public void ExtraitUnSalonAccentue()
        {
            Assert.Equal("amélie-réaumur", CaptureFinalizer.ExtraireNomSalon("amélie-réaumur-2026-08-17_20-15-35"));
        }

        [Fact]
        public void UnNomBaseDejaRenommeNeCorrespondPlusARien()
        {
            Assert.Null(CaptureFinalizer.ExtraireNomSalon("vacances_ete"));
        }
    }

    /// <summary>
    /// <see cref="HistoryService"/> : le sidecar de salon gagne toujours sur
    /// un nom de fichier reparsé, et une capture sans AUCUN des deux ne lève
    /// pas — même isolation que <c>DataLocationTests</c>
    /// (<c>AppConfig.CaptureDir</c> redirigé, jamais le vrai dossier de
    /// l'utilisateur).
    /// </summary>
    [Collection("EtatDeProcessus")]
    public class HistorySalonTests : IDisposable
    {
        private readonly string _dossier;
        private readonly string _captureDirInitial;

        public HistorySalonTests()
        {
            _captureDirInitial = AppConfig.CaptureDir;
            _dossier = Path.Combine(Path.GetTempPath(), "cbr-galerie-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dossier);
            AppConfig.CaptureDir = _dossier;
        }

        public void Dispose()
        {
            AppConfig.CaptureDir = _captureDirInitial;
            try { Directory.Delete(_dossier, recursive: true); }
            catch { /* Dossier temporaire : son sort n'engage rien. */ }
        }

        /// <summary>
        /// LE cas qui compte : un nom déjà transformé par le nommage
        /// intelligent (aucune forme "salon-DATE_HEURE" à reparser) retrouve
        /// quand même son salon grâce au sidecar écrit AVANT le renommage.
        /// </summary>
        [Fact]
        public void LeSidecarGagneMemeSurUnNomDejaPersonnalise()
        {
            var video = Path.Combine(_dossier, "capture-du-jour.mp4");
            File.WriteAllText(video, "contenu");
            File.WriteAllText(Path.Combine(_dossier, "capture-du-jour.salon.txt"), "mollyflwers");

            var entree = HistoryService.Lister().Single();

            Assert.Equal("mollyflwers", entree.Salon);
        }

        /// <summary>Repli pour les captures antérieures au 24-08, qui n'ont jamais eu de sidecar.</summary>
        [Fact]
        public void SansSidecarOnReplieSurLeNomDeFichier()
        {
            var video = Path.Combine(_dossier, "mollyflwers-2026-08-17_20-15-35.mp4");
            File.WriteAllText(video, "contenu");

            var entree = HistoryService.Lister().Single();

            Assert.Equal("mollyflwers", entree.Salon);
        }

        [Fact]
        public void NiSidecarNiNomReconnaissableRendUnSalonNul()
        {
            var video = Path.Combine(_dossier, "vacances_ete.mp4");
            File.WriteAllText(video, "contenu");

            var entree = HistoryService.Lister().Single();

            Assert.Null(entree.Salon);
        }
    }
}
