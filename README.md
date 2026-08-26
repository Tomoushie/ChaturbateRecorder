<div align="center">
  <img src="Assets/logo.png" width="88" height="88" alt="Logo Chaturbate Recorder">

  # Chaturbate Recorder

  Enregistreur de lives multi-stream pour Windows — sécurité, qualité et confidentialité configurables.

  [![Sponsoriser](https://img.shields.io/badge/Sponsor-%E2%9D%A4-EA4AAA?logo=githubsponsors&logoColor=white)](https://github.com/sponsors/Tomoushie)
  [![Dernière version](https://img.shields.io/github/v/release/Tomoushie/ChaturbateRecorder?label=version&color=0078D4)](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest)
  [![Téléchargements](https://img.shields.io/github/downloads/Tomoushie/ChaturbateRecorder/total?color=0078D4)](https://github.com/Tomoushie/ChaturbateRecorder/releases)
  [![Build + Test](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/build-test.yml/badge.svg)](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/build-test.yml)
  [![Security Scan](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/security-scan.yml/badge.svg)](https://github.com/Tomoushie/ChaturbateRecorder/actions/workflows/security-scan.yml)
  [![NuGet SentinelGuard](https://img.shields.io/nuget/v/SentinelGuard?label=SentinelGuard&color=004880&logo=nuget&logoColor=white)](https://www.nuget.org/packages/SentinelGuard)
  [![Licence](https://img.shields.io/badge/licence-MIT%20OR%20Apache--2.0-blue)](#licence)
  [![Stars](https://img.shields.io/github/stars/Tomoushie/ChaturbateRecorder?color=0078D4)](https://github.com/Tomoushie/ChaturbateRecorder/stargazers)
  [![Forks](https://img.shields.io/github/forks/Tomoushie/ChaturbateRecorder?color=0078D4)](https://github.com/Tomoushie/ChaturbateRecorder/forks)
  ![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
  ![WPF](https://img.shields.io/badge/UI-WPF-0078D4?logo=windows11&logoColor=white)

  📚 [Documentation](https://tomoushie.github.io/ChaturbateRecorder/documentation.html) ·
  🌐 [Site du projet](https://tomoushie.github.io/ChaturbateRecorder/) ·
  📦 [Dernière release](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest) ·
  📖 [Wiki](https://github.com/Tomoushie/ChaturbateRecorder/wiki) ·
  🛡️ [SentinelGuard sur NuGet](https://www.nuget.org/packages/SentinelGuard) ·
  ❤️ [Sponsoriser le projet](https://github.com/sponsors/Tomoushie) ·
  📜 [Version PowerShell d'origine](https://github.com/Tomoushie/ChaturbateRecorder/tree/winforms-legacy/legacy-powershell)
</div>

<br>

![Capture d'écran de Chaturbate Recorder](Assets/screenshot.png)

## Démarrage rapide — 2 minutes

1. Télécharge **[`setup.exe`](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest)** (2 Mo).
2. Lance-le. Il installe l'application, yt-dlp et ffmpeg, vérifie leurs empreintes, et ne demande aucun droit administrateur.
3. Colle l'adresse d'un live — Chaturbate, Twitch, YouTube ou TikTok — et clique sur **Démarrer**.

Rien d'autre à installer : ni .NET, ni yt-dlp, ni ffmpeg. Windows affichera
« Éditeur inconnu » faute d'un certificat de signature payant — *Informations
complémentaires* puis *Exécuter quand même*.

Les autres formats (portable, standard) et l'installation manuelle sont
détaillés dans [Installation](#installation-utilisateurs).

## Fonctionnalités

- 📺 **Quatre plateformes** — Chaturbate, **Twitch**, **YouTube** et **TikTok**. Colle l'adresse du live, celle que tu utiliserais dans ton navigateur.
- 🎬 **Multi-stream** — enregistre plusieurs lives en parallèle sans ouvrir plusieurs instances de l'application.
- 🎚️ **Qualité & codec au choix** — meilleure qualité, moyenne ou minimale ; réencodage optionnel en H.264/H.265 sans jamais toucher au fichier original.
- 📼 **Format de sortie** — MP4, MKV (plus robuste face à un arrêt brutal) ou MOV.
- 🔒 **Sécurité intégrée** — hash et signature de yt-dlp/ffmpeg, sandbox de chemins, détection d'ACL permissives, blocage des emplacements d'exécution suspects.
- 🕵️ **Confidentialité** — proxy SOCKS5/HTTP et import de cookies navigateur pour le contenu réservé aux comptes connectés.
- 🔄 **Mises à jour intégrées** — vérifie les releases GitHub et installe la nouvelle version automatiquement.
- 🌍 **Interface bilingue** — Français / English, mémorisé entre les lancements.
- 🗂️ **Fenêtre Paramètres dédiée** et réduction dans la zone de notification (les enregistrements continuent en arrière-plan).
- 🐞 **Signaler un bug** en un clic, directement vers un ticket GitHub pré-rempli.

Historique complet des versions dans le dialogue "Nouveautés" de l'application.

## Pourquoi ce projet existe

Chaturbate Recorder est né d'un [script PowerShell](https://github.com/Tomoushie/ChaturbateRecorder/tree/winforms-legacy/legacy-powershell)
personnel — réécrit en application .NET 10, d'abord en WinForms, puis portée
en **WPF** (2026) pour gagner en fiabilité, en ergonomie et en maintenabilité.

**Ce qui le distingue** : la plupart des enregistreurs de ce type sont
de simples wrappers autour de `yt-dlp`, sans se soucier de ce qu'ils
exécutent ni d'où. Ici, la sécurité n'est pas une option qu'on ajoute
après coup — c'est le point de départ du design.

**Pourquoi il est sécurisé** : aucun binaire externe (`yt-dlp`, `ffmpeg`)
n'est exécuté sans vérification préalable de son hash SHA256 et,
optionnellement, de sa signature Authenticode. Les chemins de fichiers
et les URLs passent par une sandbox stricte avant utilisation. Les ACL
des dossiers sensibles sont surveillées. Le détail complet est sur la
page [Sécurité du wiki](https://github.com/Tomoushie/ChaturbateRecorder/wiki/Securite)
et la [page Fonctionnalités du site](https://tomoushie.github.io/ChaturbateRecorder/features.html).

**Pourquoi il est fiable** : suite de tests xUnit (241 tests, build+tests
exécutés automatiquement sur chaque changement, voir les badges plus haut),
un watchdog anti-freeze surveille `yt-dlp`/`ffmpeg` en continu, et chaque
enregistrement est identifié par un nom de fichier déterministe (pas
d'heuristique fragile) pour que miniature/réencodage retrouvent toujours
le bon fichier.

**Ce qu'il apporte de nouveau** par rapport au script PowerShell
d'origine : enregistrement multi-stream, interface bilingue, mises à
jour automatiques, package NuGet réutilisable
([`SentinelGuard`](https://www.nuget.org/packages/SentinelGuard))
pour qui veut les mêmes validations dans un autre projet .NET, et un
pipeline CI/CD complet (build, tests, releases et déploiement du site
automatisés).

## Installation (utilisateurs)

### Avec l'installateur (recommandé)

Télécharge **`ChaturbateRecorder-vX.Y.Z-setup.exe`** depuis la
[dernière release](https://github.com/Tomoushie/ChaturbateRecorder/releases/latest)
et lance-le. Il fait 2 Mo et s'occupe de tout :

- il propose **installation classique ou version portable**, au choix, dès le premier écran ;
- il télécharge l'application, **yt-dlp et ffmpeg**, et les vérifie contre les sommes de contrôle publiées par leurs auteurs ;
- il n'exige **aucun runtime .NET** : la variante installée l'embarque ;
- il s'installe **pour ton compte utilisateur uniquement**, donc sans demande d'élévation.

Une connexion est nécessaire pendant l'installation (~150 Mo téléchargés).
L'installateur n'étant pas signé, Windows affichera « Éditeur inconnu » :
c'est attendu, faute d'un certificat de signature de code (voir la section
Sécurité plus bas).

### À la main

Les deux ZIP restent attachés à chaque release, pour une installation hors ligne
ou si tu préfères garder la main :

1. Télécharge **Standard** (nécessite le
   [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0))
   ou **Portable** (autonome, aucun runtime requis).
2. Extrais-le où tu veux.
3. Place `yt-dlp.exe` et `ffmpeg.exe` à côté de l'exécutable (non inclus, voir [Prérequis](#prérequis)).
4. Lance `ChaturbateRecorder.exe`.

Guide détaillé (première configuration, sécurité, dépannage) : voir le **[Wiki](https://github.com/Tomoushie/ChaturbateRecorder/wiki)**.

## 🛡️ SentinelGuard — les garde-fous, réutilisables dans ton projet

[![NuGet](https://img.shields.io/nuget/v/SentinelGuard?label=SentinelGuard&color=004880&logo=nuget&logoColor=white)](https://www.nuget.org/packages/SentinelGuard)
[![Téléchargements NuGet](https://img.shields.io/nuget/dt/SentinelGuard?color=004880)](https://www.nuget.org/packages/SentinelGuard)

Les vérifications de sécurité de cette application sont publiées séparément,
pour toute application .NET Windows qui lance des binaires tiers ou manipule
des chemins et des URL fournis par l'utilisateur :

```powershell
dotnet add package SentinelGuard
```

| Classe | Ce qu'elle empêche |
|---|---|
| `PathValidator` | Chemins UNC, chemins étendus, flux ADS, noms réservés Windows, symlinks |
| `UrlValidator` | Schémas non-HTTPS, domaines hors liste blanche, segments et query string douteux |
| `BinaryVerifier` | Exécutable altéré : hash SHA-256, signature Authenticode, pinning du certificat |
| `AclValidator` | Dossiers inscriptibles par `Tout le monde` — où un binaire peut être remplacé |
| `WorkingDirectoryValidator` | Exécution depuis un partage réseau, un dossier temporaire ou la corbeille |
| `CertificateValidator` | Interception TLS : pinning de certificat et validation du SAN |
| `GuardedProcessRunner` | Un binaire vérifié qui dérape une fois lancé : processus figé, processus enfants orphelins |
| `LogFileRotator` | Un fichier de log qui grossit sans limite, et des logs conservés indéfiniment |

Les validateurs sont des fonctions **pures** : chaque vérification retourne un
booléen, avec une surcharge `out string? reason` donnant le motif exact du refus.
Rien n'est journalisé, rien n'est levé dans ton dos — tu décides quoi en faire.

Cible `net8.0-windows` et `net10.0-windows`, sous double licence MIT ou
Apache-2.0. Détails et exemples : [`SentinelGuard/README.md`](SentinelGuard/README.md).

**L'application utilise ce package, elle n'en garde pas de copie.**

## 🔒 Sécurité — ce qui est en place, et ce qui ne l'est pas

Cette liste est volontairement vérifiable : chaque ligne correspond à du code
présent dans le dépôt.

**En place :**

| Protection | Contre quoi |
|---|---|
| Hash SHA-256 de `yt-dlp.exe` et `ffmpeg.exe` | Binaire tiers remplacé ou altéré |
| Pinning de certificat + validation du SAN | Interception TLS, proxy malveillant |
| Sandbox de chemins | UNC, chemins étendus, flux ADS, symlinks, noms réservés |
| Sandbox d'URL | Schémas non-HTTPS, domaines hors liste blanche |
| Validation des ACL | Dossiers inscriptibles par tous, où un binaire peut être remplacé |
| Validation du dossier d'exécution | Lancement depuis un partage réseau ou un dossier temporaire |
| Contrôle du fichier cookies | Fichier malformé qui ferait échouer les captures sans le dire |
| Watchdog anti-freeze | yt-dlp/ffmpeg bloqués sans fin |
| Noms de fichiers déterministes | Miniature ou réencodage associés au mauvais enregistrement |
| CodeQL + audit des dépendances en CI | Vulnérabilités connues et code à risque |

**Pas en place, et pourquoi :**

- **Signature Authenticode de l'exécutable** — bloquée sur l'obtention d'un
  certificat de signature de code, qui est payant. Rien ne le contourne côté
  logiciel.

L'application ne collecte rien, n'a pas de compte, et le seul appel réseau
qu'elle effectue d'elle-même est la recherche de mise à jour, désactivable dans
les Paramètres.

## Pour les développeurs

Documentation technique complète (architecture, décisions, pièges WPF
mesurés) dans [`CLAUDE.md`](CLAUDE.md), la référence de ce dépôt.

### Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows)
- `yt-dlp.exe` et `ffmpeg.exe` placés dans `Tools\` à la racine du projet —
  copiés à côté de l'exécutable à chaque build
- `NuGet.Config` local requis (déjà présent) : la config NuGet globale de
  certaines machines n'a aucune source configurée, ce qui bloque `restore`
  sans lui (`NU1100`)

### Build

```powershell
dotnet build ChaturbateRecorderApp.Wpf.csproj -c Release
```

### Tests

Suite xUnit (241 tests) couvrant la sandbox URL, la sandbox de chemins, la
vérification de hash binaire, le parsing de progression yt-dlp, la
correspondance SAN (TLS), et le rendu visuel réel des vues WPF :

```powershell
dotnet test Tests\ChaturbateRecorderApp.Wpf.Tests.csproj
```

### Publier un .exe autonome (single-file, sans dépendre du runtime .NET installé)

```powershell
dotnet publish ChaturbateRecorderApp.Wpf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

`--self-contained true` n'exige aucun runtime installé sur la machine cible ;
`false` exige le **.NET 10 Desktop Runtime**.

<details>
<summary><strong>Structure du projet</strong></summary>

```
ChaturbateRecorderApp WPF/
├── ChaturbateRecorderApp.Wpf.csproj
├── NuGet.Config                     (source nuget.org explicite, voir Prérequis)
├── App.xaml / App.xaml.cs           (point d'entrée)
├── Views/                           (fenêtres et vues XAML)
├── ViewModels/                      (logique de présentation, MVVM)
├── Converters/                      (convertisseurs de binding XAML)
├── Themes/                          (thème clair/sombre, styles de contrôles)
├── UI/                              (localisation, gestion de thème)
├── Config/                          (configuration centralisée, changelog)
├── Services/                        (moteur de téléchargement, historique,
│                                     mise à jour, pont vers le module premium)
├── SentinelGuard/                   (garde-fous de sécurité, aussi publiés
│                                     séparément sur NuGet — voir plus haut)
├── SentinelGuard.Tests/
├── Tests/                           (suite xUnit, 241 tests)
├── Tools/                           (yt-dlp.exe / ffmpeg.exe — non versionnés)
├── Assets/                          (logo, captures d'écran, QR de don)
├── installer/                       (script de l'installateur)
└── docs/                            (site du projet, GitHub Pages)
```

</details>

## Roadmap

Suivi détaillé (fait / prévu / écarté) sur la
[page Roadmap du site](https://tomoushie.github.io/ChaturbateRecorder/roadmap.html),
et backlog restant sur le
[tableau de suivi GitHub](https://github.com/users/Tomoushie/projects/2).

## Contribuer

Contributions, retours et rapports de bugs bienvenus via les [issues du dépôt](https://github.com/Tomoushie/ChaturbateRecorder/issues) — ou directement depuis l'application via le bouton **Signaler un bug**. Voir [CONTRIBUTING.md](CONTRIBUTING.md) pour le détail, ou les [Discussions](https://github.com/Tomoushie/ChaturbateRecorder/discussions) pour une question/idée. Vulnérabilité de sécurité : voir [SECURITY.md](SECURITY.md).

## Soutenir le projet

[![Sponsoriser sur GitHub](https://img.shields.io/badge/GitHub%20Sponsors-%E2%9D%A4-EA4AAA?logo=githubsponsors&logoColor=white)](https://github.com/sponsors/Tomoushie)
[![Faire un don](https://img.shields.io/badge/PayPal-Faire%20un%20don-00457C?logo=paypal&logoColor=white)](https://paypal.me/tomoushie)

**[GitHub Sponsors](https://github.com/sponsors/Tomoushie)** pour un soutien
récurrent, **PayPal** pour un don ponctuel. L'application reste entièrement
gratuite et sans publicité dans les deux cas.

### Pourquoi sponsoriser ?

Au-delà du temps passé, un point précis est aujourd'hui bloqué faute de budget :
la **signature Authenticode de l'exécutable**. Un certificat de signature de
code est payant et délivré à une entité vérifiée. Sans lui :

- Windows SmartScreen affiche un avertissement au premier lancement, que chaque
  utilisateur doit contourner manuellement
- l'application ne peut pas vérifier sa propre intégrité au démarrage, et donc
  pas détecter qu'elle a été modifiée ou « crackée »

C'est la seule fonctionnalité de la roadmap bloquée par de l'argent plutôt que
par du travail. Le reste avance sans.

## ⚖️ Légalité (Belgique)

Chaturbate Recorder enregistre uniquement des flux publiquement accessibles, tels que l'utilisateur peut déjà les visionner dans son navigateur. Le logiciel ne contourne aucune mesure technique de protection, n'accède à aucun système ou contenu privé et n'exploite aucune faille : les infractions d'accès non autorisé à un système informatique (art. 550bis du Code pénal) et d'atteinte aux données (art. 550ter du Code pénal) ne sont donc pas en cause.

L'enregistrement d'un flux public peut relever de l'exception de copie privée, prévue par l'article XI.190, §1er, 5° du Code de droit économique (anciennement art. 22, §1er, 5° de la loi du 30 juin 1994), tant que l'usage reste strictement personnel et non commercial. En revanche, diffuser, partager, transmettre ou rendre accessible un enregistrement à caractère sexuel d'une personne sans son consentement constitue une infraction pénale en Belgique (art. 417/5 du Code pénal), indépendamment de la manière dont l'enregistrement a été obtenu.

L'utilisateur est seul responsable de l'usage qu'il fait des enregistrements. Il lui appartient de vérifier les conditions d'utilisation de la plateforme — qui peuvent interdire l'enregistrement indépendamment de la loi —, le droit à l'image et la protection des données des personnes filmées, ainsi que le droit d'auteur applicable et la législation de son pays de résidence.

*Ce texte est informatif et ne constitue pas un avis juridique.*

## Licence

Double licence, au choix : [MIT](LICENSE-MIT) ou [Apache License 2.0](LICENSE-APACHE).

Sauf mention contraire explicite de ta part, toute contribution soumise pour
inclusion dans ce projet est placée sous cette double licence, sans condition
supplémentaire.
