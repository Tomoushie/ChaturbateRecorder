# Chaturbate Recorder — projet C# WinForms

Portage complet du script PowerShell en projet .NET 10 / WinForms natif.

- 🌐 [Site du projet](https://tomoushie.github.io/ChaturbateRecorder/)
- 📦 [Dernière release](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest) (portable ou dépendante du runtime)
- 📜 [Version PowerShell d'origine](legacy-powershell/) (avant migration .NET, non maintenue)

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows)
- `yt-dlp.exe` et `ffmpeg.exe` placés dans `Tools\` à la racine du projet (voir
  `ChaturbateRecorderApp.csproj` — copiés à côté de l'exécutable à chaque
  build ; ajuste `AppConfig.YtDlpPath` / `AppConfig.FFmpegPath` si tu préfères
  un autre emplacement)
- `donate_qr.png` dans `Assets\` (déjà inclus) — sera copié à côté de l'exe au build
- (optionnel) `ffprobe.exe` à côté de l'exécutable, pour afficher la durée des
  vidéos dans l'historique des enregistrements

## Build

```powershell
cd ChaturbateRecorderApp
dotnet build -c Release
```

L'exécutable se trouve ensuite dans `bin\Release\net10.0-windows\ChaturbateRecorder.exe`.

> Si `dotnet build`/`publish` échoue avec `NU1100` (impossible de résoudre un
> package), vérifie que `NuGet.Config` à la racine du projet est bien présent —
> le `NuGet.Config` global de certaines machines n'a aucune source configurée,
> ce qui bloque toute dépendance externe.

## Tests

Suite xUnit (`Tests/ChaturbateRecorderApp.Tests.csproj`) couvrant la sandbox
URL, la sandbox de chemins, la vérification de hash binaire, le parsing de
progression yt-dlp, et la correspondance SAN (Subject Alternative Name) TLS :

```powershell
dotnet test Tests/ChaturbateRecorderApp.Tests.csproj
```

## Publier un .exe autonome (single-file, sans dépendre du runtime .NET installé)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Résultat dans `bin\Release\net10.0-windows\win-x64\publish\ChaturbateRecorder.exe`
(~115 Mo, runtime .NET inclus — n'a besoin de rien d'installé sur la machine
cible). Copie aussi `donate_qr.png` (et tes propres `yt-dlp.exe`/`ffmpeg.exe`
si tu veux les distribuer avec) depuis ce même dossier `publish\`.

## Avant le premier lancement

Comme pour la version PowerShell, `AppConfig.YtDlpExpectedSha256` et
`AppConfig.FfmpegExpectedSha256` sont vides — la vérification d'intégrité
binaire refusera donc de démarrer tant qu'ils ne sont pas renseignés :

```powershell
Get-FileHash Tools\yt-dlp.exe -Algorithm SHA256
Get-FileHash Tools\ffmpeg.exe -Algorithm SHA256
```

Colle les valeurs obtenues dans `Config/AppConfig.cs`.

## Signer l'EXE (Authenticode)

Signer un exécutable nécessite un **certificat de signature de code que tu
possèdes** — je ne peux ni t'en fournir un, ni signer quoi que ce soit à ta
place (aucune clé privée, aucun accès à ta machine).

### Option 1 — certificat commercial (recommandé pour diffusion publique)

Achète un certificat de signature de code auprès d'une autorité reconnue
(DigiCert, Sectigo, SSL.com...), puis :

```powershell
signtool sign /fd SHA256 /a /f "moncert.pfx" /p "motdepasse" `
    /t http://timestamp.digicert.com `
    bin\Release\net10.0-windows\ChaturbateRecorder.exe
```

`signtool.exe` fait partie du Windows SDK (souvent déjà présent si tu as
Visual Studio installé, sinon installable séparément).

### Option 2 — certificat auto-signé (usage personnel uniquement)

Utile seulement sur **ta propre machine** (Windows ne fera pas confiance à ce
certificat ailleurs, SmartScreen continuera d'avertir sur d'autres postes) :

```powershell
$cert = New-SelfSignedCertificate -Type CodeSigning -Subject "CN=TonNom" `
    -CertStoreLocation Cert:\CurrentUser\My

signtool sign /fd SHA256 /sha1 $cert.Thumbprint `
    bin\Release\net10.0-windows\ChaturbateRecorder.exe
```

C'est ce même thumbprint que tu peux ensuite renseigner dans
`AppConfig.TrustedCaThumbprint` / `AppConfig.YtDlpExpectedSignerThumbprint`
etc. si tu veux réutiliser ce certificat pour re-signer yt-dlp/ffmpeg et
activer `EnableCaPinning`.

## Structure du projet

```
ChaturbateRecorderApp/
├── ChaturbateRecorderApp.csproj
├── NuGet.Config                    (source nuget.org explicite, voir plus haut)
├── Program.cs                      (point d'entrée)
├── MainForm.cs                     (UI + câblage des événements)
├── Properties/
│   └── AssemblyInfo.cs             (InternalsVisibleTo pour les tests)
├── Config/
│   ├── AppConfig.cs                 (config centralisée, équiv. $Config PS1)
│   └── Changelog.cs                 (historique de versions, affiché en local)
├── Security/
│   ├── BinaryVerifier.cs            (hash, Authenticode, pinning CA)
│   ├── UrlValidator.cs              (sandbox URL)
│   ├── PathValidator.cs             (sandbox dossier)
│   ├── WorkingDirectoryValidator.cs (dossier d'exécution : réseau/temp/compressé)
│   ├── AclValidator.cs              (détection d'ACL permissives)
│   └── CertificateValidator.cs      (TLS + SAN serveur distant)
├── Services/
│   ├── Logger.cs                    (logs JSON structurés)
│   ├── LogRotationManager.cs        (purge + rotation des logs)
│   ├── FavoritesManager.cs
│   ├── SettingsManager.cs           (paramètres persistés, settings.json)
│   ├── RecordingJob.cs              (métadonnées d'un enregistrement)
│   ├── DownloadEngine.cs            (process yt-dlp, watchdog anti-freeze)
│   ├── UpdateChecker.cs             (vérification des releases GitHub)
│   └── UpdateInstaller.cs           (téléchargement + remplacement de l'exe)
├── UI/
│   ├── ThemeManager.cs
│   ├── IconManager.cs               (icônes vectorielles, rendu SVG->Bitmap)
│   ├── ProgressBarColorExtensions.cs (couleur dynamique de la ProgressBar)
│   └── TutorialForm.cs              (guide de démarrage)
├── Assets/
│   ├── donate_qr.png
│   └── app.ico
├── Tools/                           (yt-dlp.exe / ffmpeg.exe — non versionnés, voir Prérequis)
├── Tests/
│   └── ChaturbateRecorderApp.Tests.csproj  (suite xUnit, voir section Tests)
├── docs/
│   └── index.html                   (site du projet, GitHub Pages)
└── legacy-powershell/                (version d'origine avant migration, voir son propre README)
```
