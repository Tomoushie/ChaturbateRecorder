using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Télécharge le ZIP de la release et déclenche le remplacement des
    /// fichiers. Windows ne permet pas à un process de remplacer son propre
    /// .exe pendant qu'il tourne : on génère donc un petit script PowerShell
    /// détaché qui attend la fin du process courant, copie les nouveaux
    /// fichiers, relance l'appli, puis se nettoie lui-même.
    ///
    /// **Trois défauts corrigés en v1.28.0, trouvés en relisant ce code qui
    /// n'avait jamais été exercé depuis son écriture en 1.2.0** :
    /// - le ZIP était téléchargé et exécuté **sans aucun contrôle d'intégrité**,
    ///   dans une application qui vérifie pourtant obsessionnellement le hash de
    ///   yt-dlp et de ffmpeg. GitHub publie désormais l'empreinte de chaque
    ///   fichier de release (champ `digest`), donc plus aucune raison de s'en
    ///   passer ;
    /// - passé un délai de 15 s, la copie échouait sur un exe encore verrouillé
    ///   et le script relançait **l'ancienne version sans rien signaler** :
    ///   l'utilisateur croyait avoir mis à jour ;
    /// - après une mise à jour, « Applications installées » continuait
    ///   d'afficher l'ancienne version pour qui avait installé via
    ///   l'installateur (23.0).
    /// </summary>
    public static class UpdateInstaller
    {
        /// <summary>
        /// Identifiant Inno Setup de l'installateur (23.0). La cle de
        /// desinstallation vaut cet identifiant suivi de « _is1 ». Doit rester
        /// synchronise avec AppId dans installer/ChaturbateRecorder.iss.
        /// </summary>
        private const string InnoAppId = "{7C4E1F2A-9B63-4D18-A5E7-3F0C6D2B84A1}";

        /// <summary>
        /// Prend l'UpdateInfo entier plutot que des valeurs separees : une
        /// premiere version passait l'URL et l'empreinte a part, et ecrivait
        /// dans la fiche de desinstallation la version de l'assembly EN COURS
        /// — donc l'ancienne. Regrouper ce qui va ensemble supprime la
        /// possibilite meme de melanger les trois.
        /// </summary>
        public static async Task DownloadAndInstallAsync(UpdateInfo update, string appDir)
        {
            var downloadUrl = update.DownloadUrl;
            var expectedSha256 = update.Sha256;

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

            // Contrôle d'intégrité AVANT d'extraire quoi que ce soit. Une
            // empreinte absente (release antérieure au champ `digest` de l'API)
            // laisse passer : refuser rendrait les anciennes versions
            // impossibles à mettre à jour, ce qui serait pire.
            if (!string.IsNullOrWhiteSpace(expectedSha256))
            {
                var actual = ComputeSha256(zipPath);
                if (!string.Equals(actual, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(tempDir);
                    throw new InvalidOperationException(
                        Localization.Format("update.hashMismatch", expectedSha256.Trim(), actual));
                }
                Logger.Log($"Mise à jour : empreinte du ZIP vérifiée ({actual}).");
            }
            else
            {
                Logger.Log("Mise à jour : aucune empreinte publiée pour ce fichier, installation sans vérification.", LogLevel.WARN);
            }

            var extractDir = Path.Combine(tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractDir);

            var exeName = Path.GetFileName(Application.ExecutablePath);
            var scriptPath = Path.Combine(tempDir, "update.ps1");
            var statusLog = Path.Combine(AppConfig.LogDir, "update.log");

            // Mise à jour de la fiche de désinstallation, uniquement si l'appli
            // a été posée par l'installateur — la présence de son désinstalleur
            // est le marqueur le plus simple et le plus fiable.
            var installedBySetup = File.Exists(Path.Combine(appDir, "unins000.exe"));
            // La version a inscrire est celle QUI ARRIVE, pas celle qui tourne.
            // Mesure le 2026-08-08 : l'assembly en cours d'execution est
            // l'ancienne version, donc la fiche restait inchangee tout en
            // annoncant une mise a jour reussie.
            var version = update.Version;

            var script = $@"
$ErrorActionPreference = 'Stop'
$log = '{statusLog}'
function Note($m) {{ Add-Content -LiteralPath $log -Value ((Get-Date).ToString('s') + '  ' + $m) }}

try {{
    # 120 s et non 15 : arrêter des enregistrements en cours peut prendre du
    # temps, et copier sur un exe encore verrouillé échouait en silence.
    Wait-Process -Id {Environment.ProcessId} -Timeout 120 -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1

    # Copie element par element, et NON 'dossier\*'.
    #
    # Piege mesure le 2026-08-08 : « Copy-Item -LiteralPath 'dossier\*' » ne
    # copie RIEN et ne leve AUCUNE erreur — -LiteralPath desactive
    # l'interpretation du joker, qui devient un nom de fichier litteral
    # inexistant. Le script annoncait donc « mise a jour appliquee » sur une
    # copie vide : exactement l'echec silencieux qu'il etait cense supprimer.
    #
    # Le compteur est le garde-fou : une copie vide devient une erreur bruyante
    # au lieu d'un succes mensonger.
    $copied = 0
    Get-ChildItem -LiteralPath '{extractDir}' | ForEach-Object {{
        Copy-Item -LiteralPath $_.FullName -Destination '{appDir}' -Recurse -Force
        $copied++
    }}
    if ($copied -eq 0) {{ throw 'Archive vide : aucun fichier a installer.' }}
    Note ""Mise a jour appliquee ($copied element(s)).""

    if ('{installedBySetup}' -eq 'True') {{
        $key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{InnoAppId}_is1'
        if (Test-Path $key) {{
            Set-ItemProperty -Path $key -Name DisplayVersion -Value '{version}'
            Note 'Fiche de desinstallation mise a jour.'
        }}
    }}
}} catch {{
    # Ne JAMAIS echouer en silence : sans cette trace, l'utilisateur relancait
    # l'ancienne version en croyant avoir mis a jour.
    Note ('ECHEC de la mise a jour : ' + $_.Exception.Message)
}} finally {{
    Start-Process -FilePath '{Path.Combine(appDir, exeName)}'
    Start-Sleep -Seconds 2
    Remove-Item -LiteralPath '{tempDir}' -Recurse -Force -ErrorAction SilentlyContinue
}}
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

        internal static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }

        private static void TryDelete(string dir)
        {
            try { Directory.Delete(dir, recursive: true); }
            catch (Exception ex) { Logger.Log($"Nettoyage de '{dir}' impossible : {ex.Message}", LogLevel.WARN); }
        }
    }
}
