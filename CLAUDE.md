# Chaturbate Recorder — contexte projet

App WinForms .NET 10, portage d'un script PowerShell (`legacy-powershell/`).
Dépôt public : https://github.com/Tomoushie/ChaturbateRecorder (branche `main`).
Site : https://tomoushie.github.io/ChaturbateRecorder/

## État au 2026-08-03 — version courante : v1.17.0 (app), CI/CD + site Jekyll en place (34.0/37.0), site/README/wiki bilingues (25.0/25.1)

(v1.15.0 Crash Reporter et v1.16.0 Diagnostic Mode ont été livrés sans que
l'en-tête ci-dessus soit mis à jour — corrigé ici.)

**24.0 traité (2026-08-03) — extension de la traduction FR/EN, 4 commits** :
- **Libellés dynamiques des lignes de job** : le panneau "Enregistrements en
  cours" est construit en code (`BuildJobRow`), donc il était resté hors du
  premier passage 20.0. Nouvel enum `JobRowStatus` + `RefreshJobRowLabels`,
  seul endroit qui traduit l'état en texte, appelé aussi depuis
  `ApplyLanguage` — changer de langue en pleine session retraduit les lignes
  déjà affichées **sans** écraser un pourcentage en cours ou un compte à
  rebours de reconnexion. Corrige au passage un vrai bug : l'état terminal
  affichait `$"{state}"`, donc les noms d'enum anglais (Completed/Failed/
  Stopped) même en français.
- **Messages/dialogues/notifications** : 25 `MessageBox` + 5 notifications.
  **Point d'architecture** : `Get(key, lang)` obligeait à faire circuler la
  langue jusqu'au point d'affichage, impossible pour `Program.Main` qui
  affiche un `MessageBox` AVANT que `MainForm` n'existe. D'où
  `Localization.Current` statique + `Format(key, args)`.
  **Piège évité** : `Format` n'a délibérément PAS de surcharge prenant une
  `AppLanguage` — l'enum se lierait silencieusement au `params object[]`
  (donc au premier trou) au lieu de choisir la langue.
  **Arbitrage sécurité** : la langue vit dans `settings.json` à côté de
  l'exe, donc dans le dossier que `WorkingDirectoryValidator` n'a pas encore
  validé. Le contrôle d'emplacement reste la toute première instruction et ce
  seul message se rabat sur `CultureInfo.CurrentUICulture` plutôt que de lire
  un fichier depuis un emplacement non vérifié.
- **Guide de démarrage** : `TutorialForm.Steps` stocke des clés résolues dans
  `RenderStep` (pas à l'initialisation statique, qui figerait le guide dans la
  langue du démarrage). Les noms cités entre guillemets dans la prose
  reprennent mot pour mot les libellés traduits (`button.start`,
  `panel.progress`, `job.open`...) : sinon le guide anglais décrit des boutons
  qui n'existent pas sous ce nom.
- **Changelog** : traduit **à partir de la version courante seulement**
  (1.16.0+), décision de l'utilisateur — l'historique ancien reste français,
  son intérêt étant archivistique et le dialogue "Nouveautés" n'affichant
  qu'une version à la fois. `GetChanges(version, bool english)` prend un bool
  et pas un `AppLanguage` : `Config` est la couche basse (référencée par
  `Security`/`Services`) et n'a pas à dépendre de `UI`.
- **Restent en français par choix documenté** (dans le code) :
  `DiagnosticForm` et `CrashReportForm` (leur sortie est collée dans un ticket
  GitHub — des rapports en deux langues compliqueraient le dépouillement ;
  `CrashReportForm` avait déjà une raison plus forte de ne pas dépendre de
  `Localization` : l'état de l'app peut être corrompu quand il s'affiche), les
  logs, et l'historique ancien du changelog.
- **Tests** : `Tests/LocalizationTests.cs` (8) + `Tests/ChangelogTests.cs` (8),
  77 au total. Les deux garde-fous qui comptent : (a) FR et EN doivent avoir
  exactement les mêmes trous `{0}` — sinon `Format` lève une exception dans
  **une seule** des deux langues, invisible à un test manuel fait en français ;
  (b) toute version >= 1.16.0 doit avoir sa traduction, donc **le prochain bump
  fera échouer la suite** tant que la nouvelle entrée n'est pas traduite. Les
  deux ont été vérifiés en les cassant volontairement.

**26.0 (russe + chinois) — analysé puis reporté (2026-08-03)**, ne pas
re-proposer sans demande explicite. Mesuré : les 76 sites d'appel passent tous
par `Get`/`Format`, le tuple `(Fr, En)` ne fuit que dans `Localization.cs` et
ses tests — un passage à 4 langues toucherait **2 fichiers**, pas 76 sites. Le
faire "en prévision" n'économiserait donc rien. Le vrai coût est ailleurs et
un 4ᵉ champ de tuple ne le règle pas : **pluriels russes** (3 formes selon le
nombre ; `update.runningJobsWarning` dit "{0} enregistrement(s)", que
`string.Format` ne sait pas décliner), **polices CJK** (Segoe UI, imposée par
l'app, n'a aucun glyphe chinois), et **libellés à taille fixe** (le corps du
tutoriel est en 440x196 et l'anglais gagne déjà une ligne à l'étape 3 ; le
russe est ~10-15% plus long que l'anglais). S'ajoute le fait qu'une IA ne peut
pas produire ~300 chaînes RU/ZH vérifiables par le mainteneur.

**30.0/33.0 traités (2026-08-03)** :
- **30.0** : premier package NuGet publié sur GitHub Packages —
  `ChaturbateRecorder.Security` (v1.0.0), nouveau projet séparé
  `ChaturbateRecorder.Security/` + `ChaturbateRecorder.Security.Tests/` à la
  racine du repo (sibling de `ChaturbateRecorderApp.csproj`, exclu de sa
  compilation par défaut — voir `<Compile Remove>` dans le .csproj principal).
  Copie (pas déplacement) des 6 validateurs de `Security/`, retravaillée pour
  être une bibliothèque pure sans effet de bord : `Logger.Log(...)` remplacé
  par un paramètre `out string? reason` optionnel sur chaque méthode (signatures
  historiques conservées via surcharges). `VerifySubjectAlternativeName`
  passe de `internal` à `public`. 63 tests xUnit (portés + quelques nouveaux
  pour ACL/dossier d'exécution). Code de `Security/*.cs` et `Tests/*.cs` de
  l'app principale non touché.
- **33.0** : GitHub Project (v2) créé et lié au repo — "Chaturbate Recorder -
  Backlog" (`https://github.com/users/Tomoushie/projects/2`), 3 cartes Todo
  pour le backlog restant (21.0 portage Mac, 22.0 extension navigateur Mac,
  23.0 installateur).
- **Piège majeur découvert (les deux items étaient bloqués dessus)** : le
  token Git Credential Manager (classique, scopes `gist repo workflow`) n'a
  ni `write:packages` ni `project`. L'utilisateur a d'abord essayé un token
  **fine-grained** (nouvelle génération) : `write:packages` n'existe PAS du
  tout comme permission fine-grained (GitHub Packages ne supporte pas encore
  les tokens fine-grained), et `project` est bien listé mais l'appel
  `createProjectV2` a quand même été refusé (`FORBIDDEN`) même une fois la
  permission "Projects" ajoutée côté fine-grained — dans ce cas précis, seul
  un **token classique** avec les scopes `write:packages`/`project` a
  fonctionné pour les deux. Les deux fenêtres GitHub (Packages ET Projects
  v2) se créent **privées par défaut**, sans option de bascule au moment de
  la création — passage en public fait après coup : impossible via l'API
  REST pour un package NuGet (`PATCH /user/packages/...` → 404, a fallu que
  l'utilisateur le fasse manuellement dans les paramètres du package sur
  github.com) ; possible via GraphQL pour un Project v2
  (`updateProjectV2(input: {projectId, public: true})`).
  Utilisé un token très largement scopé (quasi tous les scopes possibles,
  fourni par l'utilisateur) — à usage strictement limité à ces deux appels,
  supprimé du disque immédiatement après ; suggéré à l'utilisateur de le
  révoquer/restreindre après coup vu son étendue.

**31.0/32.0 traités (2026-08-03)** — documentation uniquement, aucun changement de code applicatif, pas de bump `<Version>`/Changelog :
- **31.0** : refonte du `README.md` — logo (`Assets/logo.png`, extrait de `app.ico`), capture d'écran (`Assets/screenshot.png`, générée avec des données factices — jamais le vrai contenu de capture de l'utilisateur), badges shields.io, nouvelle section "Installation (utilisateurs)" avant la partie développeur, section "Fonctionnalités" alignée sur le site, contenu existant conservé mais réorganisé sous des `<details>` repliables. Cible clarifiée avec l'utilisateur avant de commencer : les deux profils GitHub donnés en exemple (ishandutta2007, grigorkalajdziev) étaient des profils **personnels** (bannière, stats de contributions, typing animation) — l'utilisateur a confirmé vouloir améliorer le README du **projet**, pas un profil perso, donc emprunt du style badges/structure uniquement.
- **32.0** : Wiki GitHub créé (`https://github.com/Tomoushie/ChaturbateRecorder/wiki`, dépôt séparé `ChaturbateRecorder.wiki.git`) — 7 pages : Home, Installation, Guide-utilisation, Configuration, Securite, FAQ-Depannage, Contribuer, plus `_Sidebar.md`. **Piège découvert** : impossible de créer la toute première page d'un wiki GitHub par API ou par simple `git push` sur `<repo>.wiki.git` (repo inexistant tant qu'aucune page n'a été sauvegardée une fois via l'interface web) — a fallu demander à l'utilisateur de cliquer une fois sur "Create the first page", ensuite tout le contenu a pu être poussé normalement par git comme n'importe quel dépôt. Contenu en français uniquement (pas bilingue, à la différence du site/de l'app) — scope non demandé, à proposer seulement si demandé.

**34.0/37.0 traités (2026-08-03)** :
- **34.0** : 5 workflows GitHub Actions dans `.github/workflows/` — Build + Test
  (build + tests xUnit sur push/PR vers `main`) ; Publish Release (sur tag
  `vX.Y.Z` : build/tests, `dotnet publish` standard + portable, zip, création
  de la release GitHub avec les deux ZIP attachés — remplace le script curl
  manuel de la section Conventions ci-dessous, qui reste utilisable en
  secours) ; Update Checker (régénère `docs/latest.json` avec version/URLs/
  SHA256 à chaque release publiée + cron quotidien en filet de sécurité,
  découplé de Publish Release pour couvrir aussi une release créée
  manuellement) ; Security Scan (CodeQL C# + `dotnet list package
  --vulnerable`/`--outdated` sur les deux projets) ; Pages Build (déploie
  `docs/` via `actions/deploy-pages`). Complété par `.github/dependabot.yml`
  (PRs auto pour NuGet + GitHub Actions — 5 PRs de bump de versions d'actions
  déjà mergées). Ces workflows utilisent le `GITHUB_TOKEN` par défaut
  (permissions lecture/écriture activées dans les réglages du dépôt) plutôt
  que le PAT classique du Credential Manager.
  **Correctif trouvé en concevant Publish Release** : avec deux ZIP
  (standard/portable) désormais systématiquement attachés à chaque release,
  `Services/UpdateChecker.cs` prenait juste "le premier .zip trouvé", ce qui
  pouvait faire télécharger la mauvaise variante à "Rechercher une mise à
  jour" (ex: remplacer un build portable self-contained par le build
  standard sans runtime .NET). Détection du build en cours (présence de
  `ChaturbateRecorder.dll` à côté de l'exe) pour choisir le bon ZIP, avec
  repli sur l'ancien comportement si une release ne suit pas la convention
  de nommage. Bump 1.14.0 -> 1.14.1 (patch, pattern 1.13.1) + changelog.
  **Piège découvert** : le job GitHub natif "Automatic Dependency
  Submission (NuGet)" (Settings > Code security > Dependency graph,
  indépendant de nos workflows, activé par défaut) tourne sur `ubuntu-latest`
  et échouait avec `NETSDK1100` en tentant de restaurer
  `ChaturbateRecorderApp.csproj` — `UseWindowsForms=true` tire le
  FrameworkReference `Microsoft.WindowsDesktop.App.WindowsForms`, que le SDK
  .NET refuse de résoudre hors Windows sans
  `<EnableWindowsTargeting>true</EnableWindowsTargeting>` (ajouté sur ce
  projet et sur `Tests/ChaturbateRecorderApp.Tests.csproj`, qui le référence ;
  `ChaturbateRecorder.Security.csproj` n'est pas concerné, il n'a pas
  `UseWindowsForms`). Sans effet sur un build Windows classique.
  **Deux réglages de dépôt à activer manuellement une fois** (pas
  automatisables sans risque depuis l'agent) pour que ces workflows
  fonctionnent : Settings > Actions > General > Workflow permissions =
  "Read and write permissions" ; Settings > Pages > Source = "GitHub
  Actions" (au lieu de "Deploy from a branch") — fait par l'utilisateur.
- **37.0** : thème Jekyll (`jekyll-theme-cayman`, `docs/_config.yml`) + 3
  nouvelles pages sur le site : `docs/features.md` (sandbox, sécurité, logs,
  UI, historique, update checker, watchdog — contenu tiré du code),
  `docs/screenshots.md` (3 captures dans `docs/assets/` : thème clair
  réutilisé du README, thème sombre + fenêtre Paramètres nouvellement
  générées via la technique `DrawToBitmap` habituelle, données factices),
  `docs/roadmap.md` (fait/prévu/écarté, public). Contenu en français
  uniquement (comme le wiki), pas de toggle FR/EN — refaire le mécanisme JS
  de la page d'accueil pour 3 pages Markdown statiques n'apportait pas
  grand-chose ; à étendre si demandé. Liens de navigation bilingues ajoutés
  sur la page d'accueil vers les 3 nouvelles pages.
  **Piège découvert** : `pages-build.yml` (créé en 34.0) publiait `docs/` tel
  quel via `upload-pages-artifact`, sans jamais passer par un build Jekyll —
  sans correction, `_config.yml`/le thème/le rendu Markdown des nouvelles
  pages n'auraient eu aucun effet (les `.md` auraient été servis en texte
  brut). Ajout d'une étape `actions/jekyll-build-pages` avant l'upload
  (`source: ./docs`, `destination: ./_site`). `docs/index.html` n'a pas de
  front matter YAML : Jekyll le copie tel quel sans lui appliquer le thème,
  donc la page d'accueil personnalisée bilingue existante n'est pas
  affectée — vérifié en production après déploiement. Liens internes en
  `.html` explicite (permalink par défaut de Jekyll pour une page racine,
  pas d'URL "pretty" configurée).
- Workflow git utilisé pour 34.0/37.0 (nouveau pour ce projet) : une branche
  + PR par sous-tâche, mergées via squash-merge par l'API GitHub (`gh` non
  installé, curl + token du Credential Manager comme pour les releases).
  Checks CI (`build-test`, CodeQL, `dependencies`) vérifiés avant merge.

**25.0 (suite)/25.1 traités (2026-08-03)** — bascule FR/EN étendue à tout ce
qui restait en français uniquement :
- **25.0 (suite)** : les 3 pages Jekyll de 37.0 (`docs/features.md`,
  `docs/roadmap.md`, `docs/screenshots.md`) étaient restées en français
  uniquement. Même principe que `index.html` mais adapté au contenu long
  (prose plutôt que cartes courtes) : tout le contenu FR et tout le contenu
  EN dupliqués dans deux `<div class="lang-fr"|"lang-en" markdown="1">` par
  page, `docs/assets/lang-toggle.js` bascule leur `display`. **Piège** :
  kramdown ne parse pas le Markdown à l'intérieur d'un bloc HTML par défaut
  — `markdown="1"` sur le `<div>` est nécessaire, sinon le contenu apparaît
  en texte brut. Partage la même clé `localStorage("lang")` que
  `index.html`, donc la langue reste cohérente en naviguant entre toutes
  les pages du site. Vérifié en production (bascule + persistance
  inter-pages testées sur les 3 pages).
- **25.1** : description + champ `homepage` du dépôt GitHub passés en
  anglais (API, pas de fichier). Wiki : 7 pages anglaises ajoutées dans
  `ChaturbateRecorder.wiki.git`, convention `NomPage-EN.md` (ex.
  `Installation-EN.md`), lien de bascule en haut de chaque page (FR comme
  EN), `_Sidebar.md` mise à jour avec les deux listes. README :
  `README.en.md` (traduction complète) + lien réciproque en haut des deux
  fichiers — pas de mécanisme JS possible sur un README (rendu statique
  GitHub), donc fichiers séparés comme pour le wiki plutôt qu'une bascule à
  la volée.
  **Piège découvert (clone du wiki)** : cloner `ChaturbateRecorder.wiki.git`
  dans un chemin trop profond (scratchpad avec UUID de session) échoue sur
  Windows avec `Filename too long` — cloner avec `git -c core.longpaths=true
  clone ...` résout le problème sans avoir à changer d'emplacement.
  **Piège découvert (commit)** : `git commit -am` ne stage QUE les fichiers
  déjà trackés modifiés, pas les nouveaux fichiers non trackés (les 7 pages
  `-EN.md` avaient été oubliées du premier commit/push du wiki) — toujours
  `git add` explicitement les nouveaux fichiers avant de committer, ne pas
  se fier à `-a` pour eux.
  Traduction complète des messages d'erreur/notifications/logs/guide de
  démarrage/changelog de l'app **toujours hors périmètre** (seule l'UI
  principale de l'app est traduite depuis 20.0) — non demandé ici, ne pas
  se lancer dedans sans demande explicite.

**Dossier du projet déplacé** : `E:\Corpus\Documents\Chaturbate Record\Projet logiciel\ChaturbateRecorderApp` (l'ancien `...\Projet logiciel\NET 8 Old\ChaturbateRecorderApp` n'existe plus/est obsolète, l'utilisateur devait supprimer "NET 8 Old" après la copie).

v1.14.0 — items 18.0/19.0/25.0 du fichier de notes perso :
- **19.0** (le plus gros morceau) : `UI/SettingsForm.cs`, nouvelle fenêtre modale séparée qui regroupe thème/langue/dossier de sauvegarde/cookies/proxy/reconnexion automatique par défaut (déplacés hors de `MainForm` — qualité/codec/format y restent, ce sont des choix par enregistrement). Communique avec `MainForm` par callbacks (`Action<AppTheme>`/`Action<AppLanguage>`/`Action<string>`), pas par référence directe aux contrôles. `AutoReconnectDefault` devient un vrai réglage persisté (ne l'était pas avant). Le X de la fenêtre principale masque désormais (`Hide`) au lieu de fermer l'app (`_isReallyClosing` distingue ce cas du clic sur "Fermer" du nouveau menu contextuel de la zone de notification : Ouvrir/Paramètres/Fermer, double-clic = Ouvrir) — un enregistrement en cours continue donc en arrière-plan. Notice affichée une seule fois (`UserSettings.HasSeenTrayHint`).
- **18.0** : bouton "Signaler un bug" (icône "alert") qui ouvre un ticket GitHub pré-rempli (version/OS/dossier de capture) dans le navigateur — rien n'est collecté depuis l'appli.
- **25.0** : `docs/index.html` traduit en anglais, bascule FR/EN via un petit bouton sans rechargement de page (comme le sélecteur de langue de l'app), détecte la langue du navigateur au premier chargement, mémorise ensuite (localStorage).
- Bug corrigé au passage : `modeToggleButton.Text` utilisait un littéral français en dur dans `ApplyUiMode`, écrasant la traduction anglaise à chaque bascule de mode.

v1.13.1 : correctifs signalés par l'utilisateur (pas de nouvelle fonctionnalité) :
- Miniature/réencodage associés au mauvais fichier vidéo quand un même salon était enregistré plusieurs fois — `FindLatestCaptureFile` (heuristique "fichier le plus récent" ambiguë) remplacé par `FindOwnCaptureFile` (chemin exact via nouveau `RecordingJob.OutputBaseName`, déterministe).
- Fenêtre non responsive en largeur — `Anchor` Left+Right ajouté sur les panneaux/champs larges, Top+Right sur les boutons.
- Boutons "Site web"/"Ouvrir dossier"/"Supprimer favori" au texte rogné — tailles ajustées.

Toutes les sections d'une liste de tâches numérotée (1 à 9) ont été traitées, plus l'item 20.0 (sélecteur de langue) :
1. Sécurité (hash binaires, sandbox chemins/URL, dossier d'exécution, ACL, QR code) — 1.1 (signature de l'EXE) reste bloqué, nécessite un certificat de signature de code que l'utilisateur n'a pas.
2. Robustesse (watchdog anti-freeze, ffmpeg hors thread UI, logs JSON, rotation des logs)
3. UI (icônes SVG, mode simple/avancé, notifications toast, barre de progression animée)
4. Fonctionnel (reconnexion auto, historique des enregistrements — nom/durée/taille/date déjà couverts par ailleurs)
5. Maintenance (architecture déjà modulaire, suite xUnit 59 tests dans `Tests/`)
6. Distribution (single-file self-contained fonctionne, NativeAOT écarté car non supporté par WinForms, signature Authenticode bloquée sur certificat)
7. Modernisation UI (palette Windows 11, Segoe UI, boutons arrondis sans bordure, espacement)
8. Modernisation UI, suite (v1.11.0) : GroupBox remplacés par `UI/RoundedGroupPanel.cs` (bordure arrondie + titre dessiné à la main, mêmes coordonnées d'enfants qu'avant), ombre légère sous chaque panneau, remplissage animé de la ProgressBar en fin de job (`AnimateProgressBarFill` dans `MainForm.cs`), couleurs hover/pressed sur les boutons via `FlatAppearance` dans `ThemeManager.cs`.
9. Modernisation avancée (v1.12.0) : fondu d'ouverture au démarrage + clignotement léger au changement de mode simple/avancé (`AnimateOpacity`/`PulseOpacity` dans `MainForm.cs`), transition de couleur animée entre thème clair/sombre (`ThemeManager.Palette`/`GetPalette`/`ApplyPalette`/`LerpPalette` + `MainForm.AnimateThemeTransition`), couleurs hover/appui des boutons animées en douceur (remplace le changement instantané natif de `FlatAppearance` par un suivi manuel souris + interpolation). Palette pastel (9.1) explicitement écartée par l'utilisateur pour rester sur le bleu Windows 11 déjà établi. 9.3 (mode simple/avancé) et 9.4 (notifications Toast) étaient déjà couverts par la section 3.

20.0 Sélecteur de langue (v1.13.0) : Français/English, `UI/Localization.cs` (dictionnaire clé -> (Fr,En)) + `MainForm.ApplyLanguage(lang)`. **Portée volontairement limitée à l'UI principale** (labels, boutons, cases à cocher, en-têtes de colonnes, items de ComboBox, bascule de mode) — décision prise avec l'utilisateur. Messages d'erreur/confirmations, notifications toast, logs, guide de démarrage (TutorialForm) et historique des nouveautés (Changelog) restent en français, pas couverts par ce passage (générés à des dizaines d'endroits différents dans le code). Nouveau sélecteur "Langue :" à côté du thème, visible en mode avancé seulement ; barre du haut passée à deux lignes (plus assez de place sur une seule) — `grpRecordY` à 75 au lieu de 50. Choix persisté dans `UserSettings.Language` ("fr"/"en").

**(Traité depuis)** : cette "prochaine étape" — messages d'erreur/MessageBox,
notifications, guide de démarrage, changelog — a été faite en 24.0 (v1.17.0),
voir la section en haut de ce fichier.

Items explicitement en attente/écartés, ne pas re-proposer sans raison nouvelle :
- 1.1 et 6.3 (signature Authenticode) : bloqués sur certificat, pas de solution logicielle possible.
- 6.2 (NativeAOT) : écarté, fait technique (WinForms non supporté), pas une question de config.
- 9.1 (couleurs pastel) : écarté par l'utilisateur, palette bleu Windows 11 conservée.
- 17.0 (extension navigateur) : gros projet à part, reporté à la demande de l'utilisateur.
- 15.0 (portable vs installeur choisi au 1er lancement) : concept corrigé — c'est un choix de *publication* (deux fichiers de release séparés), pas un dialogue runtime. Déjà en place via les deux formats de release.
- 20.0 (traduction des messages/notifications/guide/changelog) : **fait en
  24.0 / v1.17.0**. Restent en français par choix documenté : DiagnosticForm,
  CrashReportForm, les logs et l'historique ancien du changelog.
- 26.0 (russe + chinois) : analysé et reporté en 2026-08-03, voir la section
  dédiée en haut de ce fichier. Le blocage n'est pas la structure de données
  (2 fichiers à toucher) mais pluriels russes / polices CJK / libellés à
  taille fixe / impossibilité de produire des traductions vérifiables.

Restent dans le fichier de notes perso, non traités, à proposer seulement si demandé :
- 21.0 (portage Macintosh), 22.0 (extension navigateur portée sur Mac/Safari — dépend aussi de 17.0), 23.0 (installateur avec étapes d'installation).

## Conventions établies dans ce projet

**Versioning & releases** — depuis 34.0, le workflow GitHub Actions "Publish
Release" automatise les étapes 6 (publish standard+portable, zip, upload sur
la release) à partir d'un tag poussé — les étapes manuelles ci-dessous
restent documentées comme méthode de secours/référence :

À chaque lot de fonctionnalités livré :
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

**Piège WinForms — `DrawToBitmap` sur un `Form` top-level** : contrairement à un `Panel` enfant, `DrawToBitmap` sur un formulaire top-level (ex: une fenêtre modale comme `SettingsForm`) inclut la barre de titre et les bordures. Dimensionner le bitmap sur `form.ClientSize` écrase alors le contenu du bas (la barre de titre "mange" de la hauteur sans que le bitmap ne s'agrandisse en conséquence). Utiliser `form.Size` (pas `.ClientSize`) pour la taille du bitmap et du rectangle de dessin.

**Piège WinForms — `Anchor` posé avant `Controls.Add`** : définir `Anchor` dans l'initialiseur d'objet d'un contrôle (avant qu'il soit ajouté à son parent) capture une marge basée sur un `Parent` encore `null` — le contrôle se retrouve projeté hors de la fenêtre dès le premier redimensionnement (marge négative interprétée comme "encore plus loin du bord"). Toujours poser `control.Anchor = ...;` en instruction séparée, APRÈS le `Controls.Add`/`AddRange` qui le parente.

**NuGet** — Le `NuGet.Config` global de cette machine (`%APPDATA%\NuGet\NuGet.Config`) a une liste de sources vide. Un `NuGet.Config` local (déjà présent à la racine du projet) ajoute `nuget.org`, sans toucher au fichier global. Sans lui, toute dépendance externe (y compris le self-contained publish, qui a besoin de runtime packs) échoue avec `NU1100`.

**Tests** — `Tests/ChaturbateRecorderApp.Tests.csproj` (xUnit, `net10.0-windows` car référence le projet principal WinForms). `Properties/AssemblyInfo.cs` expose `InternalsVisibleTo` pour tester des méthodes `internal` (ex: `CertificateValidator.VerifySubjectAlternativeName`). Un vrai bug a été trouvé et corrigé via ces tests : le SAN TLS dépendait du texte localisé par l'OS (`Format()`), remplacé par un décodage ASN.1 direct.

**Style de commit utilisateur** — messages de commit en français dans le titre court, corps détaillé technique (avec le "pourquoi"), toujours signés `Co-Authored-By: Claude Sonnet 5`.
