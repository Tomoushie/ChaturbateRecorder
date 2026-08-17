using System;
using System.IO;
using ChaturbateRecorderApp.Config;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Où vivent les données de l'utilisateur, et comment elles remontent.
    ///
    /// Les quatre fichiers — salons, favoris, surveillance, réglages — vivaient
    /// à côté de l'exécutable. Deux conséquences que rien n'aurait montrées
    /// avant un vrai déploiement : installée dans `Program Files`,
    /// l'application n'aurait pas pu enregistrer un seul salon ; et une mise à
    /// jour qui remplace son dossier effaçait la liste de l'utilisateur.
    ///
    /// C'est une capture d'écran qui l'a révélé — l'aperçu WPF montrait une
    /// liste VIDE alors que la version WinForms en avait onze, chacune lisant
    /// le `rooms.json` de SON dossier.
    ///
    /// Ce fichier éprouve la règle de reprise, parce que c'est le seul endroit
    /// du changement où des données peuvent être perdues.
    /// </summary>
    [Collection("DataLocation")]
    public class DataLocationTests : IDisposable
    {
        private readonly string _dossierNeuf;
        private readonly string _dataDirInitial;

        public DataLocationTests()
        {
            _dataDirInitial = AppConfig.DataDir;
            _dossierNeuf = Path.Combine(Path.GetTempPath(), "cbr-data-" + Guid.NewGuid().ToString("N"));
            AppConfig.DataDir = _dossierNeuf;
        }

        public void Dispose()
        {
            AppConfig.DataDir = _dataDirInitial;
            try { if (Directory.Exists(_dossierNeuf)) Directory.Delete(_dossierNeuf, recursive: true); }
            catch { /* Dossier temporaire : son sort n'engage rien. */ }
        }

        /// <summary>
        /// L'écriture vise TOUJOURS le dossier de données, jamais celui de
        /// l'exécutable — c'est tout l'objet du changement.
        /// </summary>
        [Fact]
        public void LEcritureViseToujoursLeDossierDeDonnees()
        {
            var chemin = AppConfig.DataFile("rooms.json");
            Assert.Equal(Path.Combine(_dossierNeuf, "rooms.json"), chemin);
        }

        /// <summary>
        /// Et elle crée le dossier au passage. Sans cela le tout premier
        /// enregistrement échouerait — sur une machine neuve uniquement, donc
        /// jamais sur celle du développeur.
        /// </summary>
        [Fact]
        public void LEcritureCreeLeDossierSiBesoin()
        {
            Assert.False(Directory.Exists(_dossierNeuf));
            AppConfig.DataFile("rooms.json");
            Assert.True(Directory.Exists(_dossierNeuf));
        }

        /// <summary>
        /// Le fichier neuf existe : c'est lui qu'on lit, même si un ancien
        /// traîne encore à côté de l'exécutable. Sans cette priorité, une
        /// modification enregistrée serait masquée au démarrage suivant par la
        /// version périmée — l'utilisateur verrait ses changements disparaître.
        /// </summary>
        [Fact]
        public void LeFichierNeufGagneSurLAncien()
        {
            var nom = "test-priorite-" + Guid.NewGuid().ToString("N") + ".json";
            var ancien = Path.Combine(AppConfig.AppDir, nom);
            var neuf = AppConfig.DataFile(nom);
            try
            {
                File.WriteAllText(ancien, "[]");
                File.WriteAllText(neuf, "[]");

                Assert.Equal(neuf, AppConfig.DataFileToRead(nom));
            }
            finally
            {
                try { File.Delete(ancien); } catch { }
            }
        }

        /// <summary>
        /// LE test de la reprise : rien dans le dossier de données, un fichier à
        /// côté de l'exécutable — on lit l'ancien. C'est ce qui fait remonter la
        /// liste de salons d'une installation antérieure au lieu d'accueillir
        /// l'utilisateur avec un écran vide.
        /// </summary>
        [Fact]
        public void SansFichierNeufOnLitCeluiDeLAncienneEmplacement()
        {
            var nom = "test-reprise-" + Guid.NewGuid().ToString("N") + ".json";
            var ancien = Path.Combine(AppConfig.AppDir, nom);
            try
            {
                File.WriteAllText(ancien, "[]");
                Assert.Equal(ancien, AppConfig.DataFileToRead(nom));
            }
            finally
            {
                try { File.Delete(ancien); } catch { }
            }
        }

        /// <summary>
        /// Aucun des deux : on rend le chemin NEUF. Un appelant qui teste
        /// ensuite `File.Exists` conclut « pas de fichier » — et le premier
        /// enregistrement ira au bon endroit.
        /// </summary>
        [Fact]
        public void SansAucunFichierOnRendLeCheminNeuf()
        {
            var nom = "test-absent-" + Guid.NewGuid().ToString("N") + ".json";
            Assert.Equal(Path.Combine(_dossierNeuf, nom), AppConfig.DataFileToRead(nom));
        }

        /// <summary>
        /// La lecture ne DOIT PAS créer le dossier : elle est appelée au
        /// démarrage, avant que l'utilisateur ait rien enregistré, et semer un
        /// dossier vide chez quelqu'un qui ne fait que lancer l'application
        /// serait impoli.
        /// </summary>
        [Fact]
        public void LaLectureNeCreeRien()
        {
            AppConfig.DataFileToRead("rooms.json");
            Assert.False(Directory.Exists(_dossierNeuf));
        }

        /// <summary>
        /// Les journaux et les données partagent le même dossier parent : c'est
        /// la relation qu'on veut, et la laisser implicite la ferait diverger au
        /// premier déplacement de l'un des deux.
        /// </summary>
        [Fact]
        public void LesJournauxSontSousLeDossierDeDonnees()
        {
            Assert.Equal(
                Path.Combine(AppConfig.DefaultDataDir(), "logs"),
                AppConfig.DefaultLogDir());
        }
    }

    /// <summary>
    /// `AppConfig.DataDir` est un état statique que ces tests déplacent : la
    /// collection les sérialise pour qu'aucune autre classe ne le voie bouger.
    /// </summary>
    [CollectionDefinition("DataLocation")]
    public class CollectionEmplacementDonnees { }
}
