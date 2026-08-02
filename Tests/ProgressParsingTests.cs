using ChaturbateRecorderApp.Services;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    public class ProgressParsingTests
    {
        [Theory]
        [InlineData("[download]  42.0% of ~ 150.00MiB at  2.50MiB/s ETA 00:35", 42.0)]
        [InlineData("[download] 100% of 200.00MiB", 100.0)]
        [InlineData("[download]   0.3% of ~1.00GiB", 0.3)]
        public void TryParseProgress_ExtractsPercentFromYtDlpLine(string line, double expected)
        {
            var ok = DownloadEngine.TryParseProgress(line, out var percent);
            Assert.True(ok);
            Assert.Equal(expected, percent, precision: 3);
        }

        [Theory]
        [InlineData("frame=  966 fps= 35 q=-1.0 size=  20736KiB time=00:00:32.00 bitrate=5308.4kbits/s")]
        [InlineData("[https @ 0000022666848cc0] Opening 'https://edge4-ams.live.mmcdn.com/...'")]
        [InlineData("")]
        [InlineData("ligne quelconque sans pourcentage")]
        public void TryParseProgress_ReturnsFalseForNonProgressLines(string line)
        {
            var ok = DownloadEngine.TryParseProgress(line, out var percent);
            Assert.False(ok);
            Assert.Equal(0, percent);
        }
    }
}
