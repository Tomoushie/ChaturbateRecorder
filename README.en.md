<div align="center">
  <img src="Assets/logo.png" width="88" height="88" alt="Chaturbate Recorder logo">

  # Chaturbate Recorder

  Multi-stream live recorder for Windows — configurable security, quality, and privacy.

  [![Latest version](https://img.shields.io/github/v/release/Tomoushie/ChaturbateRecorder?label=version&color=0078D4)](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest)
  [![Downloads](https://img.shields.io/github/downloads/Tomoushie/ChaturbateRecorder/total?color=0078D4)](https://github.com/Tomoushie/ChaturbateRecorder/releases)
  [![Build + Test](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/build-test.yml/badge.svg)](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/build-test.yml)
  [![Security Scan](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/security-scan.yml/badge.svg)](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/security-scan.yml)
  ![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
  ![Windows](https://img.shields.io/badge/Windows-0078D4?logo=windows11&logoColor=white)
  ![Top language](https://img.shields.io/github/languages/top/Tomoushie/ChaturbateRecorder?color=0078D4)

  🌐 [Project website](https://tomoushie.github.io/ChaturbateRecorder/) ·
  📦 [Latest release](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest) ·
  📖 [Wiki](https://github.com/Tomoushie/ChaturbateRecorder/wiki) ·
  📜 [Original PowerShell version](legacy-powershell/)

  🇬🇧 **English** · 🇫🇷 [Français](README.md)
</div>

<br>

![Chaturbate Recorder screenshot](Assets/screenshot.png)

## Features

- 🎬 **Multi-stream** — records several lives in parallel without opening multiple instances of the app.
- 🎚️ **Choice of quality & codec** — best, medium, or minimum quality; optional H.264/H.265 re-encoding that never touches the original file.
- 📼 **Output format** — MP4, MKV (more resilient to an abrupt stop), or MOV.
- 🔒 **Built-in security** — hash and signature checks for yt-dlp/ffmpeg, path sandboxing, permissive-ACL detection, blocking of suspicious execution locations.
- 🕵️ **Privacy** — SOCKS5/HTTP proxy and browser cookie import for content restricted to logged-in accounts.
- 🔄 **Built-in updates** — checks GitHub releases and installs the new version automatically.
- 🌍 **Bilingual interface** — French / English, remembered between launches.
- 🗂️ **Dedicated Settings window** and minimizing to the notification area (recordings keep running in the background).
- 🐞 **Report a bug** in one click, straight to a pre-filled GitHub issue.

Full version history is in the app's "What's new" dialog, or in [`Config/Changelog.cs`](Config/Changelog.cs).

## Installation (users)

1. Download the ZIP of the [latest release](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest):
   - **Standard** (~550 KB) — requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
   - **Portable** (~46 MB) — self-contained, no installation required.
2. Extract it wherever you like.
3. Place `yt-dlp.exe` and `ffmpeg.exe` in a `Tools\` folder next to the executable (not included, see [Prerequisites](#prerequisites-developers)).
4. Launch `ChaturbateRecorder.exe`.

Detailed guide (first-time setup, security, troubleshooting): see the **[Wiki](https://github.com/Tomoushie/ChaturbateRecorder/wiki)**.

## For developers

### Prerequisites (developers)

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows)
- `yt-dlp.exe` and `ffmpeg.exe` placed in `Tools\` at the project root (see
  `ChaturbateRecorderApp.csproj` — copied next to the executable on every
  build; adjust `AppConfig.YtDlpPath` / `AppConfig.FFmpegPath` if you'd
  rather use a different location)
- `donate_qr.png` in `Assets\` (already included) — copied next to the exe on build
- (optional) `ffprobe.exe` next to the executable, to show video duration
  in the recording history

### Build

```powershell
cd ChaturbateRecorderApp
dotnet build -c Release
```

The executable ends up in `bin\Release\net10.0-windows\ChaturbateRecorder.exe`.

> If `dotnet build`/`publish` fails with `NU1100` (unable to resolve a
> package), check that `NuGet.Config` at the project root is present —
> some machines' global `NuGet.Config` has no configured source, which
> blocks any external dependency.

### Tests

xUnit suite (`Tests/ChaturbateRecorderApp.Tests.csproj`) covering URL
sandboxing, path sandboxing, binary hash verification, yt-dlp progress
parsing, and TLS SAN (Subject Alternative Name) matching:

```powershell
dotnet test Tests/ChaturbateRecorderApp.Tests.csproj
```

### Publishing a self-contained .exe (single-file, no installed .NET runtime required)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Result in `bin\Release\net10.0-windows\win-x64\publish\ChaturbateRecorder.exe`
(~115 MB, .NET runtime included — needs nothing installed on the target
machine). Also copies `donate_qr.png` (and your own `yt-dlp.exe`/`ffmpeg.exe`
if you want to distribute them alongside) from that same `publish\` folder.

<details>
<summary><strong>Before the first launch (yt-dlp/ffmpeg hashes)</strong></summary>

As with the PowerShell version, `AppConfig.YtDlpExpectedSha256` and
`AppConfig.FfmpegExpectedSha256` are empty — binary integrity verification
will therefore refuse to start until they're filled in:

```powershell
Get-FileHash Tools\yt-dlp.exe -Algorithm SHA256
Get-FileHash Tools\ffmpeg.exe -Algorithm SHA256
```

Paste the resulting values into `Config/AppConfig.cs`.

</details>

<details>
<summary><strong>Signing the EXE (Authenticode)</strong></summary>

Signing an executable requires a **code-signing certificate that you
own** — I can neither provide one nor sign anything on your behalf (no
private key, no access to your machine).

#### Option 1 — commercial certificate (recommended for public distribution)

Buy a code-signing certificate from a recognized authority (DigiCert,
Sectigo, SSL.com...), then:

```powershell
signtool sign /fd SHA256 /a /f "mycert.pfx" /p "mypassword" `
    /t http://timestamp.digicert.com `
    bin\Release\net10.0-windows\ChaturbateRecorder.exe
```

`signtool.exe` is part of the Windows SDK (often already present if you
have Visual Studio installed, otherwise installable separately).

#### Option 2 — self-signed certificate (personal use only)

Only useful on **your own machine** (Windows won't trust this
certificate elsewhere, SmartScreen will keep warning on other machines):

```powershell
$cert = New-SelfSignedCertificate -Type CodeSigning -Subject "CN=YourName" `
    -CertStoreLocation Cert:\CurrentUser\My

signtool sign /fd SHA256 /sha1 $cert.Thumbprint `
    bin\Release\net10.0-windows\ChaturbateRecorder.exe
```

You can later put that same thumbprint into
`AppConfig.TrustedCaThumbprint` / `AppConfig.YtDlpExpectedSignerThumbprint`
etc. if you want to reuse this certificate to re-sign yt-dlp/ffmpeg and
enable `EnableCaPinning`.

</details>

<details>
<summary><strong>Project structure</strong></summary>

```
ChaturbateRecorderApp/
├── ChaturbateRecorderApp.csproj
├── NuGet.Config                    (explicit nuget.org source, see above)
├── Program.cs                      (entry point)
├── MainForm.cs                     (UI + event wiring)
├── Properties/
│   └── AssemblyInfo.cs             (InternalsVisibleTo for tests)
├── Config/
│   ├── AppConfig.cs                 (centralized config, equiv. of $Config in PS1)
│   └── Changelog.cs                 (version history, shown locally)
├── Security/
│   ├── BinaryVerifier.cs            (hash, Authenticode, CA pinning)
│   ├── UrlValidator.cs              (URL sandbox)
│   ├── PathValidator.cs             (folder sandbox)
│   ├── WorkingDirectoryValidator.cs (execution directory: network/temp/compressed)
│   ├── AclValidator.cs              (permissive ACL detection)
│   └── CertificateValidator.cs      (TLS + remote server SAN)
├── Services/
│   ├── Logger.cs                    (structured JSON logs)
│   ├── LogRotationManager.cs        (log purge + rotation)
│   ├── FavoritesManager.cs
│   ├── SettingsManager.cs           (persisted settings, settings.json)
│   ├── RecordingJob.cs              (recording metadata)
│   ├── DownloadEngine.cs            (yt-dlp process, anti-freeze watchdog)
│   ├── UpdateChecker.cs             (GitHub release checking)
│   └── UpdateInstaller.cs           (download + exe replacement)
├── UI/
│   ├── ThemeManager.cs               (light/dark theme, button animations)
│   ├── IconManager.cs               (vector icons, SVG->Bitmap rendering)
│   ├── Localization.cs              (FR/EN translations of the main UI)
│   ├── RoundedGroupPanel.cs          (rounded-border panels)
│   ├── SettingsForm.cs               (separate Settings window)
│   ├── ProgressBarColorExtensions.cs (dynamic ProgressBar color)
│   └── TutorialForm.cs              (getting-started guide)
├── Assets/
│   ├── donate_qr.png
│   ├── logo.png / screenshot.png    (used by this README)
│   └── app.ico
├── Tools/                           (yt-dlp.exe / ffmpeg.exe — not versioned, see Prerequisites)
├── Tests/
│   └── ChaturbateRecorderApp.Tests.csproj  (xUnit suite, see Tests section)
├── docs/
│   └── index.html                   (project website, GitHub Pages, FR/EN)
└── legacy-powershell/                (original version before migration, see its own README)
```

</details>

## Contributing

Contributions, feedback, and bug reports are welcome via the [repo's issues](https://github.com/Tomoushie/ChaturbateRecorder/issues) — or directly from the app via the **Report a bug** button.

## Support the project

[![Donate](https://img.shields.io/badge/PayPal-Donate-00457C?logo=paypal&logoColor=white)](https://paypal.me/tomoushie)
