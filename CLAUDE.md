# Chaturbate Recorder — contexte projet

App WinForms .NET 10, portage d'un script PowerShell (`legacy-powershell/`).
Dépôt public : https://github.com/Tomoushie/ChaturbateRecorder (branche `main`).
Site : https://tomoushie.github.io/ChaturbateRecorder/

## État au 2026-08-02 — version courante : v1.12.0

Toutes les sections d'une liste de tâches numérotée (1 à 9) ont été traitées :
1. Sécurité (hash binaires, sandbox chemins/URL, dossier d'exécution, ACL, QR code) — 1.1 (signature de l'EXE) reste bloqué, nécessite un certificat de signature de code que l'utilisateur n'a pas.
2. Robustesse (watchdog anti-freeze, ffmpeg hors thread UI, logs JSON, rotation des logs)
3. UI (icônes SVG, mode simple/avancé, notifications toast, barre de progression animée)
4. Fonctionnel (reconnexion auto, historique des enregistrements — nom/durée/taille/date déjà couverts par ailleurs)
5. Maintenance (architecture déjà modulaire, suite xUnit 59 tests dans `Tests/`)
6. Distribution (single-file self-contained fonctionne, NativeAOT écarté car non supporté par WinForms, signature Authenticode bloquée sur certificat)
7. Modernisation UI (palette Windows 11, Segoe UI, boutons arrondis sans bordure, espacement)
8. Modernisation UI, suite (v1.11.0) : GroupBox remplacés par `UI/RoundedGroupPanel.cs` (bordure arrondie + titre dessiné à la main, mêmes coordonnées d'enfants qu'avant), ombre légère sous chaque panneau, remplissage animé de la ProgressBar en fin de job (`AnimateProgressBarFill` dans `MainForm.cs`), couleurs hover/pressed sur les boutons via `FlatAppearance` dans `ThemeManager.cs`.
9. Modernisation avancée (v1.12.0) : fondu d'ouverture au démarrage + clignotement léger au changement de mode simple/avancé (`AnimateOpacity`/`PulseOpacity` dans `MainForm.cs`), transition de couleur animée entre thème clair/sombre (`ThemeManager.Palette`/`GetPalette`/`ApplyPalette`/`LerpPalette` + `MainForm.AnimateThemeTransition`), couleurs hover/appui des boutons animées en douceur (remplace le changement instantané natif de `FlatAppearance` par un suivi manuel souris + interpolation). Palette pastel (9.1) explicitement écartée par l'utilisateur pour rester sur le bleu Windows 11 déjà établi. 9.3 (mode simple/avancé) et 9.4 (notifications Toast) étaient déjà couverts par la section 3.

**Section 9 entièrement traitée.** Pas de section 10 planifiée pour l'instant — attendre une nouvelle demande de l'utilisateur avant de proposer une suite. Le fichier de notes perso (`E:\Corpus\Documents\Chaturbate Record\Projet en Powershell\A ajouter.txt`, hors dépôt) contient encore les points 17.0 à 23.0 (extension navigateur, bouton signaler un bug, menu clic droit sur l'icône, sélection de langue, portage Mac, installateur) — à proposer seulement si l'utilisateur le mentionne.

Items explicitement en attente/écartés, ne pas re-proposer sans raison nouvelle :
- 1.1 et 6.3 (signature Authenticode) : bloqués sur certificat, pas de solution logicielle possible.
- 6.2 (NativeAOT) : écarté, fait technique (WinForms non supporté), pas une question de config.
- 9.1 (couleurs pastel) : écarté par l'utilisateur, palette bleu Windows 11 conservée.
- 17.0 (extension navigateur) : gros projet à part, reporté à la demande de l'utilisateur.
- 15.0 (portable vs installeur choisi au 1er lancement) : concept corrigé — c'est un choix de *publication* (deux fichiers de release séparés), pas un dialogue runtime. Déjà en place via les deux formats de release.

## Conventions établies dans ce projet

**Versioning & releases** — à chaque lot de fonctionnalités livré :
1. Bump `<Version>` dans `ChaturbateRecorderApp.csproj` (incrément mineur, ex: 1.9.0 -> 1.10.0)
2. Ajouter une entrée dans `Config/Changelog.cs` (affichée en local via le dialogue "Nouveautés")
3. `dotnet build` + `dotnet test Tests/ChaturbateRecorderApp.Tests.csproj` avant de committer
4. Commit avec identité Git via variables d'environnement (PAS `git config`, jamais autorisé) :
   `GIT_AUTHOR_NAME="Tomoushie" GIT_AUTHOR_EMAIL="Tomoushie@users.noreply.github.com"` (idem COMMITTER)
5. `git tag -a vX.Y.Z` + push du commit et du tag
6. Publier DEUX formats de release et les uploader via l'API GitHub (curl + token du Git Credential Manager, PAS de `gh` CLI — il n'est pas installé) :
   - Standard : `dotnet publish -c Release -r win-x64 --self-contained false` (~530 Ko, nécessite .NET 10 Desktop Runtime)
   - Portable : `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` (~46 Mo compressé, autonome)
   - Récupérer le token : `printf "protocol=https\nhost=github.com\n\n" | git credential fill | grep '^password=' | cut -d= -f2-`
   - Ne JAMAIS inclure `yt-dlp.exe`/`ffmpeg.exe` dans les ZIP de release (licence GPL de ffmpeg — voir README)

**Vérification visuelle des changements UI** — WinForms ne peut pas être piloté par `computer-use` (exe non reconnu comme "app installée"). À la place : ajouter temporairement dans le constructeur de `MainForm` un handler `Shown += (s,e) => { ... DrawToBitmap ... Environment.Exit(0); }` qui capture un vrai rendu en PNG dans le scratchpad, à regarder via `Read`. Piège découvert : `Form.DrawToBitmap()` ne rend pas correctement le fond d'un `Panel` avec `AutoScroll=true` imbriqué — capturer directement `contentPanel.DrawToBitmap(...)` à la place donne le rendu réel. Pour vérifier une animation (ex: transition de thème) : `Shown += async (s,e) => { ... await Task.Delay(...); Capture(...); TriggerAnimation(); await Task.Delay(...); Capture(...); Environment.Exit(0); }` — capturer après un délai supérieur à la durée de l'animation donne l'état final, `DrawToBitmap` n'étant pas affecté par `Form.Opacity` (qui n'agit que sur le compositing OS, pas le rendu GDI). Toujours retirer ce code de debug avant de committer, et penser aussi à commenter/décommenter `ShowFirstRunDialogs()` si un dialogue de premier lancement bloquerait la capture.

**NuGet** — Le `NuGet.Config` global de cette machine (`%APPDATA%\NuGet\NuGet.Config`) a une liste de sources vide. Un `NuGet.Config` local (déjà présent à la racine du projet) ajoute `nuget.org`, sans toucher au fichier global. Sans lui, toute dépendance externe (y compris le self-contained publish, qui a besoin de runtime packs) échoue avec `NU1100`.

**Tests** — `Tests/ChaturbateRecorderApp.Tests.csproj` (xUnit, `net10.0-windows` car référence le projet principal WinForms). `Properties/AssemblyInfo.cs` expose `InternalsVisibleTo` pour tester des méthodes `internal` (ex: `CertificateValidator.VerifySubjectAlternativeName`). Un vrai bug a été trouvé et corrigé via ces tests : le SAN TLS dépendait du texte localisé par l'OS (`Format()`), remplacé par un décodage ASN.1 direct.

**Style de commit utilisateur** — messages de commit en français dans le titre court, corps détaillé technique (avec le "pourquoi"), toujours signés `Co-Authored-By: Claude Sonnet 5`.
