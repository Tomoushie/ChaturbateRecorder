# Chaturbate Recorder — version WPF

Portage WinForms → WPF de l'application **gratuite**. Même produit, même
`ChaturbateRecorder.exe`, interface refondue.

> Ce fichier dit comment **construire et lancer**. Pour le contexte technique —
> architecture, pièges WPF mesurés, décisions et leurs raisons — voir
> [`CLAUDE.md`](CLAUDE.md), qui est la référence de ce dépôt.

## Les trois dossiers, et ce qui les sépare

| Dossier | Rôle |
|---|---|
| `ChaturbateRecorderApp WPF\` | **ce dépôt** — l'application gratuite, en WPF |
| `..\ChaturbateRecorderApp\` | la version WinForms, encore en place et fonctionnelle |
| `..\StreamRecorderPro\` | le module **payant**, fermé, sans interface |

L'application **ne référence pas** `StreamRecorderPro` : le module est chargé
par réflexion (`Services/PremiumBridge.cs`). Une référence de compilation
casserait la variante portable auto-contenue, à l'exécution, chez l'utilisateur.

## Construire

```
dotnet build ChaturbateRecorderApp.Wpf.csproj
```

`yt-dlp.exe` et `ffmpeg.exe` sont pris dans `..\ChaturbateRecorderApp\Tools\` et
copiés à côté de l'exécutable. **Sans eux l'application démarre mais ne peut
rien faire** : le sondage et la capture les cherchent dans son propre dossier.

Ils ne sont jamais inclus dans un ZIP de release — ffmpeg est sous licence GPL.

## Lancer

```
dotnet publish ChaturbateRecorderApp.Wpf.csproj -c Release -r win-x64 --self-contained true -o "..\Apercu-WPF-autonome"
```

`--self-contained true` (422 Mo) n'exige aucun runtime installé ;
`false` (250 Mo) exige le **.NET 10 Desktop Runtime**.

En PowerShell, l'opérateur d'appel est nécessaire — sans lui, le chemin est
seulement affiché :

```
& "..\Apercu-WPF-autonome\ChaturbateRecorder.exe"
```

**La republication échoue tant que l'application tourne** (`MSB3027`) : elle
verrouille sa propre DLL. Quitter par l'icône de la zone de notification.

## Tester

```
dotnet test Tests\ChaturbateRecorderApp.Wpf.Tests.csproj
```

241 tests. Quinze fichiers viennent du dépôt WinForms — ils portent sur des
services copiés à l'identique. Deux ont été adaptés aux équivalents WPF.

## Quand quelque chose ne marche pas

1. **Réglages → Outils → Diagnostic…** : versions des binaires, empreintes
   SHA-256, droits des dossiers, joignabilité du réseau, état du module payant.
2. **Réglages → Afficher le panneau des journaux** : la sortie de yt-dlp en
   direct, dans la vue Enregistrer.
3. `%LOCALAPPDATA%\ChaturbateRecorder\logs\` : journal de session (JSONL) et
   sous-dossier `crashes\`.

Le journal trace chaque sortie anticipée du démarrage. Une application qui
disparaît sans rien laisser est un défaut à part entière — c'en est un qui a
déjà coûté une session.

## Deux pièges partagés avec la version WinForms

Les deux applications utilisent **le même mutex d'instance unique** et **le même
`settings.json`**. Si l'une tourne, l'autre sort en silence après avoir signalé
l'évènement de réveil. En fermer une avant d'ouvrir l'autre.
