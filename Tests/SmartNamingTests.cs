using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le nommage intelligent (premium) : jamais de licence à fabriquer ici,
    /// grâce à <c>CaptureFinalizer.ConstruireNomIntelligent</c>, une fonction
    /// PURE isolée exprès de la vérification de licence et du disque —
    /// exactement la même raison que la fabrique de moteurs de
    /// <c>RecordingCoordinator</c> (17-08) : rendre éprouvable ce qui, sinon,
    /// ne le serait pas sans lancer un vrai composant premium.
    /// </summary>
    public class SmartNamingTests
    {
        private const string NomBase = "mollyflwers-2026-08-17_20-15-35";

        [Fact]
        public void RemplaceLesQuatreJetons()
        {
            var resultat = CaptureFinalizer.ConstruireNomIntelligent(
                "{salon}_{date}_{heure}_{qualite}", NomBase, "1080p");

            Assert.Equal("mollyflwers_2026-08-17_20-15-35_1080p", resultat);
        }

        [Fact]
        public void QualiteInconnueUtiliseUnRepliLisible()
        {
            var resultat = CaptureFinalizer.ConstruireNomIntelligent("{salon}-{qualite}", NomBase, null);

            Assert.Equal("mollyflwers-qualite-inconnue", resultat);
        }

        /// <summary>
        /// Un motif SANS jeton reconnu (faute de frappe, ou juste un texte
        /// fixe) rend un nom constant : PAS null, puisque le motif s'est
        /// appliqué avec succès — c'est le nom qui ne dépend plus de rien.
        /// </summary>
        [Fact]
        public void UnMotifSansJetonRendUnNomConstant()
        {
            var resultat = CaptureFinalizer.ConstruireNomIntelligent("capture-du-jour", NomBase, "720p");

            Assert.Equal("capture-du-jour", resultat);
        }

        [Fact]
        public void UnNomBaseInattenduDesactiveLeNommage()
        {
            // Un nom déjà renommé à la main, par exemple : ne colle plus au
            // moule "salon-AAAA-MM-JJ_HH-mm-ss".
            var resultat = CaptureFinalizer.ConstruireNomIntelligent("{salon}_{qualite}", "vacances_ete", "720p");

            Assert.Null(resultat);
        }

        /// <summary>
        /// Un résultat IDENTIQUE au nom d'origine ne compte pas comme un
        /// renommage : évite un File.Move vers... le même chemin.
        /// </summary>
        [Fact]
        public void UnResultatIdentiqueAuNomDOrigineNeCompensePasCommeUnRenommage()
        {
            var resultat = CaptureFinalizer.ConstruireNomIntelligent(NomBase, NomBase, "720p");

            Assert.Null(resultat);
        }

        [Fact]
        public void UnResultatVideDesactiveLeNommage()
        {
            var resultat = CaptureFinalizer.ConstruireNomIntelligent("   ", NomBase, "720p");

            Assert.Null(resultat);
        }

        /// <summary>
        /// Un nom de salon contenant un caractère interdit en chemin (ex :
        /// venu d'une plateforme dont le nom d'utilisateur autorise plus de
        /// caractères que Windows n'en accepte dans un fichier) ne doit pas
        /// produire un nom de fichier invalide.
        /// </summary>
        [Fact]
        public void LesCaracteresInterditsEnCheminSontNettoyes()
        {
            var nomBaseAvecDeuxPoints = "salon:etrange-2026-08-17_20-15-35";
            var resultat = CaptureFinalizer.ConstruireNomIntelligent("{salon}", nomBaseAvecDeuxPoints, "720p");

            Assert.NotNull(resultat);
            Assert.DoesNotContain(':', resultat);
        }

        /// <summary>
        /// Les accents sont des caractères NTFS parfaitement valides — aucune
        /// raison de les toucher — mais tous les tests jusqu'ici n'employaient
        /// que des noms de salon ASCII. Verrouille qu'un `Replace` de jeton
        /// n'en mange aucun, la même classe de défaut que celui trouvé sur la
        /// PROSE de ce même commit (mot sans accent dans la table de
        /// traduction, voir <c>AccentsTests</c>).
        /// </summary>
        [Fact]
        public void LesAccentsDuNomDeSalonSurviventAuRemplacement()
        {
            var nomBaseAccentue = "amélie-réaumur-2026-08-17_20-15-35";
            var resultat = CaptureFinalizer.ConstruireNomIntelligent("{salon}_{qualite}", nomBaseAccentue, "1080p");

            Assert.Equal("amélie-réaumur_1080p", resultat);
        }

        // --- Détection de qualité, contre un VRAI ffmpeg ------------------

        /// <summary>
        /// <c>testsrc</c> : une mire vidéo synthétique, générée par ffmpeg
        /// lui-même, aucun réseau ni salon réel nécessaire — même principe
        /// que côté StreamRecorderPro (RawFrameReaderTests). Le chemin
        /// ffmpeg est un PARAMÈTRE de DetecterQualiteAsync (pas
        /// AppConfig.FFmpegPath lu en interne) précisément pour pouvoir viser
        /// le vrai binaire ici sans toucher à un état de processus global
        /// que d'autres tests lisent en parallèle.
        /// </summary>
        [Fact]
        public async Task DetecteLaHauteurDUnFichierReel()
        {
            var cheminReel = CheminFfmpegDuBuildPrincipal();
            if (cheminReel == null) return; // pas de ffmpeg disponible ici

            var fichier = Path.Combine(Path.GetTempPath(), "cbr-qualite-" + Guid.NewGuid().ToString("N") + ".mp4");
            try
            {
                Assert.True(await GenererClipDeTestAsync(cheminReel, fichier, 320, 180),
                    "le clip de test n'a pas pu être généré : rien à sonder");

                var qualite = await CaptureFinalizer.DetecterQualiteAsync(fichier, cheminReel);

                Assert.Equal("180p", qualite);
            }
            finally
            {
                try { if (File.Exists(fichier)) File.Delete(fichier); } catch { /* dossier temporaire */ }
            }
        }

        [Fact]
        public async Task RendNullSansFfmpeg()
        {
            var introuvable = Path.Combine(Path.GetTempPath(), "ffmpeg-introuvable-" + Guid.NewGuid().ToString("N") + ".exe");

            var qualite = await CaptureFinalizer.DetecterQualiteAsync("peu-importe.mp4", introuvable);

            Assert.Null(qualite);
        }

        /// <summary>
        /// Un flux qui meurt avant la première image : le `.part` renommé
        /// existe mais ne contient aucune vidéo lisible (0 octet, ou un
        /// conteneur tronqué). Ni exception, ni fausse qualité — juste
        /// l'absence de qualité, comme un fichier introuvable.
        /// </summary>
        [Fact]
        public async Task UnFichierVideOuTronqueNeFaitPasPlanterLaDetection()
        {
            var cheminReel = CheminFfmpegDuBuildPrincipal();
            if (cheminReel == null) return; // pas de ffmpeg disponible ici

            var fichier = Path.Combine(Path.GetTempPath(), "cbr-vide-" + Guid.NewGuid().ToString("N") + ".mp4");
            try
            {
                File.WriteAllBytes(fichier, Array.Empty<byte>());

                var qualite = await CaptureFinalizer.DetecterQualiteAsync(fichier, cheminReel);

                Assert.Null(qualite);
            }
            finally
            {
                try { if (File.Exists(fichier)) File.Delete(fichier); } catch { /* dossier temporaire */ }
            }
        }

        private static string? CheminFfmpegDuBuildPrincipal()
        {
            if (File.Exists(AppConfig.FFmpegPath)) return AppConfig.FFmpegPath;

            // Le projet de tests ne copie pas ffmpeg dans sa propre sortie :
            // le build PRINCIPAL, lui, l'a déjà (même dépôt, chemin relatif
            // stable — pas un dépôt privé voisin comme pour StreamRecorderPro).
            var relatif = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bin", "Debug", "net10.0-windows", "ffmpeg.exe");
            var normalise = Path.GetFullPath(relatif);
            return File.Exists(normalise) ? normalise : null;
        }

        private static async Task<bool> GenererClipDeTestAsync(string ffmpegPath, string destination, int largeur, int hauteur)
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            foreach (var a in new[]
            {
                "-y", "-f", "lavfi", "-i", $"testsrc=size={largeur}x{hauteur}:rate=5",
                "-frames:v", "5", "-loglevel", "error", destination
            })
            {
                psi.ArgumentList.Add(a);
            }

            using var p = Process.Start(psi);
            if (p == null) return false;
            await p.StandardError.ReadToEndAsync().ConfigureAwait(false); // draine, jamais bloquer
            await p.WaitForExitAsync().ConfigureAwait(false);

            return File.Exists(destination) && new FileInfo(destination).Length > 0;
        }
    }
}
