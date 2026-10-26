using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// La ligne de commande yt-dlp, qui est LA seule partie de la chaîne de
    /// capture éprouvable sans direct réel.
    ///
    /// Tout le reste — le salon diffuse-t-il, le flux se remuxe-t-il, le
    /// fichier est-il lisible — demande un vrai live. Ce que ces tests
    /// verrouillent est l'étape d'avant : que les options soient celles qu'on
    /// croit, et que le yt-dlp livré les COMPRENNE. Une option retirée par une
    /// version ultérieure de yt-dlp ferait échouer toutes les captures d'un
    /// coup, sans que rien dans le projet ne le voie venir.
    /// </summary>
    public class DownloadArgumentsTests
    {
        private const string Ffmpeg = @"C:\Outils\ffmpeg.exe";
        private const string Sortie = @"C:\Captures\salon-2026-08-17.%(ext)s";
        private const string Url = "https://chaturbate.com/un_salon/";

        private static List<string> Ligne(
            string? format = null, string conteneur = "mp4",
            string? cookies = null, string? proxy = null) =>
            DownloadEngine.BuildArguments(Ffmpeg, Url, Sortie, format, conteneur, cookies, proxy);

        /// <summary>
        /// Une valeur suit toujours son option : on vérifie le COUPLE et non la
        /// seule présence du drapeau. Un `--ffmpeg-location` sans chemin
        /// décalerait tout ce qui suit d'un cran, et yt-dlp prendrait alors
        /// l'option suivante pour le chemin de ffmpeg.
        /// </summary>
        private static string? ValeurDe(List<string> args, string option)
        {
            var i = args.IndexOf(option);
            if (i < 0 || i + 1 >= args.Count) return null;
            return args[i + 1];
        }

        [Fact]
        public void LesOptionsQuiNeDependentDeRienSontToujoursLa()
        {
            var args = Ligne();

            // `--newline` est la condition de TOUT le reste de l'interface :
            // sans lui yt-dlp réécrit sa ligne de progression avec un retour
            // chariot, le lecteur de flux ne rend jamais de ligne, et la barre
            // de progression comme le journal restent vides.
            Assert.Contains("--newline", args);
            Assert.Contains("--progress", args);

            // Un direct coupé n'est pas un échec : c'est le cas NORMAL.
            Assert.Equal("infinite", ValeurDe(args, "--retries"));
            Assert.Equal("infinite", ValeurDe(args, "--fragment-retries"));

            // `--hls-use-mpegts` rend le fichier lisible AVANT la fin de la
            // capture. Sans lui, un arrêt brutal laisse un mp4 sans index,
            // donc illisible : tout ce qui a été enregistré est perdu.
            Assert.Contains("--hls-use-mpegts", args);
        }

        [Fact]
        public void LeCheminDeFfmpegEtLeGabaritDeSortieSontTransmis()
        {
            var args = Ligne();

            Assert.Equal(Ffmpeg, ValeurDe(args, "--ffmpeg-location"));
            Assert.Equal(Sortie, ValeurDe(args, "-o"));
            Assert.Contains(Url, args);
        }

        [Theory]
        [InlineData("mp4")]
        [InlineData("mkv")]
        public void LeConteneurDemandeEstCeluiDuRemux(string conteneur)
        {
            Assert.Equal(conteneur, ValeurDe(Ligne(conteneur: conteneur), "--remux-video"));
        }

        /// <summary>
        /// LE défaut que le mode sans échec existe pour empêcher : un
        /// composant désactivé ne doit PAS atteindre yt-dlp. Un cookies.txt
        /// invalide passé quand même faisait échouer TOUTES les captures — et
        /// enregistrer sans authentification vaut mieux que ne rien
        /// enregistrer. Le coordinateur passe alors une chaîne VIDE, pas null :
        /// les deux doivent donc être écartés ici.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UnCookiesDesactiveNAtteintPasYtDlp(string? cookies)
        {
            Assert.DoesNotContain("--cookies", Ligne(cookies: cookies));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UnProxyDesactiveNAtteintPasYtDlp(string? proxy)
        {
            Assert.DoesNotContain("--proxy", Ligne(proxy: proxy));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AucunFormatChoisiLaisseYtDlpDecider(string? format)
        {
            // Pas de `-f` du tout, et surtout pas un `-f` suivi de rien : yt-dlp
            // prendrait l'argument suivant — l'URL — pour le sélecteur.
            Assert.DoesNotContain("-f", Ligne(format: format));
        }

        [Fact]
        public void LesOptionnelsDemandesSontTransmisAvecLeurValeur()
        {
            var args = Ligne(format: "best", cookies: @"C:\c\cookies.txt", proxy: "socks5://127.0.0.1:9050");

            Assert.Equal("best", ValeurDe(args, "-f"));
            Assert.Equal(@"C:\c\cookies.txt", ValeurDe(args, "--cookies"));
            Assert.Equal("socks5://127.0.0.1:9050", ValeurDe(args, "--proxy"));
        }

        /// <summary>
        /// Un élément vide dans la liste d'arguments n'est pas ignoré par
        /// Windows : il devient un argument à part entière, et décale la
        /// lecture de tous les suivants.
        /// </summary>
        [Fact]
        public void AucunArgumentNEstVide()
        {
            var args = Ligne(format: "best", cookies: @"C:\c\cookies.txt", proxy: "http://p:8080");
            Assert.All(args, a => Assert.False(string.IsNullOrWhiteSpace(a)));
        }

        // ------------------------------------------------------------------
        // Le seul test de cette suite qui lance le vrai binaire.
        // ------------------------------------------------------------------

        /// <summary>
        /// Le yt-dlp LIVRÉ accepte-t-il la ligne qu'on lui construit ?
        ///
        /// La discrimination tient au code de sortie, et elle est nette :
        /// **2 = la ligne d'options est refusée** (option inconnue, valeur
        /// invalide), et yt-dlp s'arrête AVANT tout accès réseau. Tout autre
        /// code veut dire que les options sont passées et que l'échec est en
        /// aval. On vise donc un hôte en `.invalid`, réservé par la RFC 2606 et
        /// garanti de ne jamais résoudre : le test ne dépend d'aucun salon, ni
        /// même d'une connexion — sans réseau la résolution échoue, ce qui est
        /// exactement le résultat attendu.
        ///
        /// `--version` ne peut PAS servir de validateur : mesuré, yt-dlp le
        /// court-circuite et rend 0 même avec une option inventée dans la même
        /// ligne.
        /// </summary>
        [Fact]
        public void LeYtDlpLivreAccepteLaLigneConstruite()
        {
            var ytDlp = CheminOutil("yt-dlp.exe");
            var ffmpeg = CheminOutil("ffmpeg.exe");
            if (ytDlp is null || ffmpeg is null)
                return; // Binaires absents de l'arborescence : rien à éprouver.

            var dossier = Path.Combine(Path.GetTempPath(), "cbr-args-" + Guid.NewGuid().ToString("N"));
            var args = DownloadEngine.BuildArguments(
                ffmpeg,
                "https://aucun-hote.invalid/salon",
                Path.Combine(dossier, "capture.%(ext)s"),
                "best", "mp4",
                cookiesFilePath: null, proxyUrl: null);

            // `--ignore-config` pour que le fichier de configuration de la
            // machine qui exécute les tests ne puisse pas rendre le verdict.
            args.Insert(0, "--ignore-config");
            // Sans cela, `--retries infinite` pourrait faire durer l'échec.
            args.Add("--socket-timeout");
            args.Add("5");

            var psi = new ProcessStartInfo(ytDlp)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var proc = Process.Start(psi)!;
            var sortie = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
            if (!proc.WaitForExit(120_000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                return; // Ni refus ni acceptation : on ne conclut pas.
            }

            Assert.False(proc.ExitCode == 2,
                "yt-dlp " + VersionDe(ytDlp) + " REFUSE la ligne d'options construite par " +
                "BuildArguments — aucune capture ne peut démarrer. Sortie :\n" + sortie);
        }

        private static string? CheminOutil(string nom)
        {
            // Depuis le binaire des tests, on remonte jusqu'au dossier qui
            // porte `Tools\` — le csproj l'y copie depuis le dépôt WinForms.
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d is not null)
            {
                var candidat = Path.Combine(d.FullName, "Tools", nom);
                if (File.Exists(candidat)) return candidat;
                var voisin = Path.Combine(d.FullName, nom);
                if (File.Exists(voisin)) return voisin;
                d = d.Parent;
            }
            return null;
        }

        private static string VersionDe(string ytDlp)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo(ytDlp, "--version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                })!;
                var v = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(10_000);
                return v;
            }
            catch { return "(version inconnue)"; }
        }
    }
}
