using System;
using System.IO;
using ChaturbateRecorder.Security;
using Xunit;

namespace ChaturbateRecorder.Security.Tests
{
    public class WorkingDirectoryValidatorTests
    {
        [Fact]
        public void IsAuthorizedLocation_RejectsEmptyPath()
        {
            Assert.False(WorkingDirectoryValidator.IsAuthorizedLocation(""));
        }

        [Fact]
        public void IsAuthorizedLocation_RejectsUncShare()
        {
            Assert.False(WorkingDirectoryValidator.IsAuthorizedLocation(@"\\server\share\app"));
        }

        [Fact]
        public void IsAuthorizedLocation_RejectsTempDirectory()
        {
            var tempSubDir = Path.Combine(Path.GetTempPath(), "someapp");
            Assert.False(WorkingDirectoryValidator.IsAuthorizedLocation(tempSubDir));
        }

        [Fact]
        public void IsAuthorizedLocation_AcceptsOrdinaryLocalFolder()
        {
            var ordinary = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SomeApp");
            Assert.True(WorkingDirectoryValidator.IsAuthorizedLocation(ordinary));
        }

        [Fact]
        public void IsAuthorizedLocation_OutReason_IsPopulatedOnRejection()
        {
            Assert.False(WorkingDirectoryValidator.IsAuthorizedLocation("", out var reason));
            Assert.False(string.IsNullOrEmpty(reason));
        }
    }
}
