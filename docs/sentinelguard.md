---
layout: default
title: SentinelGuard
---

<link rel="stylesheet" href="assets/lang-toggle.css">
<button id="langToggle" class="lang-toggle-btn" type="button"></button>

<div class="lang-fr" markdown="1">

# 🛡️ SentinelGuard

[![NuGet](https://img.shields.io/nuget/v/SentinelGuard?label=SentinelGuard&color=004880&logo=nuget&logoColor=white)](https://www.nuget.org/packages/SentinelGuard)
[![Téléchargements](https://img.shields.io/nuget/dt/SentinelGuard?color=004880)](https://www.nuget.org/packages/SentinelGuard)
[![Licence](https://img.shields.io/badge/licence-MIT%20OR%20Apache--2.0-blue)](https://github.com/Tomoushie/ChaturbateRecorder)

Les vérifications de sécurité de Chaturbate Recorder sont publiées séparément
sur NuGet, utilisables dans n'importe quelle application .NET Windows.

```powershell
dotnet add package SentinelGuard
```

## À qui ça sert

Toute application de bureau qui **lance un binaire tiers** (yt-dlp, ffmpeg, un
outil interne…) ou qui **manipule des chemins et des URL fournis par
l'utilisateur** hérite des mêmes problèmes : un chemin peut sortir du dossier prévu,
une URL peut pointer ailleurs que prévu, et un exécutable posé à côté du vôtre
peut avoir été remplacé.

SentinelGuard regroupe les contrôles à faire **avant** de faire confiance.

## Ce qu'il vérifie

| Classe | Ce qu'elle empêche |
|---|---|
| `PathValidator` | Chemins UNC, chemins étendus (`\\?\`, `\\.\`), flux ADS, noms réservés Windows (`CON`, `NUL`…), symlinks et points de re-analyse |
| `UrlValidator` | Schémas autres que HTTPS, domaines hors liste blanche, hôtes bannis, segments de chemin et query string douteux |
| `BinaryVerifier` | Exécutable altéré : hash SHA-256, signature Authenticode absente ou invalide, certificat signataire inattendu |
| `AclValidator` | Dossiers inscriptibles par `Tout le monde` / `Utilisateurs authentifiés` — là où un binaire peut être remplacé juste avant son exécution |
| `WorkingDirectoryValidator` | Exécution depuis un partage réseau, un dossier temporaire, la corbeille ou un dossier compressé NTFS |
| `CertificateValidator` | Interception TLS : pinning explicite de certificat et validation du SAN |

## Comment ça s'utilise

Des fonctions **pures**, sans effet de bord. Chaque vérification retourne un
booléen, avec une surcharge optionnelle donnant le motif exact du refus :

```csharp
using SentinelGuard;

if (!PathValidator.IsValidPath(cheminUtilisateur, mustExist: true, out var motif))
{
    Console.WriteLine($"Chemin refusé : {motif}");
    return;
}

if (!BinaryVerifier.VerifyTrustedBinary(cheminOutil, hashAttendu, out var motifBin))
{
    Console.WriteLine($"Binaire refusé : {motifBin}");
    return;
}
```

Rien n'est journalisé, rien n'est levé dans votre dos : vous décidez quoi faire
du motif — l'écrire dans vos logs, l'afficher, ou l'ignorer.

## Portée

Cible `net8.0-windows` et `net10.0-windows`. Windows uniquement, en connaissance
de cause : les ACL NTFS, Authenticode et les magasins de certificats Windows
n'ont pas d'équivalent multiplateforme.

**Ce que ce n'est pas** : un bac à sable ni une frontière de sécurité.
SentinelGuard réduit la surface d'attaque des entrées non fiables dans une
application de bureau ; il ne confine pas un processus hostile. À considérer
comme une défense en profondeur, en complément des protections du système — pas
à leur place.

## Liens

- [Page NuGet](https://www.nuget.org/packages/SentinelGuard)
- [Code source](https://github.com/Tomoushie/ChaturbateRecorder/tree/main/SentinelGuard)
- [Signaler un problème](https://github.com/Tomoushie/ChaturbateRecorder/issues)

[← Accueil](index.html) · [Fonctionnalités](features.html) · [Roadmap](roadmap.html)

</div>

<div class="lang-en" markdown="1" style="display:none">

# 🛡️ SentinelGuard

[![NuGet](https://img.shields.io/nuget/v/SentinelGuard?label=SentinelGuard&color=004880&logo=nuget&logoColor=white)](https://www.nuget.org/packages/SentinelGuard)
[![Downloads](https://img.shields.io/nuget/dt/SentinelGuard?color=004880)](https://www.nuget.org/packages/SentinelGuard)
[![License](https://img.shields.io/badge/license-MIT%20OR%20Apache--2.0-blue)](https://github.com/Tomoushie/ChaturbateRecorder)

The security checks used by Chaturbate Recorder are published separately on
NuGet, usable in any .NET Windows application.

```powershell
dotnet add package SentinelGuard
```

## Who it is for

Any desktop application that **launches a third-party binary** (yt-dlp, ffmpeg,
an in-house tool…) or **handles paths and URLs supplied by the user** inherits
the same problems: a path can escape the intended folder, a URL can point
somewhere unexpected, and an executable sitting next to yours may have been
swapped.

SentinelGuard gathers the checks worth running **before** trusting any of it.

## What it checks

| Class | What it guards against |
|---|---|
| `PathValidator` | UNC paths, extended paths (`\\?\`, `\\.\`), alternate data streams, reserved device names (`CON`, `NUL`…), symlinks and reparse points |
| `UrlValidator` | Non-HTTPS schemes, domains outside your allow list, denied hosts, unsafe path segments and query strings |
| `BinaryVerifier` | Tampered executables: SHA-256 hash, missing or invalid Authenticode signature, unexpected signing certificate |
| `AclValidator` | Folders writable by `Everyone` / `Authenticated Users` — where a binary can be swapped right before you run it |
| `WorkingDirectoryValidator` | Running from a network share, a temporary folder, the recycle bin or an NTFS-compressed folder |
| `CertificateValidator` | TLS interception: explicit certificate pinning and Subject Alternative Name validation |

## How it is used

**Pure functions**, no side effects. Every check returns a boolean, with an
optional overload giving the exact rejection cause:

```csharp
using SentinelGuard;

if (!PathValidator.IsValidPath(userPath, mustExist: true, out var reason))
{
    Console.WriteLine($"Path rejected: {reason}");
    return;
}

if (!BinaryVerifier.VerifyTrustedBinary(toolPath, expectedHash, out var binReason))
{
    Console.WriteLine($"Binary rejected: {binReason}");
    return;
}
```

Nothing is logged, nothing is thrown behind your back: you decide what to do
with the reason — write it to your logs, show it, or ignore it.

## Scope

Targets `net8.0-windows` and `net10.0-windows`. Windows only, deliberately:
NTFS ACLs, Authenticode and the Windows certificate stores have no
cross-platform equivalent.

**What it is not**: a sandbox or a security boundary. SentinelGuard reduces the
blast radius of untrusted input in a desktop application; it does not contain a
hostile process. Treat it as defence in depth, layered with OS-level controls —
not as a replacement for them.

## Links

- [NuGet page](https://www.nuget.org/packages/SentinelGuard)
- [Source code](https://github.com/Tomoushie/ChaturbateRecorder/tree/main/SentinelGuard)
- [Report an issue](https://github.com/Tomoushie/ChaturbateRecorder/issues)

[← Home](index.html) · [Features](features.html) · [Roadmap](roadmap.html)

</div>

<script src="assets/lang-toggle.js"></script>
