using System.IO;
using ChaturbateRecorderApp.Security;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    public class PathValidatorTests
    {
        [Fact]
        public void IsValidPath_RejectsEmptyPath()
        {
            Assert.False(PathValidator.IsValidPath(""));
        }

        [Theory]
        [InlineData(@"\\server\share\file.txt")]      // UNC
        [InlineData(@"\\?\C:\Windows\file.txt")]        // chemin étendu \\?\
        [InlineData(@"\\.\C:\Windows\file.txt")]        // chemin étendu \\.\
        public void IsValidPath_RejectsNetworkAndExtendedPaths(string path)
        {
            Assert.False(PathValidator.IsValidPath(path));
        }

        [Fact]
        public void IsValidPath_RejectsDeviceNamespace()
        {
            Assert.False(PathValidator.IsValidPath(@"C:\Some\Device\file.txt"));
        }

        [Fact]
        public void IsValidPath_RejectsAlternateDataStream()
        {
            Assert.False(PathValidator.IsValidPath(@"C:\Users\test\file.txt:hidden"));
        }

        [Theory]
        [InlineData("relative\\path\\file.txt")]
        [InlineData("file.txt")]
        public void IsValidPath_RejectsNonAbsolutePaths(string path)
        {
            Assert.False(PathValidator.IsValidPath(path));
        }

        [Theory]
        [InlineData(@"C:\Users\test\CON")]
        [InlineData(@"C:\Users\test\CON.txt")]
        [InlineData(@"C:\Users\test\COM1")]
        [InlineData(@"C:\Users\test\LPT1\file.txt")]
        public void IsValidPath_RejectsReservedWindowsNames(string path)
        {
            Assert.False(PathValidator.IsValidPath(path));
        }

        [Fact]
        public void IsValidPath_AcceptsOrdinaryAbsoluteLocalPath()
        {
            var tempDir = Path.GetTempPath().TrimEnd('\\');
            Assert.True(PathValidator.IsValidPath(Path.Combine(tempDir, "some-ordinary-file.txt")));
        }

        [Fact]
        public void IsValidPath_MustExist_RejectsMissingPath()
        {
            var missing = Path.Combine(Path.GetTempPath(), "definitely-does-not-exist-" + System.Guid.NewGuid().ToString("N") + ".txt");
            Assert.False(PathValidator.IsValidPath(missing, mustExist: true));
        }

        [Fact]
        public void IsValidPath_MustExist_AcceptsExistingFile()
        {
            var path = Path.Combine(Path.GetTempPath(), "pv-test-" + System.Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(path, "test");
            try
            {
                Assert.True(PathValidator.IsValidPath(path, mustExist: true));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
