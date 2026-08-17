# Chaturbate Recorder — version WPF (application GRATUITE)

Portage WinForms → WPF de `..\ChaturbateRecorderApp\`, dont le `CLAUDE.md`
reste la référence pour TOUT le contexte produit, commercial et historique.
Ce fichier-ci ne couvre que la migration.

**État au 2026-08-17** — 37 commits, dépôt local **sans distant**, `dotnet build`
à 0 erreur / 0 avertissement, **364 tests**. L'application navigue, ajoute un
salon, l'enregistre, le surveille, le reconnecte, tient un historique et se
configure.

**LE WINFORMS N'EST PLUS INTOUCHÉ** : l'emplacement des données de
l'utilisateur y a changé le 17-08 (voir « Données de l'utilisateur »), parce que
le faire d'un seul côté aurait fait diverger les deux applications pour de bon.

**MESURER LES AVERTISSEMENTS SUR `-t:Rebuild`, JAMAIS SUR UN BUILD
INCRÉMENTAL.** Un projet à jour ne recompile rien et ne réémet donc AUCUN
avertissement : `dotnet build` rend « 0 avertissement » sans avoir rien
regardé. Six vrais avertissements ont vécu une session entière derrière ce
faux vert, et ils avaient été ANNONCÉS comme absents.

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

**LA MIGRATION EST COMPLÈTE.** Quatre sections, six dialogues, panneau des
journaux, zone de notification, thème mémorisé, icône. **Aucune tranche de
portage n'est identifiée** : `MainForm.cs` n'a plus rien à céder. Ne pas
rouvrir ce chantier sans une raison neuve.

**LA CAPTURE FONCTIONNE — ÉPROUVÉE SUR UN VRAI DIRECT le 2026-08-17.** C'était
le seul inconnu depuis le premier commit. Le mainteneur a enregistré un salon en
ligne avec la version WPF. Sont éprouvés du même coup : la reprise des données
vers `%LocalAppData%` (sa liste de salons est remontée seule), et la barre de
titre sombre — noire quand la fenêtre est inactive, bleue quand elle est active,
ce dernier point étant le réglage Windows et non un défaut.

**LE PREMIER ESSAI A RAPPORTÉ TROIS DÉFAUTS QU'AUCUN TEST N'AURAIT VUS :**
1. la barre de progression restait VIDE pendant toute la capture —
   `Indeterminate` n'était posé qu'à la réception d'une progression, or un
   direct n'en rend souvent aucune. Invisible en thème clair (barre vide sur
   carte blanche) ;
2. **fermer l'application laissait ses `yt-dlp` EN VIE** — quatre processus
   orphelins constatés. `Dispose` ne coupait pas le coordinateur. Le `.part`
   n'était alors jamais renommé. Le WinForms, lui, est correct.

3. **LES CAPTURES NE DEVENAIENT JAMAIS DES VIDÉOS.** Le portage n'avait pas
   repris la FINALISATION : yt-dlp écrit dans `<nom>.mp4.part` et ne le renomme
   qu'en fin NATURELLE de flux, ce qui n'arrive jamais pour un direct. Chaque
   capture restait donc un `.part`, absent de l'historique qui ne liste que
   `.mp4`/`.mkv`/`.mov` — l'application enregistrait des vidéos que personne ne
   voyait. Réparé par `Services/CaptureFinalizer.cs`, appelé à CHAQUE fin, y
   compris sur échec et AVANT toute reconnexion (qui régénère
   `OutputBaseName`). **La MINIATURE n'est toujours pas portée**, écartée avec
   le mainteneur.

**RESTE NON ÉPROUVÉ, et écarté pour l'instant avec le mainteneur** : la
reconnexion automatique quand un direct coupe, et le minuteur d'arrêt. Couverts
par les tests du coordinateur, jamais joués contre un vrai flux.

Ce qui reste ne peut PAS être fait depuis l'environnement d'agent :

1. Ce qui est éprouvé sans direct, et qui l'était avant le premier essai :
   la ligne de commande yt-dlp, options par options, **et le fait que le yt-dlp
   LIVRÉ l'accepte** (`Tests/DownloadArgumentsTests.cs` lance le vrai binaire
   contre un hôte en `.invalid` et distingue le refus d'option — code 2 — de
   l'échec réseau) ; et **les six invariants du coordinateur**
   (`Tests/RecordingCoordinatorTests.cs`), grâce à une FABRIQUE de moteurs que
   `RecordingCoordinator` reçoit désormais — l'application n'en passe pas et
   obtient donc de vrais yt-dlp, les tests passent une doublure. Reste le
   réseau, le flux, le remux, le fichier.
2. **La barre de titre sombre et l'icône** — un coup d'œil du mainteneur.

**Deux variantes publiées** :
- `..\Apercu-WPF\` (250 Mo) exige le **.NET 10 Desktop Runtime** ;
- `..\Apercu-WPF-autonome\` (422 Mo) n'exige RIEN (`--self-contained true`).
Régénérer par `dotnet publish -c Release -r win-x64 --self-contained false|true
-o "..\Apercu-WPF[-autonome]"`. Le csproj copie yt-dlp et ffmpeg
depuis le `Tools\` du dépôt WinForms. **Le mutex d'instance unique et
`settings.json` sont désormais partagés avec l'app WinForms** (voir plus haut) : si celle-ci tourne, la
WPF sort en silence.

**Le dépôt n'a AUCUN distant** — trente-trois commits à un seul endroit sur un
seul disque. C'est le risque restant, et il n'est pas technique.

**LES DEUX COPIES DE `SettingsManager.cs` DIVERGENT désormais**, tranché avec le
mainteneur le 2026-08-16 : la version WPF a un champ `Theme` que la WinForms
n'a pas. L'ajout est rétrocompatible dans les deux sens — chacune ignore ce
qu'elle ne connaît pas.

**WinForms est ACTIVÉ dans ce projet WPF** (`UseWindowsForms`), uniquement pour
`NotifyIcon` : .NET n'offre rien d'autre. Conséquence : `Application`, `Color`,
`Brush`, `UserControl` et `Clipboard` deviennent ambigus. Ils sont tranchés par
des **alias globaux dans le csproj** — une seule fois, pas un alias par fichier
que le onzième fichier écrit aurait oublié.

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
6. **UN GESTIONNAIRE DÉCLARÉ EN ATTRIBUT XAML *ET* EN `+=` S'EXÉCUTE DEUX
   FOIS.** `App.xaml` déclare `Startup`, `Exit` et
   `DispatcherUnhandledException` ; un constructeur qui les réabonne les double.
   Conséquence vécue : au second passage, `Application_Startup` construisait un
   Mutex que le PREMIER passage du MÊME processus détenait déjà, en concluait
   « une autre instance tourne », et fermait l'application une seconde après son
   ouverture. Invisible sans bureau interactif, invisible à la compilation, et
   trouvée par le JOURNAL DE DÉMARRAGE — deux lignes « Demarrage » à 2 ms
   d'intervalle. **C'est pour cela que ce journal existe : le garder.**
7. `System.Windows.Localization` est homonyme de la table de chaînes du projet :
   alias obligatoire, sinon `CS0104` partout.
8. **UN `Setter` DE STYLE NE TRAVERSE PAS LE `ControlTemplate`.** Poser
   `BorderBrush` sur le `Button` ne peint rien si le `Border` du gabarit ne le
   reprend pas : ce sont deux propriétés différentes. Les trois gabarits de
   bouton réservaient `BorderThickness="1"` sans lier `BorderBrush` — un trait
   peint avec rien, donc « Parcourir... » et « Diagnostic... » en TEXTE NU. Le
   gabarit du champ de saisie faisait l'inverse : brosse posée, épaisseur
   laissée à 0, donc aucun cadre nulle part. **Trouvé sur une capture d'écran
   du mainteneur, pas par moi.** Verrouillé par `Tests/ThemeTemplateTests.cs`.

## Données de l'utilisateur

**`rooms.json`, `favorites.json`, `watchlist.json` et `settings.json` vivent
dans `%LocalAppData%\ChaturbateRecorder\`** (2026-08-17), avec les journaux.
`AppConfig.DataFileToRead` rend le fichier neuf s'il existe, sinon l'ancien à
côté de l'exe : la reprise se fait à la première lecture et **l'ancien n'est
jamais supprimé**. `AppConfig.DataFile` écrit toujours au nouvel endroit et crée
le dossier. `trusted-binaries.json` reste près de l'exe — il décrit les binaires
INSTALLÉS, pas l'utilisateur.

**LE MÊME CHANGEMENT EST FAIT DANS LE WINFORMS** (`c3da07b`), et il devait
l'être : sans lui les deux applications divergeaient pour de bon.

**CORRECTION D'UNE NOTE ANTÉRIEURE** : `settings.json` n'a JAMAIS été partagé
avec le WinForms, contrairement à ce qui était écrit ici et dans la mémoire.
Chaque dossier avait le sien, `AppDir` valant `AppContext.BaseDirectory`. Seul
le mutex d'instance unique est réellement global. Depuis ce changement, les deux
applications partagent bel et bien leurs données — mais parce qu'on les y a
mises, pas parce qu'elles l'étaient.

## Localisation

**LE XAML PASSE PAR `{ui:Str cle}`** (`UI/LocalizationSource.cs`), qui rend une
LIAISON vers un indexeur notifiant — pas une chaîne : une extension de balisage
n'est évaluée qu'au chargement, et rendre le texte figerait la langue du
démarrage.

Le portage avait perdu ce câblage sur **61 libellés dans 9 vues**, et quatre y
avaient aussi perdu leurs accents. Rien ne le signalait : build vert, écran
correct, défaut visible seulement en changeant de langue — donc jamais, puisque
l'application démarre dans celle du système. `RefreshLabels` de la barre de
navigation n'était d'ailleurs appelée qu'au constructeur : **rien** ne suivait
le changement de langue à chaud.

Deux filets dans `Tests/ViewLocalizationTests.cs` : aucun libellé en dur dans
une vue, et toute clé citée existe dans la table (`Get` rend la CLÉ quand elle
manque — donc une faute de frappe s'affiche au lieu d'échouer).

`Localization.Current` lève `LanguageChanged`. Les deux abonnés sont
`LocalizationSource` et `MainViewModel`.

**COROLLAIRE DE MÉTHODE, la leçon la plus chère** : relever la valeur d'une
RESSOURCE ne prouve RIEN sur ce qui est AFFICHÉ. Un relevé disait le thème bon
alors que la moitié de l'écran ne bougeait pas ; seule une capture
échantillonnée au pixel l'a montré.

## Vérification visuelle

**LE HARNAIS EXISTE MAINTENANT : `Tests/RenduVisuelTests.cs`.** Il dessine les
vues pour de vrai, échantillonne les PIXELS, et dépose des PNG dans
`%TEMP%\cbr-rendu\` — à REGARDER, aucune assertion ne remplace un coup d'œil.
Il a été écrit parce qu'un défaut de bordure avait traversé toute la migration :
les ressources étaient bonnes, le rendu non, et seule une capture l'a montré.

Quatre choses qu'il a coûtées, à ne pas redécouvrir :
1. **UN SEUL fil STA, persistant, avec sa boucle de répartition.** Un fil neuf
   par test fait **PLANTER le processus hôte** (`Application` et ses ressources
   appartiennent au fil qui les a créées) — et le symptôme est un vert PARTIEL,
   une partie de la suite n'ayant jamais tourné.
2. **Toutes les classes qui touchent un état de processus sont dans la
   collection `EtatDeProcessus`** (`Application.Current`, `Localization.Current`,
   `AppConfig.DataDir`). Sans elle, xunit les parallélise et la suite FIGE.
3. **`IsVisible` vaut toujours faux** dans un arbre détaché : tester
   `Visibility == Visible`.
4. La liste des dictionnaires et des converters est **LUE dans `App.xaml`**, pas
   recopiée : l'ordre de fusion décide de ce qu'un `StaticResource` voit.

Mesurer la BORDURE et non l'intérieur d'un bouton secondaire : en thème clair
son fond est celui de la carte, seule la bordure le détache — c'est exactement
pour ça que son absence le rendait invisible.

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
