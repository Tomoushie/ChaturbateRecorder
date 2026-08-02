# ChaturbateRecorder.Security

Utilitaires de sandbox et de vérification de sécurité pour applications .NET
Windows, extraits de [Chaturbate Recorder](https://github.com/Tomoushie/ChaturbateRecorder).

Bibliothèque de fonctions **pures** : aucune journalisation ni effet de bord
caché — chaque méthode retourne un booléen, avec une surcharge optionnelle
`out string? reason` pour récupérer le motif exact d'un rejet et le
journaliser (ou l'afficher) comme tu le souhaites.

## Installation

```powershell
dotnet add package ChaturbateRecorder.Security
```

## Contenu

| Classe | Rôle |
|---|---|
| `PathValidator` | Chemins de fichiers : rejette UNC, chemins étendus (`\\?\`, `\\.\`), flux ADS, noms réservés Windows, symlinks/reparse points |
| `UrlValidator` | URLs : schéma `https` uniquement, liste blanche/noire de domaines, segments de chemin et query string sûrs |
| `BinaryVerifier` | Hash SHA256, signature Authenticode et chaîne de certification d'un binaire, pinning CA optionnel |
| `AclValidator` | Détecte un droit d'écriture NTFS anormalement large (Everyone / Utilisateurs authentifiés) sur un dossier |
| `WorkingDirectoryValidator` | Détecte un emplacement d'exécution à risque (partage réseau, dossier temporaire, corbeille, dossier compressé NTFS) |
| `CertificateValidator` | Vérification TLS explicite (pinning) et validation du SAN d'un serveur distant |

## Exemple

```csharp
using ChaturbateRecorder.Security;

if (!PathValidator.IsValidPath(userSuppliedPath, mustExist: true, out var reason))
{
    Console.WriteLine($"Chemin refusé : {reason}");
    return;
}

if (!UrlValidator.IsSafeUrl(url, allowedDomains: new[] { "example.com" }, blacklist: Array.Empty<string>(), out var urlReason))
{
    Console.WriteLine($"URL refusée : {urlReason}");
    return;
}
```

## Portée

Windows uniquement (`net10.0-windows`) : `AclValidator` s'appuie sur les ACL
NTFS (`System.Security.AccessControl`), `BinaryVerifier`/`CertificateValidator`
sur des vérifications Authenticode/certificats propres à Windows.

## Licence / origine

Code source et historique complet dans le dépôt principal :
[github.com/Tomoushie/ChaturbateRecorder](https://github.com/Tomoushie/ChaturbateRecorder).
