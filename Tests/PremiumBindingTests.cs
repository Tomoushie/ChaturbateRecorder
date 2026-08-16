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
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => true;
        }

        private sealed class SansVersion
        {
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
        }

        private sealed class MauvaiseSignature
        {
            public string Version => "1.0.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            // int au lieu de bool, et un paramètre en moins.
            public int TryCapturePreview(string a, string b, string c) => 0;
        }

        private sealed class ConstructeurAvecArgument
        {
            public ConstructeurAvecArgument(string obligatoire) { _ = obligatoire; }
            public string Version => "1.0.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
        }

        private sealed class ConstructeurQuiExplose
        {
            public ConstructeurQuiExplose() => throw new InvalidOperationException("clé publique corrompue");
            public string Version => "1.0.0";
            public string LicensedTo => "";
            public string LicenceProblem => "";
            public bool TryCapturePreview(string a, string b, string c, string d, int e) => false;
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
        public void AMethodWithTheWrongSignatureIsRefused()
        {
            var lie = PremiumBinding.Bind(typeof(MauvaiseSignature), out var probleme);

            Assert.Null(lie);
            Assert.Contains("TryCapturePreview", probleme);
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
