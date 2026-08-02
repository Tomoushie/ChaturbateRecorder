using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Télécharge le ZIP de la release et déclenche le remplacement des
    /// fichiers. Windows ne permet pas à un process de remplacer son propre
    /// .exe pendant qu'il tourne : on génère donc un petit script PowerShell
    /// détaché qui attend la fin du process courant, copie les nouveaux
    /// fichiers, relance l'appli, puis se nettoie lui-même.
    /// </summary>
    public static class UpdateInstaller
    {
        public static async Task DownloadAndInstallAsync(string downloadUrl, string appDir)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ChaturbateRecorder_Update_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, "update.zip");

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromMinutes(5);
                http.DefaultRequestHeaders.UserAgent.ParseAdd("ChaturbateRecorder-UpdateChecker");
                var bytes = await http.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(zipPath, bytes);
            }

            var extractDir = Path.Combine(tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractDir);

            var exeName = Path.GetFileName(Application.ExecutablePath);
            var scriptPath = Path.Combine(tempDir, "update.ps1");
            var script = $@"
Wait-Process -Id {Environment.ProcessId} -Timeout 15 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Copy-Item -Path '{extractDir}\*' -Destination '{appDir}' -Recurse -Force
Start-Process -FilePath '{Path.Combine(appDir, exeName)}'
Start-Sleep -Seconds 2
Remove-Item -Path '{tempDir}' -Recurse -Force -ErrorAction SilentlyContinue
";
            File.WriteAllText(scriptPath, script);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            Application.Exit();
        }
    }
}
