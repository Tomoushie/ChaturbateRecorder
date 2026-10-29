using System;
using System.Collections.Generic;
using System.IO;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Installation d'un licence.key depuis la fenêtre Premium (2.2.0).
    ///
    /// Le point qui compte n'est pas la copie — c'est la RESTAURATION : un
    /// acheteur qui choisit le mauvais fichier, ou une licence liée à une autre
    /// machine, ne doit pas perdre celle qui fonctionnait. Ce scénario est
    /// difficile à provoquer à la main (il faut un vrai composant premium et
    /// deux fichiers), donc il est éprouvé ici sur un dossier temporaire.
    /// </summary>
    public class LicenceInstallerTests : IDisposable
    {
        private readonly List<string> _dossiers = new();

        private string Dossier()
        {
            var chemin = Path.Combine(Path.GetTempPath(), "srp-licence-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(chemin);
            _dossiers.Add(chemin);
            return chemin;
        }

        [Fact]
        public void UneLicenceAccepteeEstCopieeEtLAncienneEstSauvegardee()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            var source = Path.Combine(dossier, "nouvelle.key");
            File.WriteAllText(cible, "ancienne");
            File.WriteAllText(source, "nouvelle");

            var resultat = LicenceInstaller.Installer(source, () => true, cible);

            Assert.True(resultat.Succes);
            Assert.Equal("nouvelle", File.ReadAllText(cible));
            Assert.Equal("ancienne", File.ReadAllText(cible + LicenceInstaller.BackupSuffix));
        }

        [Fact]
        public void UneLicenceRefuseeRestaureLAncienneEtRelitLeComposant()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            var source = Path.Combine(dossier, "mauvaise.key");
            File.WriteAllText(cible, "ancienne");
            File.WriteAllText(source, "mauvaise");

            var relectures = 0;
            var resultat = LicenceInstaller.Installer(
                source,
                () => { relectures++; return false; },
                cible);

            Assert.False(resultat.Succes);
            Assert.Equal(IssueInstallation.LicenceRefusee, resultat.Issue);
            Assert.Equal("ancienne", File.ReadAllText(cible));
            // Une fois pour valider le nouveau fichier, une fois après
            // restauration : l'état en mémoire doit redevenir celui du disque.
            Assert.Equal(2, relectures);
        }

        [Fact]
        public void UneLicenceRefuseeSansAncienneSupprimeLeFichier()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            var source = Path.Combine(dossier, "mauvaise.key");
            File.WriteAllText(source, "mauvaise");

            var resultat = LicenceInstaller.Installer(source, () => false, cible);

            Assert.Equal(IssueInstallation.LicenceRefusee, resultat.Issue);
            Assert.False(File.Exists(cible));
        }

        [Fact]
        public void UnFichierSourceAbsentNeTouchePasALaLicenceEnPlace()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            File.WriteAllText(cible, "ancienne");

            var resultat = LicenceInstaller.Installer(
                Path.Combine(dossier, "inexistant.key"), () => true, cible);

            Assert.Equal(IssueInstallation.FichierSource, resultat.Issue);
            Assert.Equal("ancienne", File.ReadAllText(cible));
        }

        [Fact]
        public void UnFichierTropGrosEstRefuseAvantDEtreCharge()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            var source = Path.Combine(dossier, "enorme.key");
            File.WriteAllText(cible, "ancienne");
            using (var flux = File.Create(source)) flux.SetLength(2 * 1024 * 1024);

            var resultat = LicenceInstaller.Installer(source, () => true, cible);

            Assert.Equal(IssueInstallation.FichierSource, resultat.Issue);
            Assert.Equal("ancienne", File.ReadAllText(cible));
        }

        /// <summary>
        /// L'acheteur peut choisir le fichier DÉJÀ en place (boîte de dialogue
        /// ouverte dans le dossier de l'application). La sauvegarde ne doit pas
        /// l'écraser avant lecture : le fichier ressort intact.
        /// </summary>
        [Fact]
        public void ChoisirLaLicenceDejaEnPlaceNeLaPerdPas()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            File.WriteAllText(cible, "actuelle");

            var resultat = LicenceInstaller.Installer(cible, () => true, cible);

            Assert.True(resultat.Succes);
            Assert.Equal("actuelle", File.ReadAllText(cible));
            Assert.Equal("actuelle", File.ReadAllText(cible + LicenceInstaller.BackupSuffix));
        }

        /// <summary>
        /// L'activation en ligne ne passe pas par un fichier : la licence
        /// arrive dans la réponse HTTP. Le contenu doit suivre exactement le
        /// même chemin de sauvegarde/restauration que l'installation manuelle.
        /// </summary>
        [Fact]
        public void LeContenuRecuEnLigneEstEcritTelQuelEtLAncienneEstSauvegardee()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            File.WriteAllText(cible, "ancienne");

            var contenu = System.Text.Encoding.UTF8.GetBytes("{\"licence\":\"en-ligne\"}");
            var resultat = LicenceInstaller.InstallerContenu(contenu, () => true, cible);

            Assert.True(resultat.Succes);
            Assert.Equal(contenu, File.ReadAllBytes(cible));
            Assert.Equal("ancienne", File.ReadAllText(cible + LicenceInstaller.BackupSuffix));
        }

        [Fact]
        public void UnContenuVideEstRefuseSansToucherALaLicenceEnPlace()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            File.WriteAllText(cible, "ancienne");

            var resultat = LicenceInstaller.InstallerContenu(Array.Empty<byte>(), () => true, cible);

            Assert.Equal(IssueInstallation.FichierSource, resultat.Issue);
            Assert.Equal("ancienne", File.ReadAllText(cible));
        }

        [Fact]
        public void UnContenuTropGrosEstRefuseSansEtreEcrit()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            File.WriteAllText(cible, "ancienne");

            var resultat = LicenceInstaller.InstallerContenu(
                new byte[2 * 1024 * 1024], () => true, cible);

            Assert.Equal(IssueInstallation.FichierSource, resultat.Issue);
            Assert.Equal("ancienne", File.ReadAllText(cible));
        }

        /// <summary>
        /// Cas le plus important de l'activation en ligne : le serveur a signé
        /// une licence pour une autre machine, ou le composant refuse pour une
        /// raison quelconque. L'acheteur ne doit pas perdre la licence qui
        /// marchait avant le clic.
        /// </summary>
        [Fact]
        public void UnContenuRefuseRestaureLAncienneLicence()
        {
            var dossier = Dossier();
            var cible = Path.Combine(dossier, "licence.key");
            File.WriteAllText(cible, "ancienne");

            var relectures = 0;
            var resultat = LicenceInstaller.InstallerContenu(
                System.Text.Encoding.UTF8.GetBytes("mauvaise"),
                () => { relectures++; return false; },
                cible);

            Assert.Equal(IssueInstallation.LicenceRefusee, resultat.Issue);
            Assert.Equal("ancienne", File.ReadAllText(cible));
            Assert.Equal(2, relectures);
        }

        public void Dispose()
        {
            foreach (var dossier in _dossiers)
            {
                try { Directory.Delete(dossier, recursive: true); }
                catch { /* un fichier verrouillé par l'antivirus ne doit pas faire échouer un test */ }
            }
        }
    }
}
