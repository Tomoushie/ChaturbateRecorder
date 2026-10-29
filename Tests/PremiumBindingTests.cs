using System;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Liage du composant premium (97.0).
    ///
    /// **Pourquoi ces tests existent** : le contrat entre l'application libre et
    /// le composant fermé passe par la réflexion, donc AUCUN compilateur ne le
    /// vérifie. Une propriété renommée d'un côté produirait un premium
    /// silencieusement inerte chez l'acheteur — le pire des défauts, puisqu'il
    /// touche exactement les gens qui ont payé et qu'il ne lève rien.
    ///
    /// Chaque cas de refus est éprouvé sur un type volontairement incomplet :
    /// c'est ce qui garantit que le liage refuse pour la BONNE raison, et pas
    /// par accident.
    /// </summary>
    public class PremiumBindingTests
    {
        // --- Types de composants factices, un par cas ---

        private sealed class Conforme
        {
            public string Version => "1.0.0";
            public string LicensedTo => "Jane Doe";
            public string LicenceProblem => "";
            public string UsageSummary => "Vignette : 3";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => true;
            public bool TryShowLiveWindow(string a, string b, string c, string d, int e) => true;
        }

        /// <summary>Composant v2.3 : le contrat historique PLUS les membres
        /// optionnels. C'est ce que la nouvelle DLL expose.</summary>
        private sealed class ConformeV23
        {
            public string Version => "2.3.0";
            public string LicensedTo => "Jane Doe";
            public string LicenceProblem => "";
            public string UsageSummary => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => true;
            public bool TryShowLiveWindow(string a, string b, string c, string d, int e) => true;
            public bool TryCapturePreviewWithSession(string a, string b, string c, string d,
                                                     string e, string f, int g) => true;
            public bool TryShowLiveWindowEx(string a, string b, string c, string d,
                                            bool e, bool f, int g) => true;
        }

        /// <summary>Un membre optionnel dont la signature a changé doit être
        /// ignoré, PAS refuser tout le composant : une DLL d'une autre version
        /// ne doit jamais rendre le premium inerte.</summary>
        private sealed class OptionnelleMauvaiseSignature
        {
            public string Version => "2.3.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public string UsageSummary => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
            public bool TryShowLiveWindow(string a, string b, string c, string d, int e) => false;
            public bool TryShowLiveWindowEx(string a, string b, string c, string d,
                                            bool e, bool f, int g) => true; // correcte
            public string TryCapturePreviewWithSession(string a, string b, string c, string d,
                                                       string e, string f, int g) => ""; // mauvais type
        }

        private sealed class SansVersion
        {
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public string UsageSummary => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
            public bool TryShowLiveWindow(string a, string b, string c, string d, int e) => false;
        }

        /// <summary>
        /// Le pendant de <see cref="SansVersion"/> pour le troisième membre
        /// (24-08, compteurs d'usage) : tout le reste est honoré, SEUL
        /// UsageSummary manque. Sans ce cas, une régression sur ce contrôle
        /// précis passerait inaperçue — les autres fixtures qui manquent
        /// quelque chose échouent toutes AVANT d'atteindre ce contrôle-ci.
        /// </summary>
        private sealed class SansUsageSummary
        {
            public string Version => "1.0.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
            public bool TryShowLiveWindow(string a, string b, string c, string d, int e) => false;
        }

        private sealed class MauvaiseSignature
        {
            public string Version => "1.0.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public string UsageSummary => "";
            // int au lieu de bool, et un paramètre en moins.
            public int TryCapturePreview(string a, string b, string c) => 0;
            public bool TryShowLiveWindow(string a, string b, string c, string d, int e) => false;
        }

        /// <summary>
        /// Le pendant de <see cref="MauvaiseSignature"/> pour le second
        /// membre (v2, vidéo en direct) : tout le reste du contrat est
        /// honoré, SEUL TryShowLiveWindow a une mauvaise signature. Sans ce
        /// cas, une régression sur ce contrôle précis passerait inaperçue —
        /// <see cref="MauvaiseSignature"/> ne l'exerce jamais, puisqu'il est
        /// déjà refusé plus tôt sur TryCapturePreview.
        /// </summary>
        private sealed class MauvaiseSignatureVideo
        {
            public string Version => "1.0.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public string UsageSummary => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
            // string au lieu de bool.
            public string TryShowLiveWindow(string a, string b, string c, string d, int e) => "";
        }

        private sealed class ConstructeurAvecArgument
        {
            public ConstructeurAvecArgument(string obligatoire) { _ = obligatoire; }
            public string Version => "1.0.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public string UsageSummary => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
            public bool TryShowLiveWindow(string a, string b, string c, string d, int e) => false;
        }

        private sealed class ConstructeurQuiExplose
        {
            public ConstructeurQuiExplose() => throw new InvalidOperationException("clé publique corrompue");
            public string Version => "1.0.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public string UsageSummary => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
            public bool TryShowLiveWindow(string a, string b, string c, string d, int e) => false;
        }

        // --- Cas nominal ---

        [Fact]
        public void AConformingModuleBindsAndAnswers()
        {
            var lie = PremiumBinding.Bind(typeof(Conforme), out var probleme);

            Assert.NotNull(lie);
            Assert.Equal("", probleme);
            Assert.Equal("1.0.0", lie!.Version);
            Assert.Equal("Jane Doe", lie.LicensedTo);
            Assert.True(lie.TryCapturePreview("yt", "ff", "url", "out.jpg", 20));
            Assert.True(lie.TryShowLiveWindow("yt", "ff", "url", "Salon", 20));
            Assert.Equal("Vignette : 3", lie.UsageSummary);
        }

        /// <summary>
        /// Le test qui protège le CONTRAT RÉEL : la DLL effectivement livrée
        /// doit exposer les signatures exactes de la v2.3. Les fixtures
        /// précédentes prouvent le liage, pas que la DLL expédiée est à jour.
        /// </summary>
        [Fact]
        public void TheShippedV23DllExposesTheExtendedContract()
        {
            var chemin = System.IO.Path.Combine(System.AppContext.BaseDirectory, "StreamRecorderPro.dll");
            Assert.True(System.IO.File.Exists(chemin), $"DLL premium absente du dossier de test : {chemin}");

            var assembly = System.Reflection.Assembly.LoadFrom(chemin);
            var type = assembly.GetType("StreamRecorderPro.PremiumModule", throwOnError: false);
            Assert.NotNull(type);

            Assert.NotNull(type!.GetMethod("TryShowLiveWindowEx", new[]
            {
                typeof(string), typeof(string), typeof(string), typeof(string),
                typeof(bool), typeof(bool), typeof(int)
            }));
            Assert.NotNull(type.GetMethod("TryCapturePreviewWithSession", new[]
            {
                typeof(string), typeof(string), typeof(string), typeof(string),
                typeof(string), typeof(string), typeof(int)
            }));
            Assert.NotNull(type.GetMethod("TryShowMultiLive", new[]
            {
                typeof(string), typeof(string), typeof(string), typeof(string),
                typeof(string), typeof(bool), typeof(int)
            }));
            Assert.NotNull(type.GetMethod("TrySaveReplayClip", new[]
            {
                typeof(string), typeof(string), typeof(string), typeof(int)
            }));
            Assert.NotNull(type.GetMethod("ExportMarkerClips", new[]
            {
                typeof(string), typeof(string), typeof(string), typeof(string), typeof(int)
            }));

            var lie = PremiumBinding.Bind(type, out var probleme);
            Assert.NotNull(lie);
            Assert.Equal("", probleme);
        }

        /// <summary>
        /// La DLL v2.3 expose les nouveaux membres : ils doivent être résolus.
        /// </summary>
        [Fact]
        public void AV23ModuleExposesOptionalMembers()
        {
            var lie = PremiumBinding.Bind(typeof(ConformeV23), out var probleme);

            Assert.NotNull(lie);
            Assert.Equal("", probleme);
            Assert.True(lie!.TryShowLiveWindowEx("yt", "ff", "url", "Salon", true, true, 20));
            Assert.True(lie.TryCapturePreviewWithSession("yt", "ff", "url", "out.jpg", "cookies.txt", "socks5://x", 20));
        }

        /// <summary>
        /// LE CAS COMMERCIAL QUI COMPTE : une application mise à jour avec
        /// l'ANCIENNE DLL de l'acheteur. Le composant doit rester utilisable
        /// (vignette, direct) et seules les nouvelles fonctions sont
        /// indisponibles — jamais tout le premium.
        /// </summary>
        [Fact]
        public void AnOlderModuleStillBindsAndOptionalMembersStayUnavailable()
        {
            var lie = PremiumBinding.Bind(typeof(Conforme), out var probleme);

            Assert.NotNull(lie);
            Assert.Equal("", probleme);
            Assert.True(lie!.TryCapturePreview("yt", "ff", "url", "out.jpg", 20));
            Assert.False(lie.TryShowLiveWindowEx("yt", "ff", "url", "Salon", true, true, 20));
            // Repli : l'ancien aperçu reste disponible, sans cookies.
            Assert.True(lie.TryCapturePreviewWithSession("yt", "ff", "url", "out.jpg", "", "", 20));
        }

        /// <summary>
        /// Un membre optionnel mal typé est ignoré (traité comme absent) au lieu
        /// de faire échouer le liage entier.
        /// </summary>
        [Fact]
        public void AnOptionalMemberWithTheWrongSignatureIsIgnored()
        {
            var lie = PremiumBinding.Bind(typeof(OptionnelleMauvaiseSignature), out var probleme);

            Assert.NotNull(lie);
            Assert.Equal("", probleme);
            Assert.True(lie!.TryShowLiveWindowEx("yt", "ff", "url", "Salon", true, true, 20));
            Assert.False(lie.TryCapturePreviewWithSession("yt", "ff", "url", "out.jpg", "", "", 20));
        }

        // --- Chaque refus, pour la bonne raison ---

        [Fact]
        public void AMissingPropertyIsRefusedAndNamed()
        {
            var lie = PremiumBinding.Bind(typeof(SansVersion), out var probleme);

            Assert.Null(lie);
            // Le motif doit DÉSIGNER le membre manquant : « composant invalide »
            // n'aiderait personne à réparer.
            Assert.Contains("Version", probleme);
        }

        [Fact]
        public void AMissingUsageSummaryIsRefusedAndNamed()
        {
            var lie = PremiumBinding.Bind(typeof(SansUsageSummary), out var probleme);

            Assert.Null(lie);
            Assert.Contains("UsageSummary", probleme);
        }

        [Fact]
        public void AMethodWithTheWrongSignatureIsRefused()
        {
            var lie = PremiumBinding.Bind(typeof(MauvaiseSignature), out var probleme);

            Assert.Null(lie);
            Assert.Contains("TryCapturePreview", probleme);
        }

        [Fact]
        public void ALiveWindowMethodWithTheWrongSignatureIsRefused()
        {
            var lie = PremiumBinding.Bind(typeof(MauvaiseSignatureVideo), out var probleme);

            Assert.Null(lie);
            Assert.Contains("TryShowLiveWindow", probleme);
        }

        [Fact]
        public void AModuleWithoutAParameterlessConstructorIsRefused()
        {
            var lie = PremiumBinding.Bind(typeof(ConstructeurAvecArgument), out var probleme);

            Assert.Null(lie);
            Assert.Contains("constructeur", probleme);
        }

        /// <summary>
        /// Le cas qui compte le plus : le composant existe, honore le contrat,
        /// et lève au démarrage. Sans capture, l'exception traverserait le
        /// chargeur et empêcherait l'APPLICATION LIBRE de démarrer — un
        /// composant optionnel ferait tomber le produit gratuit.
        /// </summary>
        [Fact]
        public void AConstructorThatThrowsCannotTakeTheApplicationDown()
        {
            var lie = PremiumBinding.Bind(typeof(ConstructeurQuiExplose), out var probleme);

            Assert.Null(lie);
            // La cause RÉELLE, pas l'enveloppe TargetInvocationException.
            Assert.Contains("clé publique corrompue", probleme);
        }

        /// <summary>
        /// Un type qui n'a rien à voir — la DLL déposée n'est pas la bonne.
        /// </summary>
        [Fact]
        public void AnUnrelatedTypeIsRefusedWithoutThrowing()
        {
            var lie = PremiumBinding.Bind(typeof(string), out var probleme);

            Assert.Null(lie);
            Assert.NotEqual("", probleme);
        }
    }
}
