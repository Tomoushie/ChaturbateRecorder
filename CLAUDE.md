# Chaturbate Recorder — version WPF (application GRATUITE)

Portage WinForms → WPF de `..\ChaturbateRecorderApp\`, dont le `CLAUDE.md`
reste la référence pour TOUT le contexte produit, commercial et historique.
Ce fichier-ci ne couvre que la migration.

**État au 2026-08-16** — 5 commits, dépôt local sans distant, `dotnet build`
à 0 erreur / 0 avertissement. Le WinForms n'est pas touché et compile toujours.

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
complet (`Themes/`, `UI/ThemeManager.cs`), la fenêtre principale, la barre de
navigation, la carte de salon, la vue « Enregistrer », et démarrer/arrêter un
enregistrement (`Services/RecordingCoordinator.cs`).

`SideBar` (209 l.), `IconManager` (136 l.) et la dépendance NuGet `Svg` ont
disparu : un `ListBox` fait le test de survol, WPF dessine les tracés.

## Reste

`MainForm.cs` (3 527 l.) garde la reconnexion automatique, le minuteur et le
sondage d'état — c'est ce qui rendrait l'interrupteur « auto » réellement
actif. Puis les vues Historique / Réglages / Soutenir, les 8 fenêtres de
dialogue, les contrôles `Themed*` en `ControlTemplate`.

**NON ÉPROUVÉ** : qu'un enregistrement démarre vraiment. Cela demande un direct
réel. Compilation, rendu et logique de la table sont vérifiés, pas la chaîne.

**Dettes** : `Models/Settings.cs` double `UserSettings` de
`Services/SettingsManager.cs` EN RANGEANT AILLEURS (`%AppData%\StreamRecorderPro`
au lieu de `%LocalAppData%\ChaturbateRecorder`) — recopié tel quel, il rendrait
invisibles les réglages de tous les utilisateurs actuels. L'interrupteur
« auto » est une `CheckBox` standard et non l'interrupteur 34×18 dessiné.

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
4. `System.Windows.Localization` est homonyme de la table de chaînes du projet :
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
