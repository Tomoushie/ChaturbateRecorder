# Chaturbate Recorder — version WPF (application GRATUITE)

Portage WinForms → WPF de `..\ChaturbateRecorderApp\`, dont le `CLAUDE.md`
reste la référence pour TOUT le contexte produit, commercial et historique.
Ce fichier-ci ne couvre que la migration.

**État au 2026-08-16** — 23 commits, dépôt local **sans distant**, `dotnet build`
à 0 erreur / 0 avertissement. Le WinForms n'est pas touché et compile toujours.
L'application navigue, ajoute un salon, l'enregistre, le surveille, le
reconnecte, tient un historique et se configure.

## Architecture

- **Cette application ne référence PAS `StreamRecorderPro`** : le module payant
  est chargé par RÉFLEXION (`Services/PremiumBridge.cs`). Une référence de
  compilation casserait la variante portable `PublishSingleFile`, chez
  l'utilisateur, à l'exécution.
- `AssemblyName` = `ChaturbateRecorder` et `RootNamespace` =
  `ChaturbateRecorderApp`, identiques au WinForms **à dessein** : l'installateur
  et le vérificateur de mise à jour ciblent `ChaturbateRecorder.exe`, et le
  namespace identique laisse les 21 services portés mot pour mot.
- `NuGet.Config` local obligatoire : la config globale de la machine n'a aucune
  source, sans lui tout `restore` échoue en `NU1100`.

## Fait

Les 21 services (19 copiés à l'identique), `UI/Localization.cs`, le thème
complet (`Themes/`, `UI/ThemeManager.cs`), la fenêtre principale et sa barre de
navigation, la carte de salon dans ses huit états, et **les quatre sections** :
Enregistrer, Historique, Réglages, Soutenir.

Côté logique : `RecordingCoordinator` (démarrage, arrêt, reconnexion, minuteur),
`MonitorService` (sondage et déclenchement automatique), `HistoryService`.

`SideBar` (209 l.), `IconManager` (136 l.) et la dépendance NuGet `Svg` ont
disparu : un `ListBox` fait le test de survol, WPF dessine les tracés.

## Reste

**LANCER L'APPLICATION** — deux variantes publiées :
- `..\Apercu-WPF\` (250 Mo) exige le **.NET 10 Desktop Runtime** ;
- `..\Apercu-WPF-autonome\` (422 Mo) n'exige RIEN (`--self-contained true`).
Régénérer par `dotnet publish -c Release -r win-x64 --self-contained false|true
-o "..\Apercu-WPF[-autonome]"`. Le csproj copie yt-dlp et ffmpeg
depuis le `Tools\` du dépôt WinForms. **Le mutex d'instance unique et
`settings.json` sont PARTAGÉS avec l'app WinForms** : si celle-ci tourne, la
WPF sort en silence.

Par ordre de valeur :
1. **Les 4 dialogues restants** (~1 500 l.) : Nouveautés, Remerciements,
   Signalement, Légalité, Guide de démarrage, et le panneau des journaux.
   Faits : Nouveautés, Rapport de plantage, Diagnostic. La fenêtre Paramètres
   est devenue une SECTION et n'a plus lieu d'être.
2. **Icône de zone de notification** : le WinForms se MASQUE dans la zone de
   notification au lieu de se fermer (19.0), et `ShowWindowEventName` est
   déclaré mais personne ne l'écoute — la seconde instance signale un évènement
   que rien ne reçoit. WPF n'a pas de NotifyIcon : il faut trancher entre
   `<UseWindowsForms>true</UseWindowsForms>` et une dépendance NuGet, **décision
   du mainteneur**.
Les contrôles `Themed*` sont FAITS (`Themes/Natifs.xaml`).

**Toute la logique d'enregistrement est portée** : démarrage, arrêt, sondage
d'état, déclenchement automatique, reconnexion et minuteur avec son sélecteur
de durée. **Les QUATRE sections sont faites** — Enregistrer, Historique,
Réglages, Soutenir. `MainForm.cs` n'a plus rien à céder.

**LES DEUX COPIES DE `SettingsManager.cs` DIVERGENT désormais**, tranché avec le
mainteneur le 2026-08-16 : la version WPF a un champ `Theme` que la WinForms
n'a pas. L'ajout est rétrocompatible dans les deux sens — chacune ignore ce
qu'elle ne connaît pas.

**WinForms est ACTIVÉ dans ce projet WPF** (`UseWindowsForms`), uniquement pour
`NotifyIcon` : .NET n'offre rien d'autre. Conséquence : `Application`, `Color`,
`Brush`, `UserControl` et `Clipboard` deviennent ambigus. Ils sont tranchés par
des **alias globaux dans le csproj** — une seule fois, pas un alias par fichier
que le onzième fichier écrit aurait oublié.

**NON ÉPROUVÉ** : qu'un enregistrement démarre vraiment. Cela demande un direct
réel. Compilation, rendu et logique de la table sont vérifiés, pas la chaîne.

**Plus de dette ouverte.** L'interrupteur « auto » est désormais le vrai
interrupteur 34×18. Le dossier `Models\`
du squelette a été SUPPRIMÉ (quatre classes mortes, dont un `AppSettings` qui
rangeait sous `%AppData%\StreamRecorderPro` au lieu de
`%LocalAppData%\ChaturbateRecorder`).

## Pièges WPF payés ici, tous MESURÉS

1. **Un `ResourceDictionary` d'application SCELLE tout `Freezable`** qu'on lui
   confie — pinceau lu, clone reposé, pinceau neuf, dictionnaire créé en code
   puis fusionné : tous ressortent `IsFrozen = true`. Animer un pinceau en
   ressource est donc impossible, et l'échec est SILENCIEUX (build vert, thème
   qui ne bouge pas).
2. **Poser `Application.Current.Resources[clé] = pinceau` ne réévalue PAS les
   `DynamicResource` d'un Setter de Style** — seuls ceux des DÉCLENCHEURS
   suivent. Il faut remplacer un dictionnaire ENTIER dans `MergedDictionaries`.
   `ThemeManager.SetPalette` fait cela.
3. **Un style IMPLICITE avec un Setter explicite BAT l'héritage de propriété.**
   Un `<Style TargetType="TextBlock">` posant `Foreground` volait sa couleur à
   tout bouton Primary et Danger. `Foreground` s'hérite déjà du style de
   `Window` : ne pas le reposer.
4. **L'ORDRE DE FUSION DES DICTIONNAIRES COMPTE** : un `StaticResource` ne voit
   QUE ce qui a été fusionné AVANT lui. `Natifs.xaml` placé en dernier faisait
   planter l'application au PREMIER affichage d'une carte — build vert, rien ne
   vérifie ces clés à la compilation.
5. **Le rapporteur de plantage peut TUER le processus.** Si l'affichage de la
   fenêtre lève, l'exception ne remonte pas dans son `try` : WPF la route par
   `Dispatcher.CatchException` vers le gestionnaire, qui redemande la fenêtre.
   Récursion infinie → STACK OVERFLOW, qui ne se rattrape pas. D'où le garde-fou
   `_affichageEnCours` dans `CrashWindow`.
6. `System.Windows.Localization` est homonyme de la table de chaînes du projet :
   alias obligatoire, sinon `CS0104` partout.

**COROLLAIRE DE MÉTHODE, la leçon la plus chère** : relever la valeur d'une
RESSOURCE ne prouve RIEN sur ce qui est AFFICHÉ. Un relevé disait le thème bon
alors que la moitié de l'écran ne bougeait pas ; seule une capture
échantillonnée au pixel l'a montré.

## Vérification visuelle

Il n'y a pas de bureau interactif dans l'environnement d'agent :

- une `Window` NON AFFICHÉE ne rend rien (capture blanche) — détacher son
  `Content` et le rendre dans un `Border` autonome ;
- `Show()` ne tient pas : mesuré, la fenêtre se ferme d'elle-même 27 ms après ;
- **UN THÈME PAR PROCESSUS** (variable `CAPTURE_THEME`), sinon la capture montre
  un état transitoire même après `UpdateLayout` — variante WPF du
  « `DoEvents` avant `DrawToBitmap` » déjà payé en 103.0 côté WinForms.
- **Injecter des salons FICTIFS** pour toute capture de la vue « Enregistrer » :
  elle affiche des noms de salons, et une capture publiée en a déjà montré des
  vrais.

## Conventions

Identiques au dépôt WinForms : commits en français, corps technique disant le
POURQUOI, `Co-Authored-By: Claude <modèle> <noreply@anthropic.com>` avec le
modèle qui a réellement écrit. Identité git par variables d'environnement,
jamais `git config`.

Génération de code par l'orchestrateur local (`/generate-batch`, port 5001) :
lire les VERDICTS, pas le code. Les fautes récurrentes du modèle — bloc collé
en double, visibilité changée, gestionnaires aux mauvaises signatures — sont
toutes trouvées par le compilateur.
