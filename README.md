# Chaturbate Recorder — projet C# WinForms

Portage complet du script PowerShell en projet .NET 8 / WinForms natif.

## ⚠️ Important — non compilé ni testé de mon côté

Je n'ai pas d'environnement Windows ni de SDK .NET disponible ici (sandbox Linux
sans accès réseau pour installer quoi que ce soit). Ce code a été écrit avec le
plus grand soin et une relecture manuelle attentive, mais **je n'ai pas pu lancer
`dotnet build` pour le vérifier**. Il faudra probablement corriger quelques
erreurs de compilation mineures (typos, imports manquants) une fois sur ta
machine — c'est normal pour un premier build d'un projet de cette taille écrit
sans compilateur sous la main.

## Prérequis

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (Windows)
- `yt-dlp.exe` et `ffmpeg.exe` placés dans un sous-dossier `bin\` à côté de
  l'exécutable final (ou ajuste `AppConfig.YtDlpPath` / `AppConfig.FFmpegPath`)
- `donate_qr.png` dans `Assets\` (déjà inclus) — sera copié à côté de l'exe au build

## Build

```powershell
cd ChaturbateRecorderApp
dotnet build -c Release
```

L'exécutable se trouve ensuite dans `bin\Release\net8.0-windows\ChaturbateRecorder.exe`.

## Publier un .exe autonome (single-file, sans dépendre du runtime .NET installé)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Résultat dans `bin\Release\net8.0-windows\win-x64\publish\ChaturbateRecorder.exe`.

## Avant le premier lancement

Comme pour la version PowerShell, `AppConfig.YtDlpExpectedSha256` et
`AppConfig.FfmpegExpectedSha256` sont vides — la vérification d'intégrité
binaire refusera donc de démarrer tant qu'ils ne sont pas renseignés :

```powershell
Get-FileHash bin\yt-dlp.exe -Algorithm SHA256
Get-FileHash bin\ffmpeg.exe -Algorithm SHA256
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
    bin\Release\net8.0-windows\ChaturbateRecorder.exe
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
    bin\Release\net8.0-windows\ChaturbateRecorder.exe
```

C'est ce même thumbprint que tu peux ensuite renseigner dans
`AppConfig.TrustedCaThumbprint` / `AppConfig.YtDlpExpectedSignerThumbprint`
etc. si tu veux réutiliser ce certificat pour re-signer yt-dlp/ffmpeg et
activer `EnableCaPinning`.

## Structure du projet

```
ChaturbateRecorderApp/
├── ChaturbateRecorderApp.csproj
├── Program.cs                     (point d'entrée)
├── MainForm.cs                    (UI + câblage des événements)
├── Config/
│   └── AppConfig.cs                (config centralisée, équiv. $Config PS1)
├── Security/
│   ├── BinaryVerifier.cs           (hash, Authenticode, pinning CA)
│   ├── UrlValidator.cs             (sandbox URL)
│   ├── PathValidator.cs            (sandbox dossier)
│   └── CertificateValidator.cs     (TLS + SAN serveur distant)
├── Services/
│   ├── Logger.cs
│   ├── FavoritesManager.cs
│   └── DownloadEngine.cs           (vrai Process .NET, remplace Start-Job)
├── UI/
│   └── ThemeManager.cs
└── Assets/
    └── donate_qr.png
```
