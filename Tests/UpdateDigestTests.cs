using System;
using System.IO;
using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Verification d'integrite de la mise a jour (v1.28.0).
    ///
    /// Contexte : jusqu'ici l'application telechargeait et executait son propre
    /// remplacement SANS aucun controle, alors qu'elle verifie obsessionnellement
    /// le hash de yt-dlp et de ffmpeg. GitHub publie desormais l'empreinte de
    /// chaque fichier de release (champ « digest »), constatee reellement sur
    /// l'API le 2026-08-08 : « sha256:042bb7ff... ».
    /// </summary>
    public class UpdateDigestTests
    {
        /// <summary>Forme exacte relevee sur l'API GitHub.</summary>
        [Fact]
        public void TheRealApiFormIsParsedAndUppercased()
        {
            Assert.Equal(
                "042BB7FF39328852C45FE9C121DBCA13658329E8197AAC0C5C1B20AFD8B8E8E8",
                UpdateChecker.ParseDigest("sha256:042bb7ff39328852c45fe9c121dbca13658329e8197aac0c5c1b20afd8b8e8e8"));
        }

        /// <summary>
        /// Le garde-fou qui compte : une empreinte absente doit rendre une
        /// chaine vide, PAS lever. Les releases publiees avant l'introduction du
        /// champ n'en ont pas, et les rendre impossibles a installer serait pire
        /// que de les installer sans verification.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AMissingDigestYieldsAnEmptyStringRatherThanThrowing(string? digest)
        {
            Assert.Equal("", UpdateChecker.ParseDigest(digest));
        }

        /// <summary>
        /// Un algorithme inconnu est traite comme une absence : on ne compare
        /// jamais un SHA-256 calcule a une empreinte d'un autre algorithme, ce
        /// qui echouerait toujours et bloquerait toute mise a jour.
        /// </summary>
        [Theory]
        [InlineData("md5:d41d8cd98f00b204e9800998ecf8427e")]
        [InlineData("sha512:abcdef")]
        [InlineData("042bb7ff39328852")]
        public void AnUnknownAlgorithmIsTreatedAsAbsent(string digest)
        {
            Assert.Equal("", UpdateChecker.ParseDigest(digest));
        }

        /// <summary>
        /// L'empreinte calculee doit etre en majuscules sans separateur, pour
        /// se comparer directement a ParseDigest sans normalisation supplementaire.
        /// </summary>
        [Fact]
        public void ComputedHashMatchesTheParsedFormat()
        {
            var path = Path.Combine(Path.GetTempPath(), "cr-hash-" + Guid.NewGuid().ToString("N") + ".bin");
            try
            {
                File.WriteAllText(path, "chaturbate-recorder");
                var hash = UpdateInstaller.ComputeSha256(path);

                Assert.Equal(64, hash.Length);
                Assert.Equal(hash.ToUpperInvariant(), hash);
                Assert.DoesNotContain("-", hash);
            }
            finally
            {
                try { File.Delete(path); } catch { /* nettoyage au mieux */ }
            }
        }
    }
}
