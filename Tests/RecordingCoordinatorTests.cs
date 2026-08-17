using System;
using System.Collections.Generic;
using System.Linq;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le coordinateur d'enregistrement : ce qui reste « en cours », ce qui
    /// mérite une reconnexion, et ce qui conclut.
    ///
    /// Rien de tout cela ne parle au réseau — ce sont des décisions — et
    /// pourtant rien n'était éprouvé, parce que le coordinateur construisait
    /// lui-même son <c>DownloadEngine</c>, donc un vrai yt-dlp. Il reçoit
    /// désormais une FABRIQUE, et ces tests lui passent une doublure.
    ///
    /// Chaque invariant vérifié ici est écrit en commentaire dans le fichier
    /// d'origine avec la panne qu'il évite. Ces pannes ont un point commun :
    /// aucune ne se voit à la compilation, aucune ne lève, et toutes se
    /// manifestent par un symptôme trompeur sur un vrai bureau — un bouton
    /// « Arrêter » sans effet, une capture qui repart après qu'on l'a coupée,
    /// un salon devenu injoignable jusqu'au redémarrage.
    /// </summary>
    public class RecordingCoordinatorTests
    {
        private const string Url = "https://chaturbate.com/un_salon/";
        private const string Autre = "https://chaturbate.com/un_autre/";

        /// <summary>
        /// Un moteur qui ne lance aucun processus, et qui rend les états qu'on
        /// lui dit de rendre, au moment où le vrai les rendrait.
        /// </summary>
        private sealed class MoteurDeDoublure : DownloadEngine
        {
            public int Demarrages;
            public int Arrets;
            public readonly List<string> Sorties = new();

            /// <summary>État rendu PENDANT l'appel à Start, comme le vrai moteur sait le faire.</summary>
            public DownloadState? EtatAuDemarrage;

            /// <summary>État rendu PENDANT l'appel à Stop — tuer yt-dlp fait remonter un échec.</summary>
            public DownloadState? EtatALArret;

            public Exception? LeveAuDemarrage;

            public override void Start(string ytDlpPath, string ffmpegPath, string targetUrl,
                string outputTemplate, string logFilePath, string? formatSelector = null,
                string outputContainer = "mp4", string? cookiesFilePath = null, string? proxyUrl = null,
                int watchdogTimeoutSeconds = 120, long logMaxSizeBytes = 0)
            {
                Demarrages++;
                Sorties.Add(outputTemplate);
                if (LeveAuDemarrage is not null) throw LeveAuDemarrage;
                if (EtatAuDemarrage is { } etat) SetState(etat);
            }

            public override void Stop()
            {
                Arrets++;
                if (EtatALArret is { } etat) SetState(etat);
            }

            /// <summary>Rendre un état à un moment choisi, comme yt-dlp qui se termine.</summary>
            public void Rendre(DownloadState etat) => SetState(etat);
        }

        /// <summary>
        /// Un coordinateur et LE moteur qu'il fabriquera. La fabrique rend
        /// toujours la même doublure : ces tests ne pilotent qu'un salon à la
        /// fois, sauf celui qui en veut deux et qui construit sa fabrique.
        /// </summary>
        private static (RecordingCoordinator, MoteurDeDoublure) Banc()
        {
            var moteur = new MoteurDeDoublure();
            return (new RecordingCoordinator(() => moteur), moteur);
        }

        // --- Ce qui reste « en cours » -------------------------------------

        [Fact]
        public void UnSalonDemarreEstEnCours()
        {
            var (coord, _) = Banc();
            coord.Demarrer(Url);
            Assert.True(coord.EnCours(Url));
            Assert.False(coord.EnCours(Autre));
        }

        /// <summary>
        /// Deux clics rapides ne doivent pas ouvrir deux yt-dlp sur le même
        /// salon : ils écriraient dans deux fichiers et se disputeraient le
        /// flux.
        /// </summary>
        [Fact]
        public void DemarrerDeuxFoisNeLanceQuUneCapture()
        {
            var (coord, moteur) = Banc();
            coord.Demarrer(Url);
            coord.Demarrer(Url);
            Assert.Equal(1, moteur.Demarrages);
        }

        /// <summary>
        /// L'URL ne distingue pas la casse — le magasin de salons normalise
        /// déjà, et une entrée fantôme par variante de casse serait
        /// indétectable depuis l'interface.
        /// </summary>
        [Fact]
        public void LaCasseDeLUrlNeCreePasUnSecondEnregistrement()
        {
            var (coord, moteur) = Banc();
            coord.Demarrer(Url);
            coord.Demarrer(Url.ToUpperInvariant());
            Assert.Equal(1, moteur.Demarrages);
            Assert.True(coord.EnCours(Url.ToUpperInvariant()));
        }

        /// <summary>
        /// LE FANTÔME. Le moteur peut rendre son premier état — y compris
        /// `Failed` — PENDANT l'appel à Start. C'est pour cela que l'entrée est
        /// inscrite AVANT de démarrer : inscrite après, ce retrait porterait sur
        /// une entrée absente, puis l'ajout déposerait une entrée que plus rien
        /// n'enlèverait. `EnCours` resterait vrai POUR TOUJOURS, et le bouton
        /// afficherait « Arrêter » sur une capture terminée.
        /// </summary>
        [Fact]
        public void UnEchecPendantLeDemarrageNeLaissePasDeFantome()
        {
            var (coord, moteur) = Banc();
            moteur.EtatAuDemarrage = DownloadState.Failed;

            coord.Demarrer(Url);

            Assert.False(coord.EnCours(Url));
        }

        /// <summary>
        /// Un démarrage qui LÈVE doit laisser la table propre — sans quoi le
        /// salon serait injoignable jusqu'au redémarrage — et l'exception doit
        /// remonter : c'est elle qui écrit l'échec sur la carte.
        /// </summary>
        [Fact]
        public void UnDemarrageQuiLeveNettoieEtRemonte()
        {
            var (coord, moteur) = Banc();
            moteur.LeveAuDemarrage = new InvalidOperationException("yt-dlp absent");

            Assert.Throws<InvalidOperationException>(() => coord.Demarrer(Url));
            Assert.False(coord.EnCours(Url));
        }

        // --- Ce qui mérite une reconnexion ---------------------------------

        /// <summary>
        /// **SEUL `Failed` déclenche une reconnexion.** `Stopped` est un arrêt
        /// DEMANDÉ et `Completed` une fin normale : rejouer l'un ou l'autre
        /// relancerait une capture que personne n'a demandée — et, sur un salon
        /// qui vient de fermer, indéfiniment.
        /// </summary>
        [Theory]
        [InlineData(DownloadState.Completed)]
        [InlineData(DownloadState.Stopped)]
        public void UneFinNormaleNeReconnectePas(DownloadState fin)
        {
            var (coord, moteur) = Banc();
            var reconnexions = 0;
            coord.ReconnexionProgrammee += (u, d, t, m) => reconnexions++;

            coord.Demarrer(Url, reconnexionAuto: true);
            moteur.Rendre(fin);

            Assert.Equal(0, reconnexions);
            Assert.False(coord.EnCours(Url));
            Assert.Equal(1, moteur.Demarrages);
        }

        /// <summary>
        /// Un échec avec la reconnexion armée programme une tentative, et
        /// l'entrée RESTE : du point de vue de l'utilisateur ce salon est
        /// toujours pris en charge, et libérer la place laisserait démarrer une
        /// SECONDE capture pendant que la première attend sa nouvelle tentative.
        /// </summary>
        [Fact]
        public void UnEchecAvecReconnexionGardeLeSalonPrisEnCharge()
        {
            var (coord, moteur) = Banc();
            var annonces = new List<(int Delai, int Tentative, int Max)>();
            coord.ReconnexionProgrammee += (u, d, t, m) => annonces.Add((d, t, m));

            coord.Demarrer(Url, reconnexionAuto: true);
            moteur.Rendre(DownloadState.Failed);

            Assert.True(coord.EnCours(Url));
            var annonce = Assert.Single(annonces);
            Assert.Equal(1, annonce.Tentative);
            Assert.Equal(AppConfig.AutoReconnectMaxAttempts, annonce.Max);
            Assert.Equal(AppConfig.AutoReconnectDelaySeconds, annonce.Delai);
        }

        [Fact]
        public void UnEchecSansReconnexionConclut()
        {
            var (coord, moteur) = Banc();
            var etats = new List<DownloadState>();
            coord.EtatChange += (u, e) => etats.Add(e);

            coord.Demarrer(Url, reconnexionAuto: false);
            moteur.Rendre(DownloadState.Failed);

            Assert.False(coord.EnCours(Url));
            Assert.Equal(DownloadState.Failed, Assert.Single(etats));
        }

        /// <summary>
        /// Le plafond de tentatives EXISTE. Sans lui, un salon définitivement
        /// mort relancerait un yt-dlp toutes les trente secondes jusqu'à la
        /// fermeture de l'application.
        /// </summary>
        [Fact]
        public void LesTentativesSArretentAuPlafond()
        {
            var (coord, moteur) = Banc();
            var reconnexions = 0;
            coord.ReconnexionProgrammee += (u, d, t, m) => reconnexions++;
            var max = AppConfig.AutoReconnectMaxAttempts;

            coord.Demarrer(Url, reconnexionAuto: true);
            for (var i = 0; i <= max; i++) moteur.Rendre(DownloadState.Failed);

            Assert.Equal(max, reconnexions);
            Assert.False(coord.EnCours(Url));
        }

        // --- L'arrêt -------------------------------------------------------

        /// <summary>
        /// **COUPER LA RECONNEXION AVANT D'ARRÊTER LE MOTEUR.** Tuer yt-dlp fait
        /// remonter un état d'échec ; si le drapeau était encore armé, un arrêt
        /// DEMANDÉ PAR L'UTILISATEUR relancerait aussitôt une capture. C'est le
        /// défaut le plus perfide de ce fichier : l'application ferait
        /// exactement le contraire de ce qu'on vient de lui demander, et le
        /// ferait à chaque fois.
        /// </summary>
        [Fact]
        public void ArreterNeRelancePasUneCaptureMemeSiLeMoteurRendUnEchec()
        {
            var (coord, moteur) = Banc();
            var reconnexions = 0;
            coord.ReconnexionProgrammee += (u, d, t, m) => reconnexions++;
            moteur.EtatALArret = DownloadState.Failed;

            coord.Demarrer(Url, reconnexionAuto: true);
            coord.Arreter(Url);

            Assert.Equal(0, reconnexions);
            Assert.Equal(1, moteur.Demarrages);
            Assert.False(coord.EnCours(Url));
        }

        /// <summary>
        /// **ARRÊTER PENDANT UNE RECONNEXION EN ATTENTE NE TUE AUCUN PROCESSUS**
        /// — il n'y en a pas — donc le moteur ne rendra AUCUN état, donc rien ne
        /// viendrait retirer l'entrée ni repeindre la carte. Elle resterait
        /// affichée « reconnexion » avec un bouton « Arrêter » sans effet, et le
        /// salon serait injoignable pour toujours. Le coordinateur conclut donc
        /// lui-même dans ce cas précis.
        /// </summary>
        [Fact]
        public void ArreterPendantUneReconnexionEnAttenteConclutQuandMeme()
        {
            var (coord, moteur) = Banc();
            var etats = new List<DownloadState>();
            coord.EtatChange += (u, e) => etats.Add(e);

            coord.Demarrer(Url, reconnexionAuto: true);
            moteur.Rendre(DownloadState.Failed);   // une reconnexion est en attente
            Assert.True(coord.EnCours(Url));

            var arretsAvant = moteur.Arrets;
            coord.Arreter(Url);

            Assert.False(coord.EnCours(Url));
            Assert.Equal(DownloadState.Stopped, etats.Last());
            // Aucun processus a tuer : le moteur n'est meme pas sollicite.
            Assert.Equal(arretsAvant, moteur.Arrets);
        }

        [Fact]
        public void ArreterUnSalonQuiNeTournePasNeFaitRien()
        {
            var (coord, moteur) = Banc();
            coord.Arreter(Url);
            Assert.Equal(0, moteur.Arrets);
            Assert.False(coord.EnCours(Url));
        }

        /// <summary>
        /// Après un arrêt, le même salon doit pouvoir repartir. Une entrée
        /// oubliée dans la table le rendrait définitivement muet — et c'est
        /// silencieux, puisque `Demarrer` sort sans rien dire.
        /// </summary>
        [Fact]
        public void UnSalonArreteePeutRepartir()
        {
            var (coord, moteur) = Banc();
            moteur.EtatALArret = DownloadState.Stopped;

            coord.Demarrer(Url);
            coord.Arreter(Url);
            coord.Demarrer(Url);

            Assert.True(coord.EnCours(Url));
            Assert.Equal(2, moteur.Demarrages);
        }

        /// <summary>
        /// **LE DÉFAUT LE PLUS COÛTEUX TROUVÉ SUR UN VRAI DIRECT** : fermer
        /// l'application laissait ses yt-dlp EN VIE.
        ///
        /// Rien n'arrêtait les captures à la sortie — le modèle de vue libérait
        /// la surveillance, les journaux et les cartes, jamais le coordinateur.
        /// Les processus continuaient d'écrire dans leur `.part`, que plus
        /// personne n'allait renommer : un live ne se termine jamais seul, c'est
        /// l'application qui coupe. Constaté sur la machine du mainteneur —
        /// quatre `yt-dlp.exe` orphelins, dont un manifestement actif, alors
        /// qu'aucune fenêtre n'était ouverte.
        /// </summary>
        [Fact]
        public void ArreterToutCoupeChaqueCaptureEnCours()
        {
            var moteurs = new List<MoteurDeDoublure>();
            var coord = new RecordingCoordinator(() =>
            {
                var m = new MoteurDeDoublure { EtatALArret = DownloadState.Stopped };
                moteurs.Add(m);
                return m;
            });

            coord.Demarrer(Url);
            coord.Demarrer(Autre);

            coord.ArreterTout();

            Assert.False(coord.EnCours(Url));
            Assert.False(coord.EnCours(Autre));
            Assert.All(moteurs, m => Assert.Equal(1, m.Arrets));
        }

        /// <summary>
        /// Et il ne doit pas lever sur une table vide : c'est le dernier geste
        /// avant la sortie, et une exception y empêcherait le ménage suivant.
        /// </summary>
        [Fact]
        public void ArreterToutSansAucuneCaptureNeFaitRien()
        {
            var (coord, moteur) = Banc();
            coord.ArreterTout();
            Assert.Equal(0, moteur.Arrets);
        }

        // --- Plusieurs salons de front -------------------------------------

        /// <summary>
        /// Les évènements du moteur ne portent PAS l'URL : le moteur ne connaît
        /// qu'un processus, pas le salon qui l'a demandé. C'est la fermeture
        /// posée à l'abonnement qui fait le lien — et c'est aussi ce qui permet
        /// à plusieurs captures de tourner de front. Si ce lien se défaisait,
        /// l'échec d'un salon conclurait celui d'un autre.
        /// </summary>
        [Fact]
        public void LEchecDUnSalonNeConclutPasCeluiDunAutre()
        {
            var a = new MoteurDeDoublure();
            var b = new MoteurDeDoublure();
            var suivant = new Queue<MoteurDeDoublure>(new[] { a, b });
            var coord = new RecordingCoordinator(() => suivant.Dequeue());

            var conclus = new List<string>();
            coord.EtatChange += (u, e) => conclus.Add(u);

            coord.Demarrer(Url);
            coord.Demarrer(Autre);
            a.Rendre(DownloadState.Failed);

            Assert.False(coord.EnCours(Url));
            Assert.True(coord.EnCours(Autre));
            Assert.Equal(Url, Assert.Single(conclus));
            Assert.Equal(1, b.Demarrages);
        }

        /// <summary>
        /// Chaque salon a SON moteur, donc son propre processus yt-dlp : c'est
        /// ce qui permet d'enregistrer plusieurs directs sans ouvrir plusieurs
        /// instances de l'application.
        /// </summary>
        [Fact]
        public void ChaqueSalonRecoitSonPropreMoteur()
        {
            var construits = new List<MoteurDeDoublure>();
            var coord = new RecordingCoordinator(() =>
            {
                var m = new MoteurDeDoublure();
                construits.Add(m);
                return m;
            });

            coord.Demarrer(Url);
            coord.Demarrer(Autre);

            Assert.Equal(2, construits.Count);
            Assert.NotSame(construits[0], construits[1]);
            Assert.All(construits, m => Assert.Equal(1, m.Demarrages));
        }

        // --- Le nom de sortie ----------------------------------------------

        /// <summary>
        /// Le gabarit de sortie porte le nom du salon et une extension laissée à
        /// yt-dlp. Il est régénéré à CHAQUE tentative — un horodatage frais donne
        /// un fichier et un journal distincts par tentative, alors que réutiliser
        /// le précédent ferait écraser la capture déjà obtenue par celle de la
        /// reconnexion, c'est-à-dire perdre ce qu'on venait de sauver.
        ///
        /// Seul le premier gabarit est vérifiable ici : le second n'est produit
        /// qu'au déclenchement du minuteur de reconnexion, qui exige une boucle
        /// de répartition.
        /// </summary>
        [Fact]
        public void LeGabaritDeSortieLaisseLExtensionAYtDlp()
        {
            var (coord, moteur) = Banc();
            coord.Demarrer(Url);

            var gabarit = Assert.Single(moteur.Sorties);
            Assert.Contains("%(ext)s", gabarit);
            Assert.Contains(Platforms.DisplayName(Url), gabarit);
        }
    }
}
