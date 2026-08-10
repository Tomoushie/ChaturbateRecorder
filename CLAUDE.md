# Chaturbate Recorder — contexte projet

App WinForms .NET 10, portage d'un script PowerShell (`legacy-powershell/`).
Dépôt public : https://github.com/Tomoushie/ChaturbateRecorder (branche `main`).
Site : https://tomoushie.github.io/ChaturbateRecorder/

## État au 2026-08-10 — version courante : v1.34.1 (app, NON PUBLIÉE — v1.34.0 l'est), SentinelGuard 1.2.0 sur nuget.org, CI/CD + site Jekyll en place (34.0/37.0), site/README/wiki bilingues (25.0/25.1)

**Publiés le 2026-08-09** : v1.29.0 à v1.33.0 côté application, et
`sentinelguard-v1.1.0` puis `sentinelguard-v1.2.0` sur nuget.org. **Redemander
avant tout tag** : il déclenche une release publique.

(Cet en-tête a déjà été laissé en retard trois fois : v1.15.0 Crash Reporter,
v1.16.0 Diagnostic Mode, puis v1.19.0. **Le mettre à jour fait partie du bump
de version**, au même titre que `<Version>` dans le csproj et l'entrée de
`Config/Changelog.cs` — voir la section Conventions en bas de fichier.)

**102.0 EN COURS (2026-08-10) — signalements envoyés depuis l'application, sans
compte GitHub. BLOQUÉ sur un déploiement que seul le mainteneur peut faire.**
- **Architecture tranchée par le mainteneur** : un Worker Cloudflare (gratuit)
  garde le jeton et crée l'issue. La « BDD » demandée, ce sont les issues du
  dépôt filtrées sur l'étiquette `via-application` — pas un second endroit à
  consulter.
- **Le jeton ne peut PAS vivre dans l'application** : un exécutable distribué
  publiquement n'est pas un coffre, il s'extrait en quelques secondes et
  donnerait un droit d'écriture au nom du mainteneur.
- **L'URL du Worker, elle, EST extractible, et c'est irréductible.** Un secret
  partagé le serait tout autant ; Turnstile suppose un navigateur. Défense
  assumée : 3 envois/heure par adresse, 200/jour au global, bornes de taille,
  modération. Supprimer le Worker coupe tout, l'app retombant sur GitHub.
- **AUCUN champ de contact, pas même optionnel** : les issues sont publiques,
  et y publier l'adresse de quelqu'un qui signale un bug sur un enregistreur de
  cams serait un tort réel. Le suivi passe par l'URL de l'issue, renvoyée à
  l'application. Aucune IP stockée non plus (condensé salé, TTL 1 h).
- **Le quota n'est consommé qu'APRÈS création réussie** : une panne de GitHub
  ne doit pas coûter son quota à quelqu'un dont le signalement n'est pas parti.
- **AUCUNE LIGNE D'INTERFACE ÉCRITE, volontairement.** C'est l'erreur de 92.0
  (UI, traductions et tests écrits avant d'avoir vérifié le contact réel,
  quatre versions perdues). Le contact se prouve d'abord par un vrai POST.
- **Ce qui manque pour reprendre** : le mainteneur suit
  `report-worker/README.md` (jeton fine-grained limité à Issues:write sur ce
  seul dépôt, `wrangler deploy`), transmet l'URL, on prouve la chaîne, PUIS on
  écrit la fenêtre. Les fonctions pures du Worker sont déjà éprouvées en local.

**v1.34.1 (2026-08-10) — 114.0 : ascenseurs blancs en thème sombre** :
- **Signalé avec une capture, et la capture disait la vraie cause** : certains
  ascenseurs sombres, d'autres blancs. Le `SetWindowTheme(..., "DarkMode_
  Explorer")` de 103.0 vivait DANS `ThemedListView`, donc seules l'historique
  et la surveillance en bénéficiaient. Favoris, Logs et l'ascenseur de la
  fenêtre principale restaient blancs. **Le défaut n'était pas la couleur mais
  le mélange** — un seul type de contrôle était câblé.
- **`UI/NativeScrollBars.cs`, un seul exemplaire du P/Invoke**, appelé depuis
  les cas ListBox / TextBox / RichTextBox / Panel / ListView de `ThemeManager`.
  Même leçon que la fusion Security ↔ SentinelGuard : du code dupliqué ou
  enfermé dans une classe finit par ne bénéficier qu'à un seul appelant.
- **Le handle peut ne pas exister à l'appel** (contrôle jamais affiché, mode
  simple, fenêtre pas encore ouverte). L'ancien code testait `IsHandleCreated`
  et abandonnait en silence. La nouvelle classe REJOUE sur `HandleCreated`.
- **Mesuré, pas jugé à l'œil** : comparaison pixel des captures avant/après.
  Colonnes de luminance > 600/765 dans Favoris (x 483-499) et Logs (x 659-675)
  AVANT, aucune APRÈS ; l'historique est resté **identique au bit près**
  (`ImageChops.difference` → `None`), ce qui prouve que le refactor n'a rien
  changé pour le seul contrôle qui marchait déjà.
- **Tests** : `Tests/NativeScrollBarsTests.cs` (6, **226 au total**), éprouvés
  en les cassant (2 échecs). Ils ne peuvent pas vérifier une couleur — la zone
  non cliente n'est pas lisible — mais ils vérifient EXACTEMENT la cause du
  défaut : que chaque type de contrôle à ascenseur est bien passé au traitement.

**Page « Remerciements » du site (2026-08-10, suite de 104.0)** — demandée
après coup, aucun changement applicatif :
- `docs/remerciements.html` lit **le même `supporters.json`** que
  l'application. Une source de données, deux lecteurs.
- **Deux implémentations de la même règle de nettoyage, risque assumé et
  documenté des deux côtés.** L'alternative — du HTML pré-généré — obligerait
  à un build pour ajouter un nom, ce qui supprimerait tout l'intérêt du JSON.
- **La parité a été VÉRIFIÉE, et elle était fausse deux fois** : (a) le JS
  remplaçait les caractères de contrôle par un espace au lieu de les
  supprimer, d'où « Ali ce » là où le C# donne « Alice » ; (b) `\s` en
  JavaScript inclut U+FEFF, que `char.IsWhiteSpace` exclut — d'où
  `\p{White_Space}`. Les 12 vecteurs de `SupportersTests` sont désormais
  rejoués dans le navigateur avec zéro écart.
- `textContent` et jamais `innerHTML` pour les noms : ils viennent d'un fichier
  de données, rien ne justifie de les laisser produire du balisage.

**v1.34.0 (2026-08-10) — 104.0 : remerciements aux donateurs** :
- **Deux sources, choix de l'utilisateur** : `Config/Supporters.cs` (embarquée,
  socle hors ligne, vide pour l'instant) FUSIONNÉE avec `docs/supporters.json`
  servi par le site. Ajouter un nom au JSON et pousser suffit — pas besoin
  d'attendre une release ; recopier la liste dans `Supporters.Embedded` au
  prochain bump pour qu'elle reste affichable sans connexion.
- **Le tri fait partie de la consigne, pas de l'esthétique.** La demande dit
  « on ne mentionne pas la somme » : un ordre choisi à la main finirait par se
  lire comme un classement par montant. **Deux ordres tranchés par le
  mainteneur** : alphabétique (celui qui est appliqué par
  `SupportersProvider`) ou chronologique par date de don. Passer au second
  demanderait une date par entrée dans `supporters.json` — pas fait, personne
  n'ayant encore donné.
- **PSEUDONYME, jamais nom + prénom** — règle explicite du mainteneur, le
  pseudo GitHub de préférence. Un don PayPal transporte l'état civil du
  payeur. Écrit dans l'interface (« Pseudonymes publiés avec l'accord des
  personnes concernées ») ET dans les en-têtes de `Supporters.cs` et
  `supporters.json`, qui sont les endroits où quelqu'un ajoute un nom.
- **La liste est DANS L'APPLICATION**, `docs/supporters.json` n'en est que la
  source de données distante — point qui a prêté à confusion une fois. Le site
  n'a, lui, aucune page de remerciements.
- **C'est le SEUL texte de l'application qui vienne du réseau**, d'où
  l'assainissement : catégories Unicode Cc/Cf/Co supprimées (U+202E inverse le
  rendu de ce qui suit, les espaces de largeur nulle déjouent la
  déduplication), espaces repliés, 40 caractères par nom, 500 noms, 64 Ko de
  réponse, HTTPS exigé. Parcours par **rune** et non par `char` pour ne pas
  couper une paire de substituts — un pseudo peut contenir un emoji.
- **Les sauts de ligne valent un espace, ils ne sont pas supprimés** : les
  effacer souderait les mots (« Jean\nDupont » → « JeanDupont »). Ordre des
  tests dans `Clean` : espaces d'abord, catégories ensuite.
- **Aucune requête au démarrage** : le chargement n'a lieu qu'à l'ouverture de
  la fenêtre, et la liste embarquée s'affiche AVANT lui. Contacter un serveur
  à chaque lancement révélerait l'IP sans que l'utilisateur l'ait demandé.
- **Bouton dans `grpDonate` et non dans la barre du haut** : c'est au moment où
  quelqu'un envisage de donner qu'il a du sens de voir qui l'a déjà fait.
  Panneau de 144 à 168 px, garde de 10 px sous le dernier contrôle conservée.
- **Deux troncatures trouvées à la capture, invisibles autrement** : l'intitulé
  d'introduction coupait sa troisième ligne (56 → 72 px) et la mention de
  consentement passait SOUS le bouton Fermer (largeur 496 → 380, ancrage privé
  de `Right`). **Un Label ne signale pas qu'il tronque** — vérifié en FR et EN,
  clair et sombre, liste vide et liste remplie.
- **Tests** : `Tests/SupportersTests.cs` (20, **221 au total**), éprouvés en les
  cassant volontairement (3 échecs). Dont un qui relit `docs/supporters.json`
  du dépôt : c'est le seul lien entre le fichier du site et le code qui le
  consomme, et rien d'autre ne le vérifierait.

**105.0 (2026-08-10) — page Documentation** (site, README FR/EN : aucun
changement applicatif, pas de bump) :
- **Ce qui manquait n'était pas du contenu mais un POINT D'ENTRÉE.** La
  documentation existait, éclatée sur trois supports (site, wiki en 7 pages
  FR + 7 EN, deux README) sans page qui la rassemble. `docs/documentation.html`
  est ce hub, au style de `index.html` et non du thème Cayman des pages
  Markdown — donc bilingue par le même mécanisme et la même clé
  `localStorage("lang")`.
- **Trois tableaux de référence qui n'existaient nulle part** : les quatre
  réglages par enregistrement, les huit réglages de l'application avec leurs
  valeurs par défaut, et l'emplacement de CHAQUE fichier produit
  (enregistrements, logs JSONL, rapports de plantage, `settings.json`,
  `favorites.json`, `watchlist.json`, `trusted-binaries.json`,
  `installed-components.json`). Tout relevé dans le code, pas de mémoire.
- **Bascule FR/EN par attributs `data-i18n`, PAS une ligne de code par clé.**
  Ce n'est pas un goût : la méthode d'`index.html` a déjà produit un bug en
  production (« Légalité » affichait `undefined` sur tout le site anglais,
  clé absente du bloc `en`). Ici une clé manquante est signalée en console et
  le texte reste affiché. Les tableaux vivent dans une structure séparée, si
  bien qu'une divergence de lignes ou de colonnes entre FR et EN se voit.
- **Les liens vers le wiki sont réécrits selon la langue** (`data-wiki`) : ses
  pages anglaises sont des FICHIERS distincts (`Installation-EN`), sans
  bascule à la volée. Sans ça, un lecteur anglophone atterrissait en français.
  **Les 12 URL ont été vérifiées à 200**, pas supposées.
- **Vérifié dans un navigateur** : aucune clé manquante ni élément vide dans
  les deux langues, aucun débordement horizontal de la page (les tableaux
  défilent dans leur propre conteneur, contrôlé à 375 px), couleurs pilotées
  par les variables en clair comme en sombre.

**2026-08-08 — le deploiement Pages ne suit plus la pointe de main** (CI
uniquement, pas de bump) :
- **Suite de l'anomalie du 2026-08-04** : a la v1.21.0, le job de deploiement
  s'est termine en SUCCES en publiant le `latest.json` d'AVANT sa regeneration,
  alors qu'il demarrait 10 s apres le push du bot. Intermittent — les releases
  suivantes ont publie correctement, donc irreproductible a volonte.
- **Choix : supprimer la course plutot que de demontrer sa cause.**
  `update-checker.yml` expose desormais le SHA exact du commit qu'il vient de
  pousser (`git rev-parse HEAD` apres le push) et le transmet a
  `pages-build.yml` via un input `ref`. Le deploiement recupere CE commit, pas
  « la pointe de main ». **Ca fonctionne meme si l'hypothese de propagation est
  fausse** : un SHA fige ne laisse aucune fenetre, quelle que soit la cause.
- **Repli conserve** : `inputs.ref` vide (declenchement par `push` ou
  `workflow_dispatch`) retombe sur `main`. En GitHub Actions, `inputs.ref` vaut
  null hors `workflow_call` et se compare comme une chaine vide.
- **Trace de diagnostic ajoutee** : le job journalise le commit deploye et la
  version contenue dans `docs/latest.json`. Sans elle, « le site a servi du
  contenu perime » reste une impression ; avec elle, un signalement a GitHub
  serait etayable si le defaut se reproduisait **malgre** le SHA fige.
- **Ne pas signaler a GitHub en l'etat** : rien ne prouve un defaut de leur
  cote, et un rapport sur un incident unique sans logs serait clos comme
  invalide. Les logs de workflow expirent d'ailleurs a 90 jours.
- **2026-08-09 — LE CORRECTIF N'A PAS TENU, et cette note etait donc trop
  affirmative.** A la publication de v1.31.0, le site servait encore `1.30.0`
  alors que `docs/latest.json` valait bien `1.31.0` dans le depot. Le **test a
  deux fichiers a tranche** : `sentinelguard.html` contenait, lui, les ajouts du
  commit de la session — donc le deploiement a bien eu lieu, mais depuis l'arbre
  de `main` **au moment du tag**, c'est-a-dire AVANT le commit du bot. Le SHA
  fige n'a donc pas ete pris en compte sur ce chemin d'appel. Rattrape par
  `workflow_dispatch` de `pages-build.yml` (`ref: main`), site revenu a 1.31.0,
  verifie en ligne.
- **A faire quand le sujet reviendra** : verifier ce que `update-checker.yml`
  transmet reellement a `pages-build.yml` en `ref` lorsqu'il est appele DEPUIS
  `publish-release.yml` (chaine imbriquee sur deux niveaux), et non seulement
  lors d'un declenchement direct. Tant que ce n'est pas fait, **executer le test
  a deux fichiers apres chaque release** et rattraper au besoin — les
  utilisateurs de l'app ne sont de toute facon pas affectes, le verificateur de
  mise a jour lit l'API GitHub et non le site.

**106.0 / 107.0 (2026-08-09) — démarrage rapide et roadmap** (site, README et
wiki uniquement : aucun changement applicatif, pas de bump) :
- **107.0 reposait sur une prémisse fausse** : « ajouter une roadmap publique »
  — elle existe depuis 37.0. Le vrai problème était qu'elle avait vieilli au
  point d'induire en erreur (l'installateur y figurait comme « prévu » alors
  qu'il est livré depuis la v1.27.x, rien sur les quatre plateformes ni la
  surveillance). **Réflexe à garder** : vérifier ce qui existe avant de créer,
  un item du backlog peut avoir été traité par une autre voie.
- **La section « écarté » dit désormais POURQUOI** : Instagram mesuré
  infaisable, Authenticode bloqué sur un certificat payant, import des favoris
  retiré pour ne pas contourner une protection. Une roadmap qui explique ses
  refus vaut mieux qu'une liste de promesses — et évite qu'on redemande.
- **Deux défauts trouvés sur la PAGE D'ACCUEIL en écrivant 106.0** :
  - sa section Installation était **fausse** : elle ignorait `setup.exe`,
    présentait le runtime .NET comme nécessaire, et demandait de placer yt-dlp
    et ffmpeg dans un dossier `Tools\` qui n'existe plus. Elle envoyait donc
    tout visiteur vers le chemin le plus pénible, avec une instruction qui ne
    marche pas ;
  - le lien « Légalité » affichait **"undefined"** sur toute la version
    ANGLAISE : la clé `navLegal` manquait dans le bloc `en` alors que
    `setLanguage` la lit sans garde, et le bloc `fr` la définissait deux fois.
- **Vérifié dans un navigateur**, pas seulement relu : la page d'accueil rend
  et bascule FR/EN correctement, puis contrôlée en ligne après déploiement.

**109.0 / 110.0 / 111.0 (2026-08-09) — trois régressions signalées en usage
réel, toutes dans le rendu introduit par 39.0** :
- **109.0, colonnes de l'historique qui disparaissent.** `DrawItem` est levé
  pour la ligne ENTIÈRE, mais WinForms ne redessine que les cellules croisant
  la région invalidée. Remplir tout `e.Bounds` dans `DrawItem` effaçait donc
  des cellules que la passe n'allait pas repeindre : taille, durée et date
  s'évanouissaient, et un clic — qui invalide tout — les ramenait. **Chaque
  cellule peint désormais son propre fond dans `DrawSubItem`** ; `DrawItem` ne
  dessine plus rien en vue Details.
- **110.0, coins des combos qui scintillent au survol.** La première version
  repeignait PAR-DESSUS le rendu natif : correct à l'arrêt, mais chaque
  survol fait repeindre le contrôle dans son état « chaud », visible une
  fraction de seconde. `ThemedComboBox` traite maintenant `WM_PAINT` **à la
  place** du natif (BeginPaint/EndPaint, sans appeler `base`), et refuse
  `WM_ERASEBKGND`. Rien de natif ne subsiste, donc rien ne peut réapparaître.
- **111.0, boutons « rognés » dans le Guide de démarrage.** Cause réelle :
  `TutorialForm`, `DiagnosticForm` et `CrashReportForm` **n'appelaient jamais
  `ThemeManager.Apply`**. Leurs ThemedButton masquaient donc leurs coins
  arrondis avec `SystemColors.Control` sur un fond clair — quatre angles gris
  visibles. Deux correctifs : `ThemedButton.SurfaceColor` laissée vide se
  déduit désormais **du fond du parent** (un bouton non thématisé ne peut plus
  être laid), et le Guide comme le Diagnostic reçoivent le thème courant — ils
  s'affichaient d'ailleurs en clair au milieu d'une application sombre.
- **MÉTHODE, la leçon la plus réutilisable de la journée** : `DrawToBitmap`
  n'emprunte PAS `WM_PAINT` (il envoie `WM_PRINT`). Tout rendu qui passe par
  `WM_PAINT` est donc INVISIBLE à la capture habituelle. Ces trois défauts ont
  dû être vérifiés par `Graphics.CopyFromScreen` sur la fenêtre réelle,
  `TopMost` activé le temps du cliché — sans quoi une autre fenêtre se
  retrouve dans l'image.
- **Contrepartie assumée, mesurée en comparant deux captures d'écran** :
  `SetWindowTheme(..., "DarkMode_Explorer")`, qui donne les ascenseurs sombres,
  fait aussi dessiner les séparateurs de colonnes dans la zone vide sous les
  lignes. Conservé : une barre blanche vive se remarque bien plus qu'un filet
  gris, qui est le rendu même de l'Explorateur. "ItemsView" essayé, mêmes
  séparateurs.

**103.0 (2026-08-09) — icônes des plateformes prises en charge** :
- **Glyphes SIMPLIFIÉS, pas les logos officiels.** IconManager retint chaque
  icône à la couleur du thème, ce que les chartes de YouTube et Twitch
  interdisent explicitement sur leur logo ; et un pictogramme sobre indiquant
  la compatibilité relève de l'usage nominatif, admis. Chaturbate n'ayant pas
  de marque réductible à un pictogramme, une caméra générique le représente.
- **Deux emplacements** : une rangée à côté du champ d'URL (c'est là que se
  pose la question « qu'est-ce que je peux coller ici ? »), et une icône par
  ligne de la liste de surveillance.
- **`UI/PlatformStrip.cs` plutôt que quatre PictureBox** : ThemeManager encadre
  les PictureBox (elles comptent parmi les champs), ce qui aurait dessiné un
  cadre autour de chaque icône.
- **PIÈGE `ImageList`, trouvé par l'exécution** : `Images.Add` ne recopie
  l'image que si le handle de la liste EXISTE DÉJÀ. Une ImageList construite
  AVANT d'être posée sur la ListView garde la référence — libérer le bitmap
  (`using`) fait donc planter la réalisation du handle avec
  « ArgumentException : Parameter is not valid », très loin de sa cause. Le
  commentaire de la v1.29.0 (« on libère la nôtre aussitôt après l'ajout »)
  n'était vrai que parce que la liste y était déjà réalisée.
- **Deux défauts corrigés au passage, vus seulement à la capture** :
  - la dernière colonne s'étirait AVANT que la ListView ait décidé d'afficher
    un ascenseur vertical, d'où une colonne trop large de ~17 px et un
    ascenseur HORIZONTAL parasite. `ThemedListView.Refresh` diffère donc par
    `BeginInvoke` ;
  - les ascenseurs d'une ListView restaient BLANCS en thème sombre : ils sont
    dessinés par Windows en zone non cliente, hors de portée de `BackColor` et
    de tout dessin par l'application. Corrigé par
    `SetWindowTheme(handle, "DarkMode_Explorer")`, le thème étant déduit de la
    clarté du fond pour ne pas basculer d'un coup pendant le fondu clair/sombre.
- **Méthode de capture** : `Application.DoEvents()` avant le `DrawToBitmap`,
  sans quoi la capture montre un état transitoire — les réajustements différés
  par `BeginInvoke` n'ont pas encore eu lieu. C'est ce qui a d'abord fait
  croire que l'ascenseur horizontal subsistait.

**v1.32.0 (2026-08-09) — 40.0 : Twitch, YouTube et TikTok** :
- **Tout a été MESURÉ sur le vrai yt-dlp avant d'écrire une ligne d'interface**,
  et c'est la mesure qui a dicté la conception. Les quatre découvertes :
  - Twitch et TikTok hors ligne disent « The channel is not currently live ».
    Sans ce marqueur, `RoomStatusChecker` retombait en `Unknown` sur ces deux
    plateformes : la surveillance n'aurait **jamais** rien déclenché, en
    silence. Mode de panne identique à celui de la v1.26.1.
  - **Twitch distingue un compte inexistant** (« does not exist ») d'un compte
    hors ligne, là où Chaturbate rend les deux indiscernables. D'où
    `RoomStatus.NotFound` : une faute de frappe n'est plus attendue
    indéfiniment (défaut que la v1.25.0 avait dû documenter faute de pouvoir le
    corriger).
  - **YouTube rend le code de sortie 0 MÊME sur une vidéo ordinaire.** S'en
    tenir au code de retour aurait fait « surveiller » une VOD et lancé un
    enregistrement immédiat. D'où la lecture de `live_status`, qui fiabilise au
    passage le chemin Chaturbate. `NA` (champ non renseigné) retombe sur le
    code de sortie, pour ne rien changer au comportement existant.
  - Le premier segment d'URL vaut `watch` sur `youtube.com/watch?v=ID` : tous
    les enregistrements YouTube auraient porté le même nom de fichier. D'où
    `Services/StreamPlatform.cs`, qui sait tirer un nom de chaque forme d'URL
    et le nettoie de ce qui est interdit dans un chemin.
- **Piège trouvé avant l'UI** : le bac à sable d'URL refusait `@` dans un
  segment, donc `youtube.com/@chaîne` et `tiktok.com/@compte`. Corrigé dans
  SentinelGuard (1.2.0, non publié) : le `@` dangereux est celui de
  l'AUTORITÉ (`user:pass@hôte`), qui ne peut pas se trouver dans un segment et
  que `Uri.UserInfo` refuse déjà un cran plus haut. Un test le prouve.
- **`AppConfig.Whitelist` est DÉRIVÉE de `Platforms.AllowedDomains`** : deux
  listes auraient fini par diverger, et une plateforme ajoutée à l'une mais pas
  à l'autre donne des URLs refusées sans raison compréhensible. Un test vérifie
  que chaque plateforme reconnue passe le bac à sable.
- **INSTAGRAM DEMANDÉ MAIS ÉCARTÉ, mesuré infaisable** : sans session
  authentifiée il redirige vers `/accounts/login/` et yt-dlp répond
  « Unsupported URL ». Écrire l'UI, les traductions et les tests avant d'avoir
  vérifié le contact réel est exactement l'erreur de 92.0 (quatre versions
  perdues). **Ce qu'il faudrait pour reprendre** : un `cookies.txt` couvrant
  instagram.com ET un direct en cours au moment du test — un lien seul ne
  suffit pas.
- **Le NOM du logiciel ne change pas** (décision du mainteneur). Seuls les
  libellés se neutralisent : « URL du live », colonne « Source ».
- **Note de légalité généralisée** dans l'app ET le wiki (FR/EN) : sur les
  plateformes non adultes l'enjeu est le droit d'auteur et non le consentement,
  et enfreindre des conditions d'utilisation reste contractuel, pas pénal.
- **Wiki mis à jour au passage** : la page Installation ne mentionnait NULLE
  PART `setup.exe` et affirmait qu'il fallait fournir yt-dlp/ffmpeg soi-même —
  faux depuis l'installateur. C'est la première page que lit un nouveau venu.
  Aperçu de l'interface ajouté (FR/EN), données factices.
- **Tests** : `Tests/PlatformsTests.cs` (20) + 9 cas de classification bâtis sur
  les sorties réelles, **201 au total**.

**Fusion Security/ ↔ SentinelGuard (2026-08-09, suite immédiate de 36.0)** — pas
de bump applicatif, rien de visible ne change :
- **La duplication de 30.0 avait DÉJÀ coûté un correctif, c'est ce qui a décidé
  la fusion.** `20e00ef` (2026-08-03, 12h36) ajoute `ComputeSha256` à
  `Security/BinaryVerifier.cs` pour l'issue #16 (utilisateur bloqué par un hash
  yt-dlp périmé). `b5c9707` crée le package **sept heures plus tard**, à partir
  d'un état antérieur : la méthode n'y a jamais figuré, et personne ne l'a vu
  pendant six jours — les deux compilaient, les deux suites de tests passaient.
- **Constat avant d'agir** : hors `ComputeSha256`, les six validateurs ne
  différaient QUE par les surcharges `out string? reason`, les commentaires XML
  et le namespace. Vérifié fichier par fichier (`git diff --no-index`), pas
  supposé. La fusion était donc sans risque de régression fonctionnelle.
- **`Security/` supprimé** (6 fichiers), `ComputeSha256` ajouté au package,
  `VerifyFileHash` réécrite par-dessus. Le motif de refus nomme désormais les
  DEUX empreintes : sans elles, l'appelant doit recalculer à la main pour savoir
  s'il tient une mise à jour légitime ou un fichier remplacé.
- **Les 22 sites d'appel journalisent maintenant le motif eux-mêmes** : les
  validateurs du package ne journalisent rien (c'est leur règle). Sans ce
  travail, tous les refus seraient devenus muets — le pire endroit étant
  `Program.Main`, où l'application s'arrête avant même d'ouvrir sa fenêtre.
- **Gain non anticipé** : l'application n'avait AUCUN motif de refus dans ses
  logs (ses copies journalisaient un message figé). Elle a maintenant la cause
  exacte, ce qui compte pour une app qui produit des rapports de bug pré-remplis.
- **Tests** : les 4 fichiers dupliqués de `Tests/` sont supprimés (54 cas), leur
  couverture existait déjà côté package. Seuls les deux tests de `ComputeSha256`
  étaient propres à l'app : portés. **172 côté app, 85 par cible côté package.**
- **Vérifié en exécution réelle** : les validateurs sont sur le chemin de
  démarrage (`WorkingDirectoryValidator` dans `Program.Main`, `PathValidator`
  dans le constructeur de `MainForm`) — une régression aurait empêché l'app de
  s'ouvrir. Lancée pour de vrai : démarre, tourne, aucun rapport de crash, et
  l'avertissement d'ACL permissive prouve que les validateurs du package
  s'exécutent.
- **Motifs de refus traduits en anglais (2026-08-09, avant publication)** :
  ils étaient en français dans un package dont tout le reste (README, docs XML,
  description nuget.org) est anglais. **Règle qui en découle** : dans
  `SentinelGuard/`, les commentaires `//` restent en français comme partout dans
  le dépôt, mais tout ce qui SORT du package — docs `///` (elles partent dans le
  .xml d'IntelliSense) et chaînes `reason`/`details` — s'écrit en anglais.
- **Effet de bord assumé côté application** : ses logs, français, contiennent
  désormais des motifs anglais (« Chemin de log refusé : Path is empty. »). Sans
  gravité — la sortie brute de yt-dlp y est déjà en anglais — et c'était le prix
  d'un seul exemplaire du code.

**36.0 traité (2026-08-09) — SentinelGuard 1.1.0, et l'application le CONSOMME**
(pas de bump applicatif : aucun changement visible, ça part avec la v1.31.0 non
encore publiée) :
- **La consigne d'origine était « déplacer `Logger` et `DownloadEngine` dans
  SentinelGuard ». Elle a été discutée avec l'utilisateur avant d'écrire** : le
  package annonce « pure functions, no side effects, nothing is logged », et
  `DownloadEngine` est un pilote yt-dlp. L'y mettre rendait le README faux et
  remettait un composant d'enregistrement de cams dans le package justement
  renommé pour ne pas y ressembler. **Choix retenu par l'utilisateur** : extraire
  ce qui est réellement générique, pas les classes entières.
- **Ce qui est parti** : `SentinelGuard.GuardedProcessRunner` (lancement d'un
  binaire, capture ligne à ligne, watchdog d'inactivité, arrêt de l'ARBRE de
  processus, états Completed/Failed/Stopped) et `SentinelGuard.LogFileRotator`.
  C'est le pendant runtime de `BinaryVerifier` : vérifier l'empreinte ne couvre
  que l'instant d'avant le lancement.
- **Ce qui reste dans l'app, volontairement** : les arguments yt-dlp, la regex de
  progression, le fichier de log du job, et `Logger` (qui dépend d'`AppConfig`).
  Sans valeur pour un tiers.
- **L'app référence désormais le projet SentinelGuard** (`ProjectReference`, pas
  `PackageReference` : la version publiée est toujours en retard d'un correctif,
  et dépendre de son propre paquet obligerait à publier avant de pouvoir
  corriger). `Services/LogRotationManager.cs` est **supprimé**, ses deux
  appelants pointent sur `LogFileRotator`.
- **Conséquence sur la distribution, vérifiée en publiant et non supposée** : le
  ZIP standard gagne `SentinelGuard.dll`, et le portable — pourtant en fichier
  unique — laisse `SentinelGuard.pdb` À CÔTÉ de l'exe. Ajouté à
  `[UninstallDelete]` de l'installateur, sinon il survivait à la désinstallation
  et bloquait `dirifempty`, exactement le piège déjà payé avec le `.pdb` de
  l'application. Le `.xml` de documentation (33 Ko d'IntelliSense destinée aux
  consommateurs du package) est exclu de la sortie via
  `AllowedReferenceRelatedFileExtensions=.pdb`.
- **Le workflow de release n'a rien demandé** : il zippe `publish/standard/*` en
  entier, il n'y a pas de liste de fichiers à tenir à jour. L'installateur non
  plus (il fait `Expand-Archive` de toute l'archive).
- **Correction apportée au passage** : `Exited` peut se lever AVANT que les
  dernières lignes de sortie aient été remises aux gestionnaires asynchrones.
  `GuardedProcessRunner` appelle donc `WaitForExit()` dans `Exited` — la façon
  documentée d'attendre ce vidage. L'ancien code perdait par intermittence la
  fin de la sortie, c'est-à-dire souvent le message d'erreur qui explique
  l'échec.
- **Tests** : `SentinelGuard.Tests/GuardedProcessRunnerTests.cs` (11) +
  `LogFileRotatorTests.cs` (8) — **82 par cible**, sur les deux TFM. Ils lancent
  de VRAIS `cmd.exe` : un superviseur de processus simulé ne ferait que rejouer
  ce qu'on croit déjà savoir du comportement de Windows. Le watchdog échantillonne
  à `timeout/4` (borné 250 ms – 10 s), sans quoi un seuil court ne serait
  constaté qu'au bout de 10 s et le test durerait autant.
- **Publication du package : NON FAITE.** Elle passe par un tag dédié
  `sentinelguard-v1.1.0`, distinct des tags applicatifs — à demander à
  l'utilisateur, comme tout tag.

**v1.31.0 (2026-08-09) — 39.0 : refonte visuelle de la fenêtre principale** :
- **Périmètre fixé avec l'utilisateur avant d'écrire une ligne** : « toute la
  fenêtre principale », et le défaut à corriger est le **rendu plat/daté** — pas
  la disposition, pas la lisibilité. Les positions et tailles des contrôles sont
  donc restées telles quelles ; **ne pas les remanier sans nouvelle demande**.
- **Le diagnostic est venu d'une capture, pas d'une intuition.** Cinq causes
  identifiées à l'écran : 18 boutons bleus de poids égal, des cartes de la même
  couleur que le fond (donc invisibles autrement que par un liseré de 1 px),
  un titre qui coupe la bordure façon GroupBox de Windows 95, un seul niveau
  typographique, et des contrôles Win32 bruts (en-têtes de ListView clairs en
  thème sombre, bordures blanches).
- **Décision structurante, tranchée par l'utilisateur** : un seul bouton
  d'accent par zone. `UI/ThemedButton.cs` + `ButtonRole` (Primary / Secondary /
  Danger). Le rôle vit sur le contrôle, la couleur est dérivée par
  `ThemeManager.ResolveButtonColors` — aucun formulaire ne code de couleur.
- **Danger ne remplit PAS le bouton de rouge** : « Stop », « Tout arrêter »,
  « Supprimer favori » et « Ne plus surveiller » sont visibles en même temps ;
  quatre aplats rouges donneraient une fenêtre en alerte permanente. Rouge sur
  le texte, la bordure et la teinte de survol seulement.
- **Trois contrôles dessinés par l'application** (même veine que
  `ThemedProgressBar`) : `ThemedButton`, `ThemedComboBox`, `ThemedListView`
  (statique, s'attache à une ListView existante), plus `InputFrame` qui fait
  encadrer les champs par leur PARENT — une ListView/ListBox/TextBox ne sait pas
  porter une bordure de couleur choisie, la sienne vient du système.
- **`Palette` passe de 7 à 11 champs** : `Bg` / `Card` / `Input` séparés, plus
  `FgMuted`, `Accent`, `AccentFg`, `Neutral`, `Danger`. La récursion de
  `ApplyPalette` transporte la **surface courante**, ce qui permet à un panneau
  imbriqué de prendre la couleur de la carte qui le porte.
- **Trois pièges rencontrés, tous invisibles à la compilation** :
  - **`ImageList.Images[i]` CONSTRUIT un Bitmap à chaque appel.** Utilisé dans
    la boucle de dessin des miniatures, il épuisait les handles GDI en quelques
    secondes, et le symptôme ne ressemblait en rien à la cause :
    `ArgumentNullException('dc')` dans `Control.WmPaint` d'un tout autre
    contrôle, parce que `BeginPaint` finissait par rendre un DC nul. **Utiliser
    `ImageList.Draw`.** Ne se manifestait qu'avec de vraies miniatures — d'où
    des captures qui passaient et un plantage au démarrage réel.
  - **`DrawToBitmap` envoie `WM_PRINT`, pas `WM_PAINT`.** Un contrôle natif
    repeint après coup (`ThemedComboBox`) doit traiter les deux, sinon la
    capture montre le rendu SYSTÈME et laisse croire que le correctif ne
    fonctionne pas. Bordure blanche « persistante » diagnostiquée en peignant la
    nôtre en rouge : la native se dessine 1 px À L'INTÉRIEUR, il faut couvrir
    l'anneau avant de tracer.
  - **`DrawListViewItemEventArgs.State` n'est pas fiable en vue Details** :
    toutes les lignes se dessinaient sélectionnées. Lire `e.Item.Selected`.
- **La zone d'en-tête à droite de la dernière colonne n'est traversée par aucun
  évènement de dessin** — le contrôle natif la remplit en clair, d'où un bloc
  blanc en thème sombre. Corrigé en étirant la dernière colonne
  (`ThemedListView.StretchLastColumn`, rappelé après remplissage : c'est
  l'ascenseur qui change la largeur utile, sans lever `Resize`).
- **Défaut d'accessibilité PRÉEXISTANT trouvé par les tests, pas par l'œil** :
  le blanc sur le bleu d'accent ne donne que 3,2 de contraste en thème sombre
  (seuil WCAG : 4,5). Corrigé comme Windows 11 le fait — texte **sombre** sur
  l'accent en thème sombre — et survol/appui s'éloignent désormais de la couleur
  du texte au lieu d'éclaircir systématiquement. Le rouge « danger » du thème
  sombre a dû monter dans les tons saumon (#FFA79A) pour la même raison : sur le
  gris des boutons secondaires, un rouge franc plafonne à 3,5.
- **Effet de bord bénéfique** : `ThemeManager` ne force plus la police des
  cartes en gras — elle était héritée par TOUS leurs enfants, ce qui expliquait
  l'aspect lourd de l'écran.
- **Piège de parentage introduit puis refermé** : `BuildJobRow` appliquait le
  thème à une ligne pas encore ajoutée à son panneau ; avec des surfaces
  distinctes, elle prenait le fond de la FENÊTRE et formait un rectangle plus
  sombre au milieu de la carte. La ligne est désormais parentée dans
  `BuildJobRow`, avant `ThemeManager.Apply`.
- **Tests** : `Tests/ThemeHierarchyTests.cs` (23, **226 au total**), éprouvés en
  les cassant volontairement (7 échecs). Ils ne vérifient pas « c'est joli »
  mais ce qu'une retouche de palette peut casser en silence : contraste WCAG de
  chaque rôle dans ses trois états, accent distinct du neutre, carte distincte
  du fond, survol distinct du repos.
- **Vérification de rendu** : `DrawToBitmap` sur `contentPanel` en clair et
  sombre, plus `SettingsForm`. **Piège de méthode à retenir** : la première
  capture montrait le mode SIMPLE alors qu'elle était nommée « avancé » — une
  exécution précédente avait persisté `AdvancedMode = false` dans
  `settings.json`. Forcer `ApplyUiMode(true)` au début de la capture.

**v1.30.0 (2026-08-08) — 29.0 / 2.2 : Safe Mode** :
- **Principe qui gouverne tout** : aucune de ces defaillances ne justifie de
  refuser de demarrer. ffmpeg absent interdit le reencodage et les miniatures,
  PAS la capture ; un cookies.txt illisible interdit le contenu reserve, PAS le
  flux public. Ce qui produisait un echec obscur au moment d'enregistrer — voire
  TOTALEMENT silencieux pour le cookies.txt du 2026-08-08 — devient un message
  clair au demarrage.
- **Deux origines, un seul etat** : manuel (cases des Parametres, persistees par
  NOM et non par index, pour qu'ajouter un composant a l'enum ne decale pas les
  reglages existants) et automatique (controle rate au demarrage). **Le manuel
  prime a l'affichage** : dire a quelqu'un que l'application a desactive ce
  qu'il a decoche lui-meme serait faux et le ferait chercher un bug.
- **Reactiver a la main efface l'automatique** : sans ca, quelqu'un qui vient de
  reparer son ffmpeg cocherait la case sans effet. Controle refait au demarrage.
- **Cases « coche = actif »** et non « desactiver X » : une negation obligerait a
  raisonner a l'envers pour lire l'etat courant.
- **Un seul message au demarrage**, et RIEN quand tout va bien — une boite
  « tout va bien » a chaque lancement serait fermee sans etre lue.
- **Le controle du cookies.txt de la v1.26.1 ne servait qu'a la selection** : un
  fichier devenu invalide apres coup repassait inapercu. Il tourne desormais
  aussi au demarrage.
- **Tests** : `Tests/SafeModeTests.cs` (8, **203 au total**). `SafeMode` etant un
  etat global statique, chaque test le remet a zero — sans quoi l'ordre
  d'execution changerait les resultats.

**v1.29.0 (2026-08-08) — 29.0 / 4.1 : historique enrichi** :
- **Les miniatures existaient depuis la 1.3.0 et n'etaient affichees NULLE
  PART.** ffmpeg en generait une a la fin de chaque enregistrement, deposee en
  `.jpg` a cote de la video — du travail paye a chaque capture pour rien pendant
  26 versions. Elles apparaissent enfin dans l'historique.
- **Piege WinForms** : en vue `Details`, la hauteur de ligne suit celle du
  `SmallImageList`. Une vignette 48x27 fait donc passer les lignes de ~17 a
  27 px — d'ou `grpHistory` de 130 a 170, sans quoi la liste ne montrait plus
  que trois entrees. Les panneaux suivants se repositionnent seuls (chaine
  `Bottom + sectionGap`).
- **`Image.FromFile` verrouille le fichier** tant que l'image vit : la video
  n'aurait plus pu etre supprimee ni deplacee depuis l'explorateur. D'ou lecture
  via `FileStream` puis copie dans un `Bitmap`.
- **Decodage sur le thread de fond** : ouvrir et redimensionner 50 JPEG sur le
  thread UI figerait la fenetre a chaque rafraichissement. `ImageList` recopiant
  l'image dans son propre handle, la notre est liberee aussitot apres l'ajout.
- **Defaut PREEXISTANT revele par la capture** : les colonnes totalisaient 490 px
  pour une liste de 470, ce qui coupait la colonne Date et affichait un ascenseur
  horizontal. Jamais signale, jamais vu — parce que personne ne regardait ce
  panneau de pres. Reajuste a 445 px au total (185/70/60/130) et annee sur deux
  chiffres. **Toute modification de ces largeurs doit garder le total sous ~450.**
- **Quatre cycles de capture** ont ete necessaires pour equilibrer ces colonnes :
  chaque essai deplacait la troncature ailleurs. Mesurer plutot que d'estimer
  aurait ete plus rapide (~8,5 px par caractere a Segoe UI 9pt).

**v1.28.1 (2026-08-08) — DEUX defauts que j'avais INTRODUITS en v1.28.0, et
que seule l'execution reelle a montres** :
- **`Copy-Item -LiteralPath 'dossier\*'` ne copie RIEN et ne leve AUCUNE
  erreur.** `-LiteralPath` desactive l'interpretation du joker, qui devient un
  nom de fichier litteral inexistant. En « ameliorant » le script j'avais
  remplace `-Path` par `-LiteralPath` — creant exactement l'echec silencieux que
  la v1.28.0 pretendait supprimer, double d'un « Mise a jour appliquee » mensonger.
  Mesure isolement pour en avoir le coeur net : 0 fichier copie, 0 erreur.
  Corrige par une copie element par element **avec compteur** : une copie vide
  devient une erreur bruyante.
- **La fiche de desinstallation recevait `Assembly.GetName().Version`**,
  c'est-a-dire la version EN COURS D'EXECUTION — donc l'ancienne. Elle ecrivait
  1.27.5 par-dessus 1.27.5 en annoncant une mise a jour. Corrige en passant
  l'`UpdateInfo` entier a `DownloadAndInstallAsync` au lieu de valeurs
  separees : regrouper ce qui va ensemble supprime la possibilite de les melanger.
- **Protocole de test, reutilisable** : construire le source courant avec une
  version INFERIEURE a la derniere publiee (`-p:Version=1.27.5`), le poser dans
  une vraie installation, et declencher la mise a jour par un `Shown` temporaire
  — avec un **fichier temoin**, sans quoi la relance post-mise-a-jour rejoue le
  declencheur en boucle (constate : 16 executions).
- **Resultat final verifie** : exe 1.27.5 -> 1.28.0, registre 1.27.5 -> 1.28.0.
- **Lecon, la troisieme de la journee** : les tests unitaires et la compilation
  n'ont rien vu. Ni pour le plantage au demarrage, ni pour le mode silencieux de
  l'installateur, ni ici. **Ce qui touche au systeme de fichiers, aux processus
  ou au registre doit etre EXECUTE pour etre cru.**

**v1.28.0 (2026-08-08) — la mise a jour interne n'avait JAMAIS ete exercee** :
- **Trouve par relecture, pas par execution** : ce code date de la 1.2.0 et rien
  ne l'avait jamais fait tourner. Avant de le modifier pour la coherence avec
  l'installateur, une lecture attentive a revele deux defauts plus graves que
  celui qu'on venait corriger.
- **Le ZIP etait telecharge et execute SANS aucun controle d'integrite**, dans
  une application qui verifie obsessionnellement le hash de yt-dlp et de ffmpeg.
  **Verifie sur l'API avant de coder** : GitHub expose desormais `digest` par
  fichier de release (`sha256:042bb7ff...`), et la valeur correspondait
  exactement a celle calculee en local. `UpdateChecker` la transporte,
  `UpdateInstaller` la controle **avant d'extraire**.
- **Echec silencieux** : `Wait-Process -Timeout 15` puis copie sur un exe encore
  verrouille -> `Copy-Item` echoue -> le script relancait **l'ancienne version
  sans rien signaler**. Delai porte a 120 s (arreter des enregistrements prend
  du temps) et tout echec ecrit dans `update.log`.
- **Coherence avec l'installateur** (le defaut de depart) : `DisplayVersion` de
  la fiche de desinstallation est mise a jour, uniquement si `unins000.exe`
  existe a cote de l'exe — marqueur simple et fiable d'une installation par
  l'installateur.
- **Ecarte : faire telecharger le nouveau setup.exe par la mise a jour.** Plus
  elegant en apparence (un seul mecanisme), mais chaque mise a jour
  re-telechargerait **150 Mo** — yt-dlp et ffmpeg compris, deja presents et deja
  verifies — pour corriger une incoherence cosmetique. Le critere est ce que vit
  l'utilisateur, pas l'elegance de l'architecture.
- **Empreinte absente = on installe quand meme**, avec une ligne de log. Refuser
  rendrait toutes les releases anterieures au champ `digest` impossibles a
  installer, ce qui serait pire que le risque evite.
- **Tests** : `Tests/UpdateDigestTests.cs` (8, **195 au total**), batis sur la
  forme reellement relevee sur l'API. Eprouves en les cassant (4 echecs).

**23.0 traite (2026-08-08) — installateur** (CI + nouveau dossier `installer/`,
pas de bump : rien ne change dans l'application elle-meme) :
- **Motif** : plusieurs testeurs reels (proches du mainteneur) ont abandonne a
  l'installation. Ce n'est pas « copier deux fichiers » qui rebute, c'est devoir
  aller les chercher, choisir la bonne variante et savoir ou les poser.
- **Quatre decisions, toutes verifiees avant d'ecrire** :
  - **aucune charge utile embarquee** : le setup fait **1,95 Mo** et telecharge
    tout. Embarquer le build autonome donnait 34 Mo pour rien — une connexion
    etait de toute facon requise pour yt-dlp et ffmpeg ;
  - **la variante autonome est telechargee dans les DEUX modes**, donc **.NET
    n'est jamais un prerequis**. Le mainteneur avait demande que l'installateur
    installe .NET ; ne pas en avoir besoin est strictement superieur (pas d'UAC,
    pas de 55 Mo, aucune modification hors du dossier choisi) ;
  - **deux modes proposes des le premier ecran**, comme 7-Zip. En portable,
    `Uninstallable=IsInstallMode` supprime desinstalleur et entree de registre ;
  - **installation par utilisateur** dans `%LOCALAPPDATA%\Programs` : verifie
    AVANT d'ecrire que `WorkingDirectoryValidator` ne refuse pas cet
    emplacement, et l'application y ecrit ses JSON sans elevation.
- **`trusted-binaries.json` ecrit par l'installateur** : sans lui, un
  avertissement de securite s'afficherait au premier enregistrement, l'app
  comparant a un hash fige forcement perime face a un yt-dlp « derniere
  version ». L'installateur verifie contre les sommes publiees par les auteurs,
  puis inscrit le hash comme approuve — la propriete de securite est preservee.
- **PIEGE TROUVE PAR LE TEST, invisible a la compilation** : la logique de
  telechargement etait dans `NextButtonClick`, **jamais appele en execution
  silencieuse** (les pages ne s'affichent pas). Un `/VERYSILENT` se serait
  termine en « succes » sur un dossier vide. Deplacee dans `PrepareToInstall`,
  qui s'execute dans les deux modes. **Compiler ne prouve rien : il faut
  installer pour de vrai.**
- **Piege licence evite par construction** : un publish local embarque
  `ffmpeg.exe` (231 Mo) car le csproj copie `Tools\` quand il existe. Un
  `Source: "*"` dans le `[Files]` aurait redistribue ffmpeg sous GPL. Les
  fichiers sont donc listes explicitement, jamais par joker.
- **Verifie de bout en bout le 2026-08-08**, pas seulement compile :
  telechargements, verifications, extraction, ecriture du fichier de confiance
  et **demarrage de l'application installee**.
- **Reste non signe** : SmartScreen affichera « Editeur inconnu », meme blocage
  que 1.1 (certificat payant).
- **Complete le 2026-08-08 apres un cycle installation/desinstallation reel** :
  - **`installed-components.json`** ecrit par l'installateur : versions reelles
    (interrogees aupres des executables), empreintes verifiees et **licences**
    de yt-dlp et ffmpeg. Motif : le SBOM de la release ne couvre que les
    dependances NuGet, donc **le GPL de ffmpeg n'y apparait nulle part** alors
    qu'il represente l'essentiel du poids installe. Un inventaire qui omet ca
    donne une image fausse, precisement au public qui lit ce genre de fichier.
  - **Les logs de LocalAppData sont desormais supprimes a la desinstallation**
    (`filesandordirs`). Depuis la v1.27.0 ils vivent hors du dossier
    d'installation, donc ils survivaient. Les ENREGISTREMENTS ne sont jamais
    touches.
  - **PIEGE INNO a retenir** : les fichiers extraits d'un ZIP a l'installation
    ne sont **pas suivis** par Inno — il ignore leur existence et ne les
    supprimera jamais seul. Tout le contenu de l'archive doit figurer nommement
    dans `[UninstallDelete]`. Le `.pdb` oublie restait seul et empechait meme
    `dirifempty` de nettoyer le dossier.
  - **Seconde propriete d'Inno** : les regles de desinstallation sont **figees
    dans le desinstalleur au moment de l'installation**. Corriger
    `[UninstallDelete]` ne change RIEN pour les installations existantes — il
    faut reinstaller pour tester, et les utilisateurs deja installes gardent
    l'ancien comportement jusqu'a leur prochaine installation.


**v1.27.0 (2026-08-08) — l'application ne demarrait pas ailleurs que chez le
mainteneur** :
- **Signale par un utilisateur** qui avait pourtant installe les dependances et
  place yt-dlp/ffmpeg au bon endroit : « erreur fatale » avant meme l'affichage
  de la fenetre, avec `DirectoryNotFoundException` sur `E:\Streamlinkideos`.
- **Cause** : `AppConfig.CaptureDir` et `LogDir` valaient les chemins du poste du
  mainteneur, **codes en dur**. Sur une machine sans disque E:, le premier
  `Directory.CreateDirectory` du demarrage levait, et rien ne rattrapait.
  **Aggravant** : le rapport de crash s'ecrit dans `LogDir\crashes`, donc lui
  non plus ne pouvait pas s'ecrire — l'utilisateur voyait « le rapport detaille
  n'a pas pu etre enregistre » et repartait sans aucune piste.
- **Note de CLAUDE.md a corriger, elle etait trompeuse** : elle presentait ces
  deux chemins comme « les seuls chemins absolus legitimes du depot ». Ils
  n'etaient pas legitimes, ils etaient un bug latent depuis l'origine, invisible
  parce que le seul testeur avait un disque E:.
- **Correctif en trois niveaux** : `Vidéos\Chaturbate Recorder` par defaut,
  `LocalAppData\ChaturbateRecorder\logs` pour les logs (toujours present et
  inscriptible), et `Services/DirectoryResolver.cs` qui replie au lieu de lever
  — dossier demande, puis defaut, puis dossier de l'application en dernier
  recours. Un reglage persiste devenu injoignable est corrige et l'utilisateur
  prevenu, au lieu d'un plantage a chaque lancement.
- **`CrashReporter.CrashDir` passe de champ `static readonly` a propriete** :
  fige a l'initialisation du type, il aurait garde le chemin qui vient
  precisement d'echouer.
- **Regle generale** : une valeur par defaut n'est pas une preference, c'est le
  premier contact d'un inconnu avec le logiciel. Aucun chemin absolu specifique
  a une machine ne doit y figurer.
- **Tests** : `Tests/DirectoryResolverTests.cs` (7, **187 au total**), dont un
  qui reproduit le plantage exact et un qui echouerait si quelqu'un remettait un
  jour `E:\` en dur. Eprouves en les cassant volontairement (5 echecs).

**86.0 traite (2026-08-05) — SBOM CycloneDX** (CI uniquement, pas de bump ni
d'entree de changelog, meme regle que 34.0/38.0) :
- **Job `sbom` dans `security-scan.yml`** (artefact, 90 jours) **et SBOM attache
  a chaque release** dans `publish-release.yml`. C'est sur la release qu'il a de
  la valeur : une liste de dependances verifiable, rattachee a un binaire precis.
- **Outil verifie en local AVANT d'ecrire le workflow** : `CycloneDX` 6.2.0
  produit du CycloneDX **1.7**. Prefere a un scan de repertoire parce qu'il lit
  le graphe NuGet reel apres restore, transitives comprises.
- **Piege de syntaxe** : l'option `-j` n'existe PAS en v6 — c'est le nom de
  fichier qui choisit le format (`bom.json` -> JSON). Trouve en une minute en
  local, aurait coute un run rouge en CI.
- **CORRIGE le 2026-08-08 — le job `sbom` a echoue au premier passage**, pour
  deux raisons cumulees, et la lecon depasse ce workflow :
  - **le chemin ecrit en toutes lettres a ete corrompu** : `	ools\` est devenu
    une **TABULATION** dans le YAML (`.dotnet<TAB>ools\`), parce que le fichier a
    ete ecrit par un script Python. C'est la **troisieme** fois de la session que
    `	` se transforme en tabulation en passant par un script (voir aussi
    v1.23.1 et v1.26.0). **Regle** : toute chaine contenant un antislash suivi
    d'une lettre s'ecrit avec l'outil d'edition direct, jamais par script.
  - **installation et utilisation etaient dans deux etapes differentes** :
    `dotnet tool install --global` complete le PATH du **processus courant**, et
    chaque etape repart d'un processus neuf.
  - **Correctif** : une seule etape, et le dossier construit par `Join-Path`
    plutot qu'ecrit en dur — aucun antislash litteral, donc le piege ne peut
    plus se poser. Commande verifiee en local sur les DEUX projets avant
    re-poussage.
- **A savoir pour la prochaine release** : le tag v1.26.1, pousse avant ce
  commit, produira une release SANS SBOM — un workflow s'execute depuis l'arbre
  du tag. Le premier SBOM attache sera celui de la version suivante.

**v1.26.1 (2026-08-05) — le cookies.txt qui faisait tout echouer en silence** :
- **Symptome rapporte** : « enregistrer un salon hors ligne affiche echec ».
  Le vrai defaut etait tout autre, visible seulement dans les logs de la
  capture : `ERROR: invalid Netscape format cookies file`. **Tous** les
  enregistrements echouaient, en ligne comme hors ligne.
- **Cause exacte, apres trois hypotheses ecartees** : BOM (non), CRLF (non —
  une copie convertie en LF echouait pareil), colonnes (non — 7 partout). Les
  lignes commencaient par `HttpOnly_` **sans le diese**. Python ne reconnait
  alors pas le prefixe, ne saute pas la ligne comme commentaire, et bute sur
  l'assertion domaine/indicateur de `http.cookiejar`. **5 cookies sur 6
  invalides, dont `sessionid`.**
- **Deux comportements de yt-dlp a connaitre** : il **ignore silencieusement**
  un fichier de cookies **absent**, mais echoue durement sur un fichier
  **malforme**. Les deux pannes les plus probables sont donc celles qui se
  voient le moins.
- **Consequence sur 88.0, la plus vicieuse** : `RoomStatusChecker` passe le
  fichier a yt-dlp. Un fichier refuse produit une erreur qui n'est pas « Room
  is currently offline » -> etat `Unknown` -> **la surveillance ne declenche
  jamais rien**. Le garde-fou fonctionne, mais l'utilisateur aurait conclu que
  la fonctionnalite ne marche pas.
- **Methode qui a tranche** : comparer trois executions — fichier original,
  copie convertie en LF, et **chemin inexistant comme temoin**. C'est le temoin
  qui a revele l'ignorance silencieuse, et la copie LF qui a elimine CRLF.
  Sans le temoin, on aurait conclu a tort.
- `Services/CookieFileValidator.cs` + 11 tests (**180 au total**), eprouves en
  les cassant volontairement (3 echecs). Branche sur la selection du fichier
  dans `SettingsForm`.

**v1.26.0 (2026-08-05) — 98.0 note de legalite (app + site + README + wiki)** :
- **Texte redige par le mainteneur**, qui a corrige mes deux versions
  successives. Trois corrections factuelles apportees en retour : l'art. 550ter
  est l'atteinte aux donnees et NON le contournement de mesures techniques
  (celui-ci releve du droit d'auteur) ; la loi du 30 juin 1994 est codifiee
  depuis 2015, l'art. 22 §1er 5° est devenu l'art. XI.190 CDE ; et l'exception
  de copie privee exige un usage prive **et non commercial**, precision que le
  mainteneur a validee.
- **Point de fond a ne pas perdre** : violer des conditions d'utilisation n'est
  PAS illegal. C'est une inexecution contractuelle, qui expose a la fermeture du
  compte, pas a des poursuites. Ne pas melanger les deux dans une future
  redaction.
- **Bouton en rangee 1** (visible dans les deux modes), pas en rangee 3 avec
  Diagnostic : la confusion que ce texte dissipe concerne l'utilisateur au
  moment ou il enregistre.
- **Trois defauts trouves par la capture, aucun visible autrement** :
  (a) un `TextBox` multiligne WinForms n'interprete pas `
` seul — les
  paragraphes se collaient ; il faut `Environment.NewLine` ;
  (b) reposer `BackColor`/`ForeColor` depuis le formulaire APRES
  `ThemeManager.Apply` casse le rendu — ThemeManager traite deja le cas
  `TextBox`, ne rien surcharger ;
  (c) les asterisques d'emphase Markdown s'affichaient tels quels, le texte
  etant partage entre Markdown et l'application.
- **Piege d'outillage, deuxieme fois** : ecrire `
` depuis un script Python
  passe par un heredoc produit un VRAI saut de ligne dans le source C#. Pour
  toute chaine contenant des echappements, utiliser l'outil d'edition direct
  plutot qu'un script.

**v1.25.0 (2026-08-05) — 88.0 / 4.3 « Surveillance »** :
- **Faisabilite mesuree AVANT d'ecrire**, lecon de l'episode 92.0 : hors ligne
  -> `rc=1` + « Room is currently offline » ; en ligne -> `rc=0`, stderr vide.
  Releve sur trois salons reels. Un salon **inexistant** rend le meme message
  qu'un salon hors ligne : une faute de frappe se presente donc comme une
  absence, et sera attendue indefiniment.
- **yt-dlp plutot qu'une requete HTTP** : il atteint l'API JSON de Chaturbate
  sans etre bloque, la ou `HttpClient` se fait refuser en 403. Deja embarque,
  deja utilise pour enregistrer : zero dependance nouvelle.
- **Trois etats, pas deux.** `Unknown` (reseau coupe, salon banni, yt-dlp
  absent) ne declenche jamais rien. Sans lui, une coupure reseau lancerait des
  enregistrements dans le vide.
- **Controles en serie, jamais en parallele**, et 120 s par defaut : dix salons
  verifies chaque minute feraient 14 400 requetes par jour vers le site.
- **`StartRecording(url, interactive)`** : `OnStartClick` n'est plus qu'un
  appel. En mode non interactif les refus vont dans les logs — une modale
  bloquerait la boucle et personne n'est devant l'ecran. **Exception assumee** :
  `VerifyOrTrustBinary` garde son dialogue dans les deux modes, un hash de
  yt-dlp qui change DOIT interrompre.
- **PIEGE DE CAPTURE, le plus instructif de la session** : `grpWatch` avait ete
  oublie dans le `contentPanel.Controls.AddRange` — le panneau n'existait pas
  dans l'application. **La capture l'a pourtant rendu correctement**, parce que
  `DrawToBitmap` fonctionne sur un controle orphelin. Ce qui a trahi le defaut
  n'est pas la mise en page mais **les couleurs** : boutons au rendu Windows par
  defaut au lieu du bleu d'accent, signe que `ThemeManager.Apply(this)` ne
  l'avait jamais atteint. **A retenir : capturer le `contentPanel` entier, pas
  le panneau seul** — un panneau isole se dessine meme s'il n'est nulle part.

**v1.24.0 (2026-08-05) — l'import des favoris est RETIRÉ. Ne pas le re-proposer** :
- **Décision de l'utilisateur, motivée, à respecter** : la seule voie technique
  restante était WebView2 (moteur Chromium embarqué, donc empreinte TLS que
  Cloudflare ne distingue pas d'un vrai navigateur). Refusée pour trois raisons
  cumulées : contourner délibérément une protection du site met en cause
  l'éthique du projet, expose à un risque juridique, et impose une dépendance
  supplémentaire à tous les utilisateurs pour une fonctionnalité de confort.
- **Ce qui reste acquis** malgré la suppression du code : les favoris Chaturbate
  sont **privés**, aucun identifiant public ne peut y donner accès, et le 403 de
  Cloudflare ne se corrige pas côté client — un jeu d'en-têtes complet de Chrome
  a été essayé et refusé deux fois sur un vrai compte. Ne pas refaire ces essais.
- **Séquence complète, pour mémoire** : 1.23.0 ajout, 1.23.1 bouton non
  cliquable (`Anchor` manquant), 1.23.2 en-têtes complets + statut 403 dédié,
  1.23.3 correction de l'annonce trompeuse, 1.24.0 retrait. **Leçon** : quatre
  versions pour une fonctionnalité dont la faisabilité n'avait jamais été
  vérifiée contre le vrai site. Pour toute fonctionnalité reposant sur un
  service tiers non documenté, **tester le contact réel AVANT** d'écrire l'UI,
  les traductions et les tests.
- **`Services/CookieFileReader.cs` supprimé avec le reste** : plus aucun
  consommateur, l'app se contente à nouveau de passer le CHEMIN du cookies.txt à
  yt-dlp. Le savoir acquis reste ici : `#HttpOnly_` en tête de ligne n'est PAS un
  commentaire, et une expiration à `0` signifie « cookie de session ».

**v1.23.2 (2026-08-05) — 403 de la protection anti-robots** :
- **Premier contact réel avec le site** : l'import a échoué en **403 Forbidden**
  avec des cookies valides et une session fraîche. Ce n'est ni le compte ni les
  cookies — c'est le CLIENT qui est refusé. Ce que ça valide au passage : la
  conception « chaque échec a son message » a fonctionné, l'utilisateur a
  rapporté une cause exploitable du premier coup au lieu d'un « ça ne marche
  pas ».
- **Tentative** : jeu d'en-têtes complet d'un Chrome réel (`Accept`,
  `Accept-Language`, `Sec-Fetch-*`, `sec-ch-ua`, `Referer`,
  `AutomaticDecompression`). Le seul `User-Agent` ne suffit jamais face à une
  protection qui compare l'ensemble des en-têtes.
- **Pronostic à ne pas enjoliver** : si le 403 persiste, la cause est
  l'empreinte TLS (JA3), que `HttpClient` ne peut pas déguiser en Chrome — et
  aucun réglage d'en-tête n'y changera rien. Dans ce cas la fonctionnalité
  **n'est pas réalisable** telle quelle, et il faudra le dire plutôt que
  d'empiler les essais : chaque itération coûte un test manuel à l'utilisateur.
- **Statut `BlockedByBotProtection` distinct de `NetworkError`** : la conduite à
  tenir n'a rien à voir. Le message dirige vers l'ajout manuel plutôt que de
  laisser croire à un réglage à corriger.

**v1.23.1 (2026-08-05) — le bouton d'import ne repondait pas au clic** :
- **Le piège `Anchor` de la fin de ce fichier a encore frappé**, une version
  après avoir été écrit. `importFavoritesButton` n'avait tout simplement pas
  d'`Anchor` : sur une fenêtre élargie, `favoritesListBox` (`Left|Right`)
  s'étendait par-dessus lui pendant que ses deux voisins (`Top|Right`) suivaient
  le bord. **Aggravant** : ajouté en dernier dans `AddRange`, il était aussi au
  fond de l'ordre de plan — les clics partaient donc dans la liste. Symptôme vu
  par l'utilisateur : « rien ne se passe, aucun message ». Corrigé par `Anchor`
  posé APRÈS `AddRange`, plus un `BringToFront()`.
- **Leçon de vérification** : la capture d'écran de contrôle avait été prise à la
  largeur de conception (700 px), où le défaut est invisible. Pour tout contrôle
  ajouté à un panneau ancré, **capturer aussi une fenêtre élargie** — c'est le
  seul moment où un `Anchor` manquant se voit.
- **Piège d'outillage découvert, à retenir absolument** : depuis 93.0, le mutex
  d'instance unique fait **sortir immédiatement** tout lancement supplémentaire.
  Un processus resté vivant d'une capture précédente rend donc toutes les
  suivantes silencieusement inopérantes — l'exe rend 0, aucun fichier n'est
  écrit, et rien n'indique pourquoi. Avant toute session de capture :
  `taskkill //F //IM ChaturbateRecorder.exe`.
- **Second piège d'outillage** : les fichiers sources sont passés en CRLF, donc
  un motif de patch écrit en LF ne correspond plus à rien. Le script Python de
  capture doit normaliser (`s.replace('
','
')`) et réécrire dans la fin de
  ligne d'origine, sinon il ne patche rien **sans erreur visible**.

**v1.23.0 (2026-08-05) — 92.0 reformulé : import des favoris du compte** :
- **L'item d'origine reposait sur une prémisse fausse** et a failli produire un
  correctif inutile. Sa note décrivait un `LoadCookies` bogué (`parts.Length != 7`,
  `if (expiration <= 0) skip`) — ce code **n'existait pas** : l'app passait
  seulement le CHEMIN du cookies.txt à yt-dlp via `--cookies` sans jamais
  l'ouvrir. Le vrai constat de l'utilisateur (« j'importe, mes favoris
  n'apparaissent pas ») n'était pas un bug : l'import authentifie yt-dlp, il n'a
  jamais alimenté les favoris. **Réflexe** : vérifier dans le code que le
  symptôme décrit correspond à un chemin qui existe, avant de réparer.
- **Pas d'identifiant public possible** — question posée par l'utilisateur : les
  favoris Chaturbate sont privés, aucun identifiant seul ne peut y donner accès
  (sinon n'importe qui lirait les favoris de n'importe qui). L'identifiant dit
  « quel compte », les cookies disent « et j'ai le droit ». Les cookies portent
  déjà les deux, il n'existe pas de mécanisme plus simple.
- **Ironie utile** : le `LoadCookies` inexistant est devenu nécessaire. D'où
  `Services/CookieFileReader.cs`, et les deux pièges que la note d'origine
  citait sont réels — mais dans l'autre sens : `#HttpOnly_` en tête de ligne
  n'est PAS un commentaire (sauter tout `#` fait perdre les cookies de session,
  presque tous HttpOnly), et une expiration à `0` signifie « cookie de session »,
  pas « invalide ». Les deux ont leur test.
- **Fragilité assumée, conçue comme telle** : aucune API publique documentée,
  donc `ExtractRoomNames` lit le HTML. Elle cassera à la prochaine refonte du
  site sans que la CI le voie. D'où : sept statuts d'échec distincts, chacun avec
  son message, et **jamais d'échec silencieux**. Le piège qui compte :
  une session expirée renvoie la page de connexion avec un **code 200** —
  sans `LooksLikeLoginPage`, l'app annoncerait « aucun favori » à quelqu'un qui
  doit se reconnecter.
- **Non vérifié en conditions réelles** : la requête au vrai site n'a pas été
  exécutée. L'URL (`/followed-cams/`) et la structure des liens sont écrites
  d'après la forme attendue du site, pas constatées. À valider par l'utilisateur.
- **Tests** : `Tests/FavoritesImportTests.cs` (13, **173 au total**), éprouvés en
  les cassant volontairement (5 échecs).
- **Capture = revue d'interface, encore** : le libellé « Importer mes favoris »
  était tronqué à « Importer mes » dans 160 px. Raccourci en « Importer favoris »,
  au gabarit de « Supprimer favori » qui tient déjà.

**v1.22.2 + 99.0 (2026-08-04)** — captures d'écran publiées régénérées (README
+ les trois du site) : elles dataient d'avant la v1.19.0 et ne montraient ni
Diagnostic, ni Sponsoriser, ni le menu « Durée maximale » (87.0), ni les boutons
de partage (50.0). **Méthode** : injecter les données factices directement dans
les contrôles pendant la capture (`historyListView.Items.Add`) plutôt que de
faire lire le dossier de captures — le contenu réel de l'utilisateur ne doit
jamais atteindre un fichier publié. Le bump 1.22.2 vient d'un défaut vu **sur la
capture elle-même** : `grpDonate` à 136 px ne laissait que 2 px sous « Site web »
— géométriquement correct, visuellement collé à la bordure. Porté à 144.
Corollaire de méthode : régénérer les captures est aussi une revue d'interface,
c'est le seul moment où on regarde l'écran entier d'un œil neuf.

**v1.22.1 (2026-08-04) — lot de correctifs 95.0 / 93.0 / 94.0** :
- **95.0, le vrai coupable** : `DownloadEngine` lève `Running` dès que le
  **processus** yt-dlp a démarré, pas quand le flux coule. Une room hors ligne
  enchaîne donc `Running` -> `Failed` en boucle, et `HandleJobStateChanged`
  remettait `ReconnectAttempt = 0` à chaque `Running` : le plafond
  `AutoReconnectMaxAttempts` (5) était **inatteignable**, d'où une notification
  d'erreur toutes les 30 s pour toujours. La remise à zéro vit désormais dans
  `UpdateJobProgress` — recevoir une progression est la seule preuve que le flux
  existe. Leçon générale : un état « démarré » ne vaut pas un état « qui
  fonctionne », et tout compteur de retry remis à zéro sur le premier ne
  plafonne rien.
- **95.0, second défaut** : `RemoveJobRow` ne coupait ni le minuteur de
  reconnexion en attente ni le moteur, et `HandleJobStateChanged` ne vérifiait
  pas que la ligne existait encore — d'où des notifications pour un
  enregistrement disparu de l'écran. Les deux sont corrigés.
- **93.0** : `Mutex` nommé en portée `Local\` (pas `Global\` : deux sessions
  Windows doivent rester indépendantes). Un simple refus de démarrer ne
  suffisait pas — depuis 19.0 la fenêtre se masque dans la zone de notification,
  donc relancer l'exe est le geste de quelqu'un qui la cherche. La seconde
  instance signale un `EventWaitHandle` nommé que la première surveille sur un
  thread d'arrière-plan (`MainForm.ListenForSecondInstance`), puis se termine.
- **94.0 — piège WinForms à retenir** : avec un `TextImageRelation` autre
  qu'`Overlay`, WinForms découpe le bouton en deux zones et n'aligne le texte
  que **dans sa zone**, proportionnelle à sa largeur préférée. Sur un libellé
  long la zone remplit le bouton et le rendu paraît centré ; sur un libellé
  court elle est étroite et collée à l'icône. **Passer `TextAlign` de
  `MiddleRight` à `MiddleCenter` ne change donc strictement rien** — constaté
  par capture avant/après, la première tentative a été jetée. Correction réelle :
  `TextImageRelation.Overlay` sur ce seul bouton, qui place image et texte
  indépendamment. Réservé à `websiteButton` (220 px) ; les boutons de la barre
  du haut (130 px) risqueraient le chevauchement texte/icône.
- **Vérification de rendu** : `DrawToBitmap` sur `grpDonate`, avant et après.

**v1.22.0 (2026-08-04) — 50.0 « boutons de réseaux sociaux »** :
- **X et Reddit sont des liens de PARTAGE, pas des comptes** : le projet n'a ni
  compte X ni subreddit, et un bouton menant à un compte inexistant vaudrait
  moins que rien. `x.com/intent/post` et `reddit.com/submit` fonctionnent sans
  qu'on possède quoi que ce soit, et rien n'est publié depuis l'app — le
  navigateur s'ouvre sur un message pré-rempli, modifiable. Le jour où un compte
  existe, seule l'URL change dans `InitializeComponent`.
- **Aucune hauteur ajoutée** : la rangée se loge dans l'espace libre sous le
  texte du QR code (x 340..656, y 96..122) de `grpDonate`, dont la colonne de
  gauche et le QR occupaient déjà le reste. `grpLogs` est positionné à partir de
  `grpDonate.Bottom` dans `ApplyUiMode` : ne pas toucher à la hauteur évite de
  recalculer toute la suite du formulaire.
- **Pas d'entrée dans `Localization`** : les trois libellés sont des noms
  propres (X, Reddit, GitHub), identiques en FR et EN. Les ajouter au
  dictionnaire aurait créé deux traductions identiques à maintenir.
- **Vérification de rendu** : `DrawToBitmap` sur `grpDonate`.

**v1.21.0 (2026-08-04) — 79.0 « recherche automatique des mises à jour »** :
- **Le constat de départ** : `Services/UpdateChecker.cs` existait depuis la
  1.2.0 mais n'avait **qu'un seul appelant**, le bouton « Rechercher une mise à
  jour », lui-même réservé au mode avancé. Toute la fiabilisation de la chaîne
  de release faite en 38.0 (release -> `latest.json` -> site) n'atteignait donc
  jamais un utilisateur qui ne clique pas.
- **Timer + réglage relu à chaque tick** : `MainForm.StartAutoUpdateChecks`
  crée un `System.Windows.Forms.Timer` qui tourne **toujours** ; c'est le tick
  qui teste `_settings.AutoUpdateCheck`. Cocher/décocher la case dans
  `SettingsForm` prend donc effet immédiatement, sans redémarrage ni rappel à
  faire circuler jusqu'à `MainForm` (les trois autres réglages de cette fenêtre
  passent par des `Action<T>`, celui-ci n'en avait pas besoin).
- **Premier passage à 1 min, puis 1 h** : le même timer change son `Interval`
  au premier tick. Un appel réseau au démarrage se serait ajouté à la
  validation du dossier, la purge des logs, le contrôle des ACL et les
  dialogues de premier lancement.
- **Anti-répétition** : `UserSettings.LastNotifiedUpdateVersion` (une version,
  **pas** un booléen « déjà prévenu » — celui-ci aurait fait taire toutes les
  releases suivantes jusqu'au redémarrage). `UpdateChecker.ShouldNotify` porte
  la règle, `IsNewer` est passée `private` -> `internal` pour être testable.
- **Jamais de MessageBox en fond** : notification cliquable de la zone de
  notification uniquement. Une app qui tourne pendant des enregistrements de
  plusieurs heures ne doit pas voler le focus, et un échec (hors ligne, quota
  de l'API GitHub) ne produit qu'une ligne de log — contrairement au bouton,
  où l'utilisateur attend une réponse. `ShowNotification` prend un
  `Action? onClick` posé dans `_balloonClickAction`, **remis à null à chaque
  notification** pour qu'un toast de fin d'enregistrement n'hérite pas de
  l'action du toast de mise à jour précédent.
- **Piège Win32** : `NotifyIcon.Text` est limité à **63 caractères** et lève
  au-delà. Le libellé interpole la version et dépend de la traduction, d'où
  `SetTrayText` qui tronque plutôt que de risquer un plantage.
- **Refactor** : la partie « proposer + installer » de `OnCheckUpdateClick` est
  extraite en `PromptAndInstallAsync`, partagée avec le clic sur la
  notification — sinon un des deux chemins aurait fini par oublier
  l'avertissement « N enregistrements en cours seront interrompus ».
- **Tests** : `Tests/UpdateCheckerTests.cs` (10, **130 au total**), éprouvés en
  les cassant volontairement (4 échecs). Le cas qui compte vraiment est le tri
  lexicographique : `"1.9.0" > "1.10.0"` en comparaison de chaînes, d'où le
  repli `Version.TryParse` d'abord.
- **Vérification de rendu** : `DrawToBitmap` sur `SettingsForm` en clair/FR et
  sombre/EN (`ClientSize` 330 -> 382).
- **Renuméroté 1.20.0 -> 1.21.0 au moment de pousser** : une session voisine
  avait publié 87.0 (minuteur d'enregistrement) en v1.20.0 pendant l'écriture
  de celle-ci, tag compris. **Le numéro de version n'est réservé qu'au push**,
  jamais au bump : constaté ici parce que `git push` a été refusé en
  non-fast-forward *et* que `git push origin v1.20.0` a répondu « already
  exists » — deux refus qu'il ne faut surtout pas contourner par un `--force`,
  qui aurait effacé une release déjà publiée. Réflexe à garder : `git fetch`
  **avant** de taguer, pas après. Rebase sur `origin/main` : conflits attendus
  dans `CLAUDE.md` et `Config/Changelog.cs` (deux sessions ajoutent au même
  endroit), aucun dans `MainForm.cs` malgré 155 lignes ajoutées de part et
  d'autre — les deux fonctionnalités vivent dans des zones différentes du
  fichier.

**Suite immédiate à la v1.21.0 — le job `deploy` peut être VERT en publiant le
`latest.json` d'AVANT sa régénération** (aucun changement de code, note seule) :
- C'est la réponse au « à surveiller à la prochaine release » de la section
  ci-dessous. `Publish Release` a bien fini **vert de bout en bout** à la
  v1.21.0, ses trois jobs en `success`, sans intervention : la politique de
  déploiement `tag`/`v*` tient, et les bumps majeurs de Dependabot
  (`action-gh-release` v3, `deploy-pages` v5, `upload-artifact` v7) n'ont rien
  cassé. **Et pourtant le site servait encore la version précédente.**
- **Chronologie mesurée** : `update-latest-json` committe `latest.json` à
  17:59:34, le job `deploy` démarre à 17:59:46 et fait `checkout ref: main` —
  il aurait donc dû prendre le bon contenu. L'artefact déployé contenait
  malgré tout le `latest.json` d'avant.
- **Le test qui tranche, à réutiliser** : comparer **deux** fichiers du site,
  pas seulement `latest.json`. Ici `features.html` était à jour (donc le
  déploiement venait bien du commit de la session) alors que `latest.json` ne
  l'était pas — ce qui exclut d'emblée le cache CDN et désigne le contenu de
  l'artefact. Vérifier `latest.json` seul aurait laissé croire à un simple
  retard de propagation. Cache CDN écarté au préalable par 3 requêtes avec
  chaîne anti-cache sur 30 s, 2,5 min après la fin du déploiement.
- **Remise en état** : `workflow_dispatch` de `pages-build.yml` (`ref: main`),
  site revenu à 1.21.0, vérifié en ligne.
- **C'est INTERMITTENT, donc une course, pas un défaut systématique** : la
  release suivante (v1.22.0, ~20 min plus tard, même chaîne, mêmes workflows)
  a publié `latest.json` correctement **sans aucune intervention**. Ne pas
  conclure d'un déploiement réussi que le problème est réglé, ni d'un
  déploiement raté qu'un workflow est cassé — les deux se produisent avec le
  même code. D'où : **exécuter le test à deux fichiers après chaque release**,
  et rattraper par `workflow_dispatch` le cas échéant. Les utilisateurs de
  l'app ne sont pas affectés (`Services/UpdateChecker.cs` lit l'API GitHub,
  pas le site).

**Fin de la chaîne 38.0 (2026-08-04) — l'environnement `github-pages` refusait
un déploiement lancé depuis un tag** (réglage de dépôt uniquement, aucun
changement de workflow, pas de bump) :
- **Symptôme** : à la release v1.20.0, `Publish Release` s'est terminé en
  **échec** alors que la release et ses deux ZIP étaient corrects et que
  `releases/latest` pointait bien sur v1.20.0. Seul le job imbriqué
  `update-latest-json / deploy-pages / deploy` échouait, avec pour toute
  explication « The deployment was rejected or didn't satisfy other protection
  rules ». Le site est donc resté sur 1.19.1 et il a fallu relancer
  `pages-build.yml` à la main.
- **Cause** : l'environnement `github-pages` n'autorisait que des **branches**
  (`main`, `gh-pages`). Or `publish-release.yml` s'exécute sur un push de tag,
  et un workflow appelé par `workflow_call` **hérite de la référence de
  l'appelant** : la référence du déploiement était donc `v1.20.0`, rejetée par
  la règle. C'est le dernier maillon de la chaîne 38.0 — le déclenchement avait
  été réparé, la régénération de `latest.json` fonctionne (le fichier était
  correct dans le dépôt), c'est la **publication** qui était bloquée.
- **Correctif — réglage de dépôt, invisible dans le code** : ajout d'une
  politique de déploiement de **type `tag`, motif `v*`** dans Settings >
  Environments > github-pages. Les politiques personnalisées étaient déjà
  activées et `main`/`gh-pages` déjà listées : la règle a été **ajoutée**, rien
  n'a été restructuré, donc aucun risque de bloquer `main` au passage.
  API : `POST /repos/{owner}/{repo}/environments/github-pages/deployment-branch-policies`
  avec `{"name":"v*","type":"tag"}`.
- **Pourquoi c'est sans danger** : `pages-build.yml` fait `actions/checkout`
  avec `ref: main` explicite. Le contenu déployé vient donc **toujours** de
  `main` quelle que soit la référence déclenchante — seule la métadonnée du
  déploiement porte le tag. Autoriser les tags n'autorise pas à publier le
  contenu d'un tag.
- **Piège de vérification, à retenir** : tester avec un tag de pré-version
  (`vX.Y.Z-test`) **ne vérifie rien ici**. `deploy-pages` est conditionné à
  `if: needs.update-latest-json.outputs.changed == 'true'`, or une pré-version
  est exclue de `/releases/latest` et ne modifie donc pas `latest.json` : le job
  serait *sauté*, pas exécuté. La bonne vérification est un
  `workflow_dispatch` de `pages-build.yml` avec `ref` **valant un tag**
  (`{"ref":"v1.20.0"}`) : c'est le seul moyen de reproduire exactement la
  condition rejetée. Fait, et le déploiement passe désormais (`ref: v1.20.0`
  dans l'historique des déploiements).
- **À surveiller à la prochaine release** : `Publish Release` doit se terminer
  **vert** de bout en bout. S'il repasse au rouge, regarder d'abord la
  conclusion de chaque job — la release peut être parfaite alors qu'un job
  postérieur échoue, et l'inverse est vrai aussi (un run vert dont l'étape
  utile a été *sautée* n'a rien publié). La couleur du run ne dit rien : vérifier
  l'artefact final, ici `https://tomoushie.github.io/ChaturbateRecorder/latest.json`.

**87.0 traité (2026-08-04) — minuteur d'arrêt par enregistrement (v1.20.0)** :
- **Portée choisie par l'utilisateur** : un minuteur **par enregistrement**, pas
  un réglage global — cohérent avec qualité/codec/format, déjà des choix par
  enregistrement. Menu "Durée maximale" sur une deuxième rangée de
  `advancedOptionsPanel` (66 -> 112 px, `grpRecordHeightAdvanced` 172 -> 218).
- **Deux décisions de comportement, non évidentes** :
  - L'échéance est fixée au **premier** démarrage (`RecordingJob.StopAtUtc`,
    posé par `??=`) et **non repoussée par une reconnexion automatique** :
    « arrêter après 2 h » désigne 2 h de temps écoulé, pas 2 h par tentative,
    sinon une room instable enregistrerait indéfiniment.
  - L'arrêt passe par `Engine.Stop()`, qui marque l'arrêt comme **manuel** :
    l'état final est donc `Stopped`, ce qui **exclut** la reconnexion
    automatique dans `HandleJobStateChanged`. Un minuteur qui relancerait
    aussitôt l'enregistrement n'aurait aucun sens.
- **Logique pure isolée dans `Services/RecordingTimer.cs`** (table des durées +
  mise en forme du temps restant) pour être testable sans interface : 30 tests.
  Le garde-fou qui compte : il doit y avoir **exactement un libellé par durée**,
  la sélection étant convertie **par son index** — un libellé ajouté sans preset
  correspondant décalerait silencieusement toutes les durées.
- **Piège de rédaction rencontré** : deux tests initiaux échouaient parce qu'ils
  croyaient un commentaire trop affirmatif (« arrondi au supérieur »). En
  réalité `Math.Ceiling` ne porte que sur les **secondes** ; minutes et heures
  sont tronquées, comme dans tout décompte. C'est le commentaire qui a été
  corrigé, pas le code — mais l'épisode montre qu'un commentaire faux se
  propage dans les tests écrits ensuite.
- **Effet de bord assumé** : `nameLabel` d'une ligne de job passe d'`AutoSize` à
  une largeur bornée (335 px) avec `AutoEllipsis`, le libellé du minuteur
  occupant désormais la droite de la même rangée.
- **Fusion avec v1.19.0/v1.19.1** (livrées par une session voisine pendant le
  développement) : trois conflits dans `MainForm.cs`, résolus en gardant leur
  mise en page et en y réinsérant le minuteur. `RemoveJobRow`, introduit par
  v1.19.1 pour libérer le `Timer` de `ThemedProgressBar`, se charge désormais
  **aussi** d'arrêter le minuteur — même raisonnement, un `Timer` de plus que
  rien n'arrêterait si la ligne disparaissait sans passer par là.

**v1.19.1 (2026-08-04) — 3.4 « couleurs dynamiques de la barre de progression »
était inerte depuis son écriture** :
- **Le piège, à retenir au-delà de ce cas** : `PBM_SETBARCOLOR` (0x0409) est
  **ignoré sans erreur** par comctl32 v6 dès que les styles visuels sont actifs
  — et `Program.Main` appelle `Application.EnableVisualStyles()`. La
  fonctionnalité était annoncée dans le changelog de la **1.5.0** et n'a jamais
  rien affiché : Windows imposait son vert dégradé aux quatre états, donc
  `RunningColor`/`CompletedColor`/`FailedColor`/`StoppedColor` **et** l'effet
  `PulseProgressBar` étaient invisibles. Rien dans le code ne signalait l'échec,
  d'où **14 versions** livrées (1.5.0 -> 1.19.0) en annonçant une fonctionnalité
  qui n'existait pas. Leçon générale : un message Win32 envoyé par
  `SendMessage` qui « ne fait rien » ne lève pas d'exception et ne rend pas de
  code d'erreur exploitable — toute coloration de contrôle natif doit être
  vérifiée par capture réelle, jamais supposée acquise parce qu'elle compile.
- **Second défaut trouvé en reproduisant** (non signalé au départ) : en thème
  sombre la **piste** de la ProgressBar native reste blanche sur fond sombre,
  exactement le défaut de l'ascenseur natif qui avait motivé `ThemedScrollBar`.
  C'est ce qui a tranché entre les pistes de correction.
- **Écarté : `SetWindowTheme(handle, "", "")`** (uxtheme). Ferait bien reprendre
  effet à `PBM_SETBARCOLOR`, mais en retirant le style visuel du contrôle, qui
  retombe sur le rendu « classique » plat d'avant XP — et ne règle rien pour la
  piste. **Écarté aussi** : supprimer la coloration et s'en remettre à
  `StatusLabel` (perdrait une information déjà annoncée aux utilisateurs).
- **Retenu : `UI/ThemedProgressBar.cs`**, contrôle dessiné en GDI+ dérivé de
  `Control`, dans la même veine que `RoundedGroupPanel` et `ThemedScrollBar`.
  Reprend volontairement les noms de propriétés du contrôle natif
  (`Minimum`/`Maximum`/`Value`/`Style`/`MarqueeAnimationSpeed`) et son enum
  `ProgressBarStyle`, pour que les sites d'appel de `MainForm` se lisent comme
  avant. Rendu en capsule arrondie ; mode indéterminé = segment qui glisse,
  piloté par son propre `Timer`. `UI/ProgressBarColorExtensions.cs` supprimé.
- **Piège GDI+ rencontré** : avec `radius = Height / 2` sur un rectangle tracé
  à `Height - 1` (la bordure se dessine dessus), le diamètre dépasse le
  rectangle et le chemin arrondi **retombe silencieusement sur des coins
  carrés**. Le rayon doit se calculer sur la hauteur du rectangle, pas du
  contrôle. Et le garde-fou de dégénérescence doit comparer **strictement**
  (`d > côté`) : un diamètre *égal* au côté est la capsule parfaite, pas un cas
  dégénéré — sinon une progression de quelques pour cent s'affiche en carré.
- **Interaction avec le correctif de thème de la v1.19.0** : `ThemeManager` a
  désormais un cas `ThemedProgressBar`, qui ne pose **que** `TrackColor` et
  `BorderColor`, jamais `BarColor` (elle encode l'état du job, la réappliquer
  repeindrait en bleu une barre passée au rouge ou au vert). Ça compte parce que
  `5e97bbc` appelle `ThemeManager.Apply(container)` à la **création** de chaque
  ligne : le cas s'exécute donc aussi à ce moment-là. Le commentaire de
  `5e97bbc` justifiant cet appel affirmait qu'aucun cas `ProgressBar` n'existait
  dans `ThemeManager` — corrigé, sa conclusion restant vraie.
- **Fuite introduite puis refermée** : en mode indéterminé le contrôle fait
  tourner un `Timer` que seul son `Dispose` arrête, alors que les deux points de
  retrait d'une ligne se contentaient d'un `Controls.Remove`. D'où
  `MainForm.RemoveJobRow`, qui libère aussi les contrôles — `Dispose` différé
  par `BeginInvoke` parce qu'un des appelants est le gestionnaire `Click` du
  bouton « Retirer », qui appartient justement à la ligne détruite.
- **Tests** : `Tests/ThemedProgressBarTests.cs` (17, **120 au total**) sur la
  seule partie du rendu qui puisse être fausse sans se voir — barre n'atteignant
  pas tout à fait le bout à 100 %, débordement d'un pixel, et segment de marquee
  qui se figerait hors piste faute de rebouclage de la phase. Garde-fous
  éprouvés en les cassant volontairement (3 échecs).
- **Vérification de rendu** : `DrawToBitmap` en thème clair ET sombre sur de
  vraies lignes `BuildJobRow`. **Méthode à réutiliser** : pour un effet animé,
  échantillonner la **couleur du pixel** plutôt que juger à l'œil — deux
  captures espacées de la demi-période du `Pulse` sont retombées deux fois sur
  la même phase et donnaient l'illusion d'un effet mort ; la mesure au pixel
  montre l'alternance `#46BEFF` / `#0078D7` sur ~900 ms.

**v1.19.0 (2026-08-04, session voisine)** — hauteur des boutons portée à 26 px
(les jambages étaient rognés à 20/22/24 dès qu'une icône est posée en
`ImageBeforeText`), lignes de job élargies à 105 px (le `Padding` du thème
tronquait « Remove » en anglais), et surtout `ThemeManager.Apply(container)`
ajouté à la création d'une ligne : `Apply` n'étant appelé qu'une fois dans le
constructeur, une ligne créée après coup gardait le rendu système clair. Porte
aussi GitHub Sponsors (80.0, #34), mergé sans bump — d'où une version mineure
plutôt qu'un patch.

**Suite de 38.0 (2026-08-04) — le site ne publiait jamais le `latest.json`
régénéré** (CI uniquement, pas de bump) :
- **Symptôme** : le 2026-08-04, `docs/latest.json` était correct dans le dépôt
  (1.19.1) mais `https://tomoushie.github.io/.../latest.json` annonçait encore
  **1.18.0**. `Pages Build` n'avait plus tourné depuis le 2026-08-03 : le site
  a raté v1.19.0 **et** v1.19.1.
- **Cause — le même piège que 38.0, un cran plus bas** : `pages-build.yml` se
  déclenchait sur `push` avec `paths: docs/**`, or le **seul** producteur de
  `docs/latest.json` est le job `update-latest-json`, qui pousse avec le
  `GITHUB_TOKEN` — lequel ne déclenche aucun workflow. Le commit qui satisfait
  le filtre de chemin est donc exactement celui qui ne déclenche rien. 38.0
  avait réparé la **régénération** du fichier ; rien n'en assurait la
  **publication**. Morale : après avoir corrigé un maillon cassé par cette
  protection anti-boucle, vérifier tout le reste de la chaîne — chaque étape
  déclenchée par un commit du bot a le même défaut.
- **Correctif** : `pages-build.yml` accepte `workflow_call`, et
  `update-checker.yml` l'appelle en job `deploy-pages` (`needs:
  update-latest-json`), conditionné à un changement réel via une sortie
  `changed` — le cron quotidien ne redéploie donc pas le site pour rien.
- **Deux pièges dans le correctif lui-même** :
  - `actions/checkout` **doit** préciser `ref: main` dans le workflow appelé.
    Le SHA d'un run est celui de l'évènement **déclencheur** (le tag de la
    release), pas celui du commit que `update-latest-json` vient de pousser :
    sans ce `ref`, on déploie le `latest.json` d'AVANT sa mise à jour, soit
    précisément le bug qu'on corrige.
  - Les permissions d'un appel imbriqué **ne peuvent que se restreindre**, donc
    `pages: write` et `id-token: write` ont dû être ajoutés au job appelant
    dans `publish-release.yml`, qui ne déploie pourtant rien lui-même. Sans ça
    le déploiement échoue en fin de release automatisée alors qu'il passe très
    bien en exécution manuelle.
- **Remise en état immédiate** : `workflow_dispatch` sur `Pages Build` (le
  workflow le prévoit) — site revenu à 1.19.1, vérifié en ligne.
- **Non affecté** : le vérificateur de mise à jour de l'app lit
  `api.github.com/.../releases/latest` (`Services/UpdateChecker.cs`), pas le
  site — les utilisateurs voyaient donc la bonne version malgré la page figée.

**Package de sécurité renommé `SentinelGuard` et destiné à nuget.org
(2026-08-03)** — remplace/complète 30.0 (`ChaturbateRecorder.Security` sur
GitHub Packages) :
- **Pourquoi quitter GitHub Packages** : NuGet sur GitHub Packages **exige une
  authentification même pour un package public** — un consommateur doit créer
  un PAT `read:packages` et ajouter une source personnalisée avant un simple
  `dotnet add package`. Personne ne fait ça pour une bibliothèque tierce, donc
  le package publié en 30.0 était de fait inutilisable par des tiers. nuget.org
  = restauration anonyme + trouvable depuis Visual Studio.
- **Renommage `ChaturbateRecorder.Security` -> `SentinelGuard`** (répertoires,
  csproj, `AssemblyName`/`RootNamespace`, espace de noms, tests, workflows,
  dependabot, README/roadmap). Raison : un identifiant nuget.org est
  **définitif** (ni renommage ni suppression, seulement délistage), et une
  bibliothèque de sécurité Windows généraliste portant le nom d'un enregistreur
  de cams adultes ne sera pas ajoutée aux `.csproj`/SBOM d'entreprise.
- **Diligence de nommage — à refaire pour tout futur package** : vérifier non
  seulement que la racine est libre (`api.nuget.org/v3-flatcontainer/<id>/index.json`
  -> 404), mais **aussi qu'aucune famille `<id>.*` n'existe déjà**
  (`azuresearch-usnc.nuget.org/query?q=<id>`). Le premier choix de
  l'utilisateur, `RuntimeSentinel`, avait sa racine libre mais trois packages
  `RuntimeSentinel.Analyzers`/`.CodeFixes`/`.Scoring` appartenant à
  RenatoCarvalho, dans un domaine voisin (fiabilité .NET) : publier la racine
  aurait fait passer le package pour l'élément principal de sa suite. Même
  défaut écarté pour `WinSentinel` (`.Cli`, `.Core`), `Bulwark`, `Rampart`,
  `Aegis`, `Preflight`, `HardHat`. `SentinelGuard` est sans collision.
- **Multi-ciblage `net8.0-windows;net10.0-windows`** : ne cibler que net10.0
  aurait réduit fortement l'audience (la LTS est net8.0). Les tests tournent
  sur **les deux** cibles (63 x 2), pour valider le multi-ciblage à l'exécution
  et pas seulement à la compilation.
- **Qualité de package** : `GenerateDocumentationFile` (IntelliSense côté
  consommateur — a révélé que les 19 membres publics n'avaient aucun commentaire
  XML, tous documentés en anglais depuis), SourceLink + `snupkg` + build
  déterministe (`ContinuousIntegrationBuild` uniquement sur le runner, sinon il
  normalise les chemins et gêne le debug local), README en anglais servant de
  fiche nuget.org.
- **`.github/workflows/publish-nuget.yml`** : déclenché par un tag dédié
  **`sentinelguard-vX.Y.Z`**, volontairement distinct des tags `vX.Y.Z` de
  l'app (cycles de version indépendants ; un tag applicatif ne doit pas
  republier la bibliothèque). Vérifie que le tag correspond à `<Version>` du
  csproj avant de publier. `workflow_dispatch` avec `dry_run` par défaut à
  `true` pour tester sans publier.
- **Authentification par Trusted Publishing (OIDC), pas de clé API** —
  nuget.org déconseille désormais fortement les clés API pour la publication
  automatisée. Aucun secret n'est stocké : le job échange un jeton OIDC signé
  par GitHub contre une clé temporaire (1 h) via `NuGet/login@v1`, d'où
  `permissions: id-token: write` sur le job. **Piège** : les champs de la
  politique sur nuget.org doivent correspondre exactement, et « Dépôt »
  attend le **dépôt GitHub** (`ChaturbateRecorder`), pas le nom du package —
  erreur commise à la première tentative. « Flux de travail » attend le nom
  de fichier seul (`publish-nuget.yml`), sans le chemin. « Environnement »
  doit rester vide tant que le job ne déclare pas d'`environment:`.
  Autre piège : une politique neuve n'est que **temporairement active 7
  jours** ; sans publication réussie dans ce délai elle devient inactive (la
  fenêtre est relançable). La première publication réussie l'active
  définitivement en enregistrant les identifiants GitHub du dépôt, ce qui
  protège d'une attaque par recréation du dépôt sous le même nom.

**38.0 traité (2026-08-03) — `docs/latest.json` en retard d'une release** (CI
uniquement, pas de bump de version ni d'entrée de changelog) :
- **Symptôme** : après chaque release, le site GitHub Pages annonçait encore la
  version précédente pendant jusqu'à 24 h, jusqu'au passage du cron
  `17 3 * * *` de Update Checker. Contourné à la main pour v1.17.0 via
  `workflow_dispatch`.
- **Cause (piège GitHub Actions à retenir)** : **un évènement créé avec le
  `GITHUB_TOKEN` par défaut ne déclenche AUCUN workflow** (protection
  anti-boucle infinie). Or c'est ce token qui crée la release dans
  `publish-release.yml`, donc le déclencheur `release: [published]` de
  `update-checker.yml` n'est jamais parti — vérifié par l'API : 1 seul run
  depuis la création du workflow, déclenché par le cron. Le commentaire en
  tête du fichier présentait le cron comme un « filet si l'évènement a été
  raté » alors qu'il était le seul chemin réel ; corrigé.
- **Correctif** : `update-checker.yml` accepte `workflow_call` et
  `publish-release.yml` l'appelle en job `update-latest-json` (`needs:
  publish`) — appel direct, plus de dépendance à l'évènement. `release:
  published` conservé : il part bien pour une release créée **à la main**
  depuis l'interface web (identité utilisateur, pas GITHUB_TOKEN). Cron et
  `workflow_dispatch` conservés en filets. Ajout d'un `concurrency:
  update-latest-json` — trois chemins poussent un commit sur `main`, deux runs
  simultanés feraient échouer le push du second en non-fast-forward.
  Écartés : `workflow_run` (indirection en plus, mêmes garanties, et
  `workflow_run` ne s'exécute que depuis la branche par défaut) et le PAT à la
  place du `GITHUB_TOKEN` (secret à gérer/faire tourner pour rien).
- **Testabilité (ajout permanent)** : `prerelease: ${{ contains(tag, '-') }}`
  dans `publish-release.yml`. Un tag semver avec suffixe (`v1.17.1-test`) est
  publié en pré-version, que GitHub exclut de `/releases/latest` — la chaîne
  complète (tag -> build -> release -> régénération) est donc rejouable de bout
  en bout **sans** toucher au `latest.json` du site ni au vérificateur de mise
  à jour de l'app (la régénération tourne, ne voit aucune différence, ne
  committe rien).
- **Défaut préexistant révélé par le test** : `softprops/action-gh-release`
  met `make_latest` à `true` par défaut, donc **republier un ancien tag par
  `workflow_dispatch`** (rattraper les ZIP d'une vieille release) désigne cette
  vieille release comme "latest". Constaté en vrai le 2026-08-03 : 4
  republications (v1.14.1/v1.15.0/v1.15.1/v1.16.0) lancées pendant le test ont
  fait descendre `latest.json` jusqu'à 1.16.0 (site en régression ~2 min, remis
  à v1.17.0 via `make_latest` sur la release + un `workflow_dispatch` de Update
  Checker). Le défaut est antérieur au correctif 38.0 — le cron aurait produit
  le même résultat, juste 24 h plus tard donc moins visible. Corrigé par
  `make_latest: ${{ github.event_name == 'push' && !contains(tag, '-') }}` :
  seul un tag fraîchement poussé et non-pré-version devient "latest". La
  condition sur le suffixe évite de demander à l'API une combinaison qu'elle
  refuse (une pré-version ne peut pas être "latest"). Pour qu'une exécution
  manuelle devienne quand même "latest" : cocher "Set as the latest release"
  sur la page de la release.
- **Validé au passage** : le `concurrency` s'est fait éprouver pour de vrai —
  5 runs simultanés poussant chacun un commit `latest.json` sur `main`,
  sérialisés, aucun échec en non-fast-forward.

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
  SHA256 à chaque release publiée + cron quotidien en filet de sécurité —
  **voir le correctif 38.0 plus bas : le déclencheur `release: published`
  d'origine ne partait jamais**) ; Security Scan (CodeQL C# + `dotnet list package
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

**Dossier du projet** : `E:\Corpus\Chaturbate Record\Projet logiciel\ChaturbateRecorderApp`.
Il a déjà bougé deux fois — successivement `...\Projet logiciel\NET 8 Old\...`, puis
`E:\Corpus\Documents\Chaturbate Record\...`, aujourd'hui sans le `Documents`. Ne pas
supposer ce chemin : le vérifier au démarrage d'une session (le répertoire de travail
annoncé fait foi), et ne coder en dur aucun chemin absolu ailleurs que dans cette note.
Les seuls chemins absolus légitimes du dépôt sont `E:\Streamlink\videos` et
`E:\Streamlink\logs` dans `Config/AppConfig.cs` : ce sont les dossiers de capture et de
logs par défaut de l'app, sans rapport avec l'emplacement du projet.

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
   — **en français ET en anglais** dès lors que la version est >= 1.16.0 :
   `ChangelogTests` échoue sinon, volontairement (voir 24.0)
2bis. **Mettre à jour l'en-tête « État au ... — version courante » en haut de ce
   fichier.** Oublié trois fois (v1.15.0, v1.16.0, v1.19.0) : ce n'est pas une
   formalité, c'est la première chose qu'une session lit pour se situer.
3. `dotnet build` + `dotnet test Tests/ChaturbateRecorderApp.Tests.csproj` avant de committer
4. Commit avec identité Git via variables d'environnement (PAS `git config`, jamais autorisé) :
   `GIT_AUTHOR_NAME="Tomoushie" GIT_AUTHOR_EMAIL="Tomoushie@users.noreply.github.com"` (idem COMMITTER)
5. `git tag -a vX.Y.Z` + push du commit et du tag
6. Publier DEUX formats de release et les uploader via l'API GitHub (curl + token du Git Credential Manager, PAS de `gh` CLI — il n'est pas installé) :
   - Standard : `dotnet publish -c Release -r win-x64 --self-contained false` (~530 Ko, nécessite .NET 10 Desktop Runtime)
   - Portable : `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` (~46 Mo compressé, autonome)
   - Récupérer le token : `printf "protocol=https\nhost=github.com\n\n" | git credential fill | grep '^password=' | cut -d= -f2-`
   - Ne JAMAIS inclure `yt-dlp.exe`/`ffmpeg.exe` dans les ZIP de release (licence GPL de ffmpeg — voir README)

**Fichiers attaches aux releases — NE JAMAIS EN SUPPRIMER SANS VERIFIER LES
DEPENDANCES** (etabli le 2026-08-08). Chaque release porte quatre fichiers, et
trois d'entre eux ont un consommateur AUTOMATIQUE :
- `setup.exe` — l'installateur. **Il ne contient pas l'application** : il
  telecharge `portable.zip` de SA release. Supprimer ce ZIP casse toute
  installation.
- `portable.zip` — charge utile de l'installateur, ET cible de la mise a jour
  interne pour les installations autonomes.
- `standard.zip` — cible de la mise a jour interne pour les installations
  dependantes du runtime. 604 Ko : le garder ne coute rien.
- `sbom.json` — inventaire des dependances (86.0).

La question « peut-on ne publier que le setup.exe ? » s'est posee et la reponse
est **non** : l'installateur et le verificateur de mise a jour cessent tous deux
de fonctionner. Le vrai probleme etait la **presentation**, pas le nombre — d'ou
l'en-tete fixe des notes de release (`body:` dans `publish-release.yml`) qui dit
lequel prendre. **Le maintenir a jour si un fichier est ajoute ou retire.**

**Vérification visuelle des changements UI** — WinForms ne peut pas être piloté par `computer-use` (exe non reconnu comme "app installée"). À la place : ajouter temporairement dans le constructeur de `MainForm` un handler `Shown += (s,e) => { ... DrawToBitmap ... Environment.Exit(0); }` qui capture un vrai rendu en PNG dans le scratchpad, à regarder via `Read`. Piège découvert : `Form.DrawToBitmap()` ne rend pas correctement le fond d'un `Panel` avec `AutoScroll=true` imbriqué — capturer directement `contentPanel.DrawToBitmap(...)` à la place donne le rendu réel. Pour vérifier une animation (ex: transition de thème) : `Shown += async (s,e) => { ... await Task.Delay(...); Capture(...); TriggerAnimation(); await Task.Delay(...); Capture(...); Environment.Exit(0); }` — capturer après un délai supérieur à la durée de l'animation donne l'état final, `DrawToBitmap` n'étant pas affecté par `Form.Opacity` (qui n'agit que sur le compositing OS, pas le rendu GDI). Toujours retirer ce code de debug avant de committer, et penser aussi à commenter/décommenter `ShowFirstRunDialogs()` si un dialogue de premier lancement bloquerait la capture.

**Piège WinForms — `DrawToBitmap` sur un `Form` top-level** : contrairement à un `Panel` enfant, `DrawToBitmap` sur un formulaire top-level (ex: une fenêtre modale comme `SettingsForm`) inclut la barre de titre et les bordures. Dimensionner le bitmap sur `form.ClientSize` écrase alors le contenu du bas (la barre de titre "mange" de la hauteur sans que le bitmap ne s'agrandisse en conséquence). Utiliser `form.Size` (pas `.ClientSize`) pour la taille du bitmap et du rectangle de dessin.

**Piège WinForms — `Anchor` posé avant `Controls.Add`** : définir `Anchor` dans l'initialiseur d'objet d'un contrôle (avant qu'il soit ajouté à son parent) capture une marge basée sur un `Parent` encore `null` — le contrôle se retrouve projeté hors de la fenêtre dès le premier redimensionnement (marge négative interprétée comme "encore plus loin du bord"). Toujours poser `control.Anchor = ...;` en instruction séparée, APRÈS le `Controls.Add`/`AddRange` qui le parente.

**NuGet** — Le `NuGet.Config` global de cette machine (`%APPDATA%\NuGet\NuGet.Config`) a une liste de sources vide. Un `NuGet.Config` local (déjà présent à la racine du projet) ajoute `nuget.org`, sans toucher au fichier global. Sans lui, toute dépendance externe (y compris le self-contained publish, qui a besoin de runtime packs) échoue avec `NU1100`.

**Tests** — `Tests/ChaturbateRecorderApp.Tests.csproj` (xUnit, `net10.0-windows` car référence le projet principal WinForms). `Properties/AssemblyInfo.cs` expose `InternalsVisibleTo` pour tester des méthodes `internal` (ex: `CertificateValidator.VerifySubjectAlternativeName`). Un vrai bug a été trouvé et corrigé via ces tests : le SAN TLS dépendait du texte localisé par l'OS (`Format()`), remplacé par un décodage ASN.1 direct.

**Style de commit utilisateur** — messages de commit en français dans le titre
court, corps détaillé technique (avec le "pourquoi"), toujours signés
`Co-Authored-By: Claude <modèle> <noreply@anthropic.com>`.

Le modèle nommé est **celui qui a réellement écrit le commit**, pas une valeur
à recopier : l'historique porte déjà `Claude Sonnet 5` et `Claude Opus 5` selon
l'époque. Cette note disait `Claude Sonnet 5` sans adresse e-mail alors que
l'adresse était présente dans tous les commits et que plusieurs étaient déjà
signés Opus 5 — d'où cette formulation générique, qui ne se périmera pas au
prochain changement de modèle.
