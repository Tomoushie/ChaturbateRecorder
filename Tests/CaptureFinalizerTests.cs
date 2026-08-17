using System;
using System.IO;
using System.Threading.Tasks;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le renommage du fichier de capture, sur de VRAIS fichiers.
    ///
    /// Un direct ne se termine jamais seul : yt-dlp est tué, donc il ne
    /// renomme pas son temporaire. Sans cette étape, chaque capture restait un
    /// `.part` — lisible, mais absent de l'historique, qui ne liste que
    /// `.mp4`, `.mkv` et `.mov`. L'application enregistrait donc des vidéos
    /// que personne ne voyait.
    ///
    /// Le portage WPF l'avait perdue et rien ne le signalait : la capture
    /// « marchait », le fichier grossissait, l'écran disait vrai. C'est le
    /// premier essai sur un vrai direct qui l'a montré — trois `.part` dans le
    /// dossier, zéro entrée dans l'historique.
    ///
    /// De vrais fichiers et non une abstraction : ce code n'existe que pour se
    /// battre avec le système de fichiers de Windows, et une doublure ne
    /// rejouerait que ce qu'on croit déjà savoir de lui.
    /// </summary>
    public class CaptureFinalizerTests : IDisposable
    {
        private readonly string _dossier;

        public CaptureFinalizerTests()
        {
            _dossier = Path.Combine(Path.GetTempPath(), "cbr-final-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dossier);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dossier, recursive: true); } catch { }
        }

        private string Chemin(string nom) => Path.Combine(_dossier, nom);

        [Fact]
        public async Task LePartDevientLaVideo()
        {
            File.WriteAllText(Chemin("salon-2026.mp4.part"), "des octets");

            Assert.True(await CaptureFinalizer.FinaliserAsync(_dossier, "salon-2026", "mp4"));

            Assert.True(File.Exists(Chemin("salon-2026.mp4")));
            Assert.False(File.Exists(Chemin("salon-2026.mp4.part")));
            Assert.Equal("des octets", File.ReadAllText(Chemin("salon-2026.mp4")));
        }

        /// <summary>
        /// L'extension SUIT le conteneur choisi : une capture en MKV ne doit
        /// pas ressortir en `.mp4`, ce qui donnerait un fichier au nom menteur.
        /// </summary>
        [Theory]
        [InlineData("mp4")]
        [InlineData("mkv")]
        [InlineData("mov")]
        public async Task LExtensionEstCelleDuConteneur(string extension)
        {
            File.WriteAllText(Chemin($"salon.{extension}.part"), "x");

            Assert.True(await CaptureFinalizer.FinaliserAsync(_dossier, "salon", extension));
            Assert.True(File.Exists(Chemin($"salon.{extension}")));
        }

        /// <summary>
        /// **UN `.part` VIDE N'EST PAS RENOMMÉ** : cela déposerait un fichier de
        /// 0 octet dans l'historique, à côté des vrais enregistrements. Un
        /// démarrage qui échoue immédiatement en produit exactement un.
        /// </summary>
        [Fact]
        public async Task UnPartVideNEstPasRenomme()
        {
            File.WriteAllText(Chemin("salon.mp4.part"), "");

            Assert.False(await CaptureFinalizer.FinaliserAsync(_dossier, "salon", "mp4"));

            Assert.False(File.Exists(Chemin("salon.mp4")));
            // Il RESTE : c'est une trace de la tentative, et l'effacer
            // supprimerait la seule preuve qu'il s'est passe quelque chose.
            Assert.True(File.Exists(Chemin("salon.mp4.part")));
        }

        /// <summary>
        /// Le fichier final existe déjà — yt-dlp renomme lui-même quand le flux
        /// se termine normalement. On ne doit alors RIEN écraser.
        /// </summary>
        [Fact]
        public async Task UnFichierDejaFinalNEstJamaisEcrase()
        {
            File.WriteAllText(Chemin("salon.mp4"), "la vraie capture");
            File.WriteAllText(Chemin("salon.mp4.part"), "un residu");

            Assert.False(await CaptureFinalizer.FinaliserAsync(_dossier, "salon", "mp4"));
            Assert.Equal("la vraie capture", File.ReadAllText(Chemin("salon.mp4")));
        }

        [Fact]
        public async Task SansPartIlNYARienAFaire()
        {
            Assert.False(await CaptureFinalizer.FinaliserAsync(_dossier, "salon", "mp4"));
            Assert.False(File.Exists(Chemin("salon.mp4")));
        }

        /// <summary>
        /// `OutputBaseName` vaut null tant qu'une capture n'a pas démarré. Le
        /// coordinateur finalise sur CHAQUE fin, y compris celle d'un démarrage
        /// qui a levé : ce cas doit passer sans bruit plutôt que jeter.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UnNomAbsentNeFaitRien(string? nomBase)
        {
            Assert.False(await CaptureFinalizer.FinaliserAsync(_dossier, nomBase, "mp4"));
        }

        /// <summary>
        /// LE cas qui a motivé les réessais : le fichier est encore VERROUILLÉ
        /// quand on tente de le renommer, parce que Windows relâche le handle du
        /// processus tué avec un léger retard. On garde le verrou le temps de
        /// deux essais, puis on le lâche — le renommage doit finir par passer.
        ///
        /// Un essai unique échouait ici par intermittence côté WinForms, ce qui
        /// est le pire mode de panne : reproductible une fois sur cinq.
        /// </summary>
        [Fact]
        public async Task UnFichierVerrouilleEstReessayeJusquAPasser()
        {
            var part = Chemin("salon.mp4.part");
            File.WriteAllText(part, "des octets");

            var verrou = new FileStream(part, FileMode.Open, FileAccess.Read, FileShare.Read);
            var liberation = Task.Run(async () =>
            {
                await Task.Delay(CaptureFinalizer.AttenteMs * 2);
                verrou.Dispose();
            });

            var chrono = System.Diagnostics.Stopwatch.StartNew();
            var finalise = await CaptureFinalizer.FinaliserAsync(_dossier, "salon", "mp4");
            chrono.Stop();
            await liberation;

            Assert.True(finalise, "le renommage a abandonne avant que le verrou tombe");
            Assert.True(File.Exists(Chemin("salon.mp4")));

            // ET IL A BIEN REESSAYE : sans cette mesure le test passerait aussi
            // avec un renommage reussi du premier coup, c'est-a-dire sans
            // eprouver la seule chose qu'il est cense eprouver.
            Assert.True(chrono.ElapsedMilliseconds >= CaptureFinalizer.AttenteMs,
                $"rendu en {chrono.ElapsedMilliseconds} ms : le verrou n'a donc " +
                "jamais gene, et les reessais ne sont pas exerces.");
        }
    }
}
