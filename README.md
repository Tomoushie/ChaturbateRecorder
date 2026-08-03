<div align="center">
  <img src="Assets/logo.png" width="88" height="88" alt="Logo Chaturbate Recorder">

  # Chaturbate Recorder

  Enregistreur de lives multi-stream pour Windows — sécurité, qualité et confidentialité configurables.

  [![Dernière version](https://img.shields.io/github/v/release/Tomoushie/ChaturbateRecorder?label=version&color=0078D4)](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest)
  [![Téléchargements](https://img.shields.io/github/downloads/Tomoushie/ChaturbateRecorder/total?color=0078D4)](https://github.com/Tomoushie/ChaturbateRecorder/releases)
  [![Build + Test](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/build-test.yml/badge.svg)](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/build-test.yml)
  [![Security Scan](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/security-scan.yml/badge.svg)](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/security-scan.yml)
  [![Licence](https://img.shields.io/badge/licence-MIT%20OR%20Apache--2.0-blue)](#licence)
  ![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
  ![Windows](https://img.shields.io/badge/Windows-0078D4?logo=windows11&logoColor=white)
  ![Langage principal](https://img.shields.io/github/languages/top/Tomoushie/ChaturbateRecorder?color=0078D4)

  🌐 [Site du projet](https://tomoushie.github.io/ChaturbateRecorder/) ·
  📦 [Dernière release](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest) ·
  📖 [Wiki](https://github.com/Tomoushie/ChaturbateRecorder/wiki) ·
  📜 [Version PowerShell d'origine](legacy-powershell/)

  🇫🇷 **Français** · 🇬🇧 [English](README.en.md)
</div>

<br>

![Capture d'écran de Chaturbate Recorder](Assets/screenshot.png)

## Fonctionnalités

- 🎬 **Multi-stream** — enregistre plusieurs lives en parallèle sans ouvrir plusieurs instances de l'application.
- 🎚️ **Qualité & codec au choix** — meilleure qualité, moyenne ou minimale ; réencodage optionnel en H.264/H.265 sans jamais toucher au fichier original.
- 📼 **Format de sortie** — MP4, MKV (plus robuste face à un arrêt brutal) ou MOV.
- 🔒 **Sécurité intégrée** — hash et signature de yt-dlp/ffmpeg, sandbox de chemins, détection d'ACL permissives, blocage des emplacements d'exécution suspects.
- 🕵️ **Confidentialité** — proxy SOCKS5/HTTP et import de cookies navigateur pour le contenu réservé aux comptes connectés.
- 🔄 **Mises à jour intégrées** — vérifie les releases GitHub et installe la nouvelle version automatiquement.
- 🌍 **Interface bilingue** — Français / English, mémorisé entre les lancements.
- 🗂️ **Fenêtre Paramètres dédiée** et réduction dans la zone de notification (les enregistrements continuent en arrière-plan).
- 🐞 **Signaler un bug** en un clic, directement vers un ticket GitHub pré-rempli.

Historique complet des versions dans le dialogue "Nouveautés" de l'application, ou [`Config/Changelog.cs`](Config/Changelog.cs).

## Installation (utilisateurs)

1. Télécharge le ZIP de la [dernière release](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest) :
   - **Standard** (~550 Ko) — nécessite le [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
   - **Portable** (~46 Mo) — autonome, aucune installation requise.
2. Extrais-le où tu veux.
3. Place `yt-dlp.exe` et `ffmpeg.exe` dans un dossier `Tools\` à côté de l'exécutable (non inclus, voir [Prérequis](#prérequis-développeurs)).
4. Lance `ChaturbateRecorder.exe`.

Guide détaillé (première configuration, sécurité, dépannage) : voir le **[Wiki](https://github.com/Tomoushie/ChaturbateRecorder/wiki)**.

## Pour les développeurs

### Prérequis (développeurs)

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows)
- `yt-dlp.exe` et `ffmpeg.exe` placés dans `Tools\` à la racine du projet (voir
  `ChaturbateRecorderApp.csproj` — copiés à côté de l'exécutable à chaque
  build ; ajuste `AppConfig.YtDlpPath` / `AppConfig.FFmpegPath` si tu préfères
  un autre emplacement)
- `donate_qr.png` dans `Assets\` (déjà inclus) — sera copié à côté de l'exe au build
- (optionnel) `ffprobe.exe` à côté de l'exécutable, pour afficher la durée des
  vidéos dans l'historique des enregistrements

### Build

```powershell
cd ChaturbateRecorderApp
dotnet build -c Release
```

L'exécutable se trouve ensuite dans `bin\Release\net10.0-windows\ChaturbateRecorder.exe`.

> Si `dotnet build`/`publish` échoue avec `NU1100` (impossible de résoudre un
> package), vérifie que `NuGet.Config` à la racine du projet est bien présent —
> le `NuGet.Config` global de certaines machines n'a aucune source configurée,
> ce qui bloque toute dépendance externe.

### Tests

Suite xUnit (`Tests/ChaturbateRecorderApp.Tests.csproj`) couvrant la sandbox
URL, la sandbox de chemins, la vérification de hash binaire, le parsing de
progression yt-dlp, et la correspondance SAN (Subject Alternative Name) TLS :

```powershell
dotnet test Tests/ChaturbateRecorderApp.Tests.csproj
```

### Publier un .exe autonome (single-file, sans dépendre du runtime .NET installé)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Résultat dans `bin\Release\net10.0-windows\win-x64\publish\ChaturbateRecorder.exe`
(~115 Mo, runtime .NET inclus — n'a besoin de rien d'installé sur la machine
cible). Copie aussi `donate_qr.png` (et tes propres `yt-dlp.exe`/`ffmpeg.exe`
si tu veux les distribuer avec) depuis ce même dossier `publish\`.

<details>
<summary><strong>Avant le premier lancement (hash yt-dlp/ffmpeg)</strong></summary>

Comme pour la version PowerShell, `AppConfig.YtDlpExpectedSha256` et
`AppConfig.FfmpegExpectedSha256` sont vides — la vérification d'intégrité
binaire refusera donc de démarrer tant qu'ils ne sont pas renseignés :

```powershell
Get-FileHash Tools\yt-dlp.exe -Algorithm SHA256
Get-FileHash Tools\ffmpeg.exe -Algorithm SHA256
```

Colle les valeurs obtenues dans `Config/AppConfig.cs`.

</details>

<details>
<summary><strong>Signer l'EXE (Authenticode)</strong></summary>

Signer un exécutable nécessite un **certificat de signature de code que tu
possèdes** — je ne peux ni t'en fournir un, ni signer quoi que ce soit à ta
place (aucune clé privée, aucun accès à ta machine).

#### Option 1 — certificat commercial (recommandé pour diffusion publique)

Achète un certificat de signature de code auprès d'une autorité reconnue
(DigiCert, Sectigo, SSL.com...), puis :

```powershell
signtool sign /fd SHA256 /a /f "moncert.pfx" /p "motdepasse" `
    /t http://timestamp.digicert.com `
    bin\Release\net10.0-windows\ChaturbateRecorder.exe
```

`signtool.exe` fait partie du Windows SDK (souvent déjà présent si tu as
Visual Studio installé, sinon installable séparément).

#### Option 2 — certificat auto-signé (usage personnel uniquement)

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

</details>

<details>
<summary><strong>Structure du projet</strong></summary>

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
│   ├── ThemeManager.cs               (thème clair/sombre, animations boutons)
│   ├── IconManager.cs               (icônes vectorielles, rendu SVG->Bitmap)
│   ├── Localization.cs              (traductions FR/EN de l'UI principale)
│   ├── RoundedGroupPanel.cs          (panneaux à bordure arrondie)
│   ├── SettingsForm.cs               (fenêtre Paramètres séparée)
│   ├── ProgressBarColorExtensions.cs (couleur dynamique de la ProgressBar)
│   └── TutorialForm.cs              (guide de démarrage)
├── Assets/
│   ├── donate_qr.png
│   ├── logo.png / screenshot.png    (utilisés par ce README)
│   └── app.ico
├── Tools/                           (yt-dlp.exe / ffmpeg.exe — non versionnés, voir Prérequis)
├── Tests/
│   └── ChaturbateRecorderApp.Tests.csproj  (suite xUnit, voir section Tests)
├── docs/
│   └── index.html                   (site du projet, GitHub Pages, FR/EN)
└── legacy-powershell/                (version d'origine avant migration, voir son propre README)
```

</details>

## Contribuer

Contributions, retours et rapports de bugs bienvenus via les [issues du dépôt](https://github.com/Tomoushie/ChaturbateRecorder/issues) — ou directement depuis l'application via le bouton **Signaler un bug**.

## Soutenir le projet

[![Faire un don](https://img.shields.io/badge/PayPal-Faire%20un%20don-00457C?logo=paypal&logoColor=white)](https://paypal.me/tomoushie)

## Licence

Double licence, au choix : [MIT](LICENSE-MIT) ou [Apache License 2.0](LICENSE-APACHE).

Sauf mention contraire explicite de ta part, toute contribution soumise pour
inclusion dans ce projet est placée sous cette double licence, sans condition
supplémentaire.
