# SentinelGuard

[![NuGet](https://img.shields.io/nuget/v/SentinelGuard.svg)](https://www.nuget.org/packages/SentinelGuard)
[![License](https://img.shields.io/badge/license-MIT%20OR%20Apache--2.0-blue.svg)](https://github.com/Tomoushie/ChaturbateRecorder)

Defensive preflight checks for .NET desktop applications on Windows — validate
paths, URLs, external binaries, ACLs, TLS certificates and your own execution
location *before* trusting them.

**Pure functions, no side effects.** Every check returns a `bool`, with an
optional `out string? reason` overload giving the exact rejection cause. Nothing
is logged, nothing is thrown at you behind your back: you decide what to do with
the reason — log it, show it, ignore it.

Extracted from [Chaturbate Recorder](https://github.com/Tomoushie/ChaturbateRecorder),
where it guards a desktop app that launches third-party executables
(`yt-dlp.exe`, `ffmpeg.exe`) against user-supplied paths and URLs.

## Install

```powershell
dotnet add package SentinelGuard
```

Targets `net8.0-windows` and `net10.0-windows`.

## What it checks

| Class | Guards against |
|---|---|
| `PathValidator` | UNC paths, extended paths (`\\?\`, `\\.\`), alternate data streams, reserved device names (`CON`, `NUL`…), symlinks and reparse points |
| `UrlValidator` | Non-HTTPS schemes, domains outside your allow list, blacklisted hosts, unsafe path segments and query strings |
| `BinaryVerifier` | Tampered executables: SHA-256 hash mismatch, missing or invalid Authenticode signature, unexpected signing certificate (optional CA pinning) |
| `AclValidator` | Folders writable by `Everyone` / `Authenticated Users` — where an attacker could swap a binary you are about to run |
| `WorkingDirectoryValidator` | Running from a network share, temporary folder, recycle bin or NTFS-compressed folder |
| `CertificateValidator` | Man-in-the-middle on outbound TLS: explicit certificate pinning and Subject Alternative Name validation |

## Example

```csharp
using SentinelGuard;

// Reject a path before touching the filesystem.
if (!PathValidator.IsValidPath(userSuppliedPath, mustExist: true, out var reason))
{
    Console.WriteLine($"Path rejected: {reason}");
    return;
}

// Reject a URL before opening a connection.
if (!UrlValidator.IsSafeUrl(url,
        allowedDomains: new[] { "example.com" },
        blacklist: Array.Empty<string>(),
        out var urlReason))
{
    Console.WriteLine($"URL rejected: {urlReason}");
    return;
}

// Refuse to launch a third-party binary that is not exactly what you expect.
if (!BinaryVerifier.VerifyTrustedBinary(toolPath, expectedSha256, out var binReason))
{
    Console.WriteLine($"Binary rejected: {binReason}");
    return;
}
```

Every method also has an overload without the `out string? reason` parameter,
when you only care whether the check passed.

## Why Windows only

`AclValidator` reads NTFS ACLs through `System.Security.AccessControl`, and
`BinaryVerifier` / `CertificateValidator` rely on Authenticode and Windows
certificate stores. These have no cross-platform equivalent, so the package
targets `-windows` TFMs rather than pretending to be portable.

## A note on what this is not

SentinelGuard is a set of **input-validation guardrails**, not a sandbox or a
security boundary. It reduces the blast radius of untrusted input in a desktop
app; it does not contain a hostile process. Treat it as defence in depth, layered
with OS-level controls — not as a replacement for them.

## License

Dual-licensed **MIT OR Apache-2.0** — pick whichever suits your project.

Source, full history and issue tracker:
[github.com/Tomoushie/ChaturbateRecorder](https://github.com/Tomoushie/ChaturbateRecorder).
