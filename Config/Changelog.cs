using System.Collections.Generic;
using System.Linq;

namespace ChaturbateRecorderApp.Config
{
    /// <summary>
    /// Historique des versions, affiché en local (aucun serveur requis) via une
    /// boîte de dialogue "Nouveautés" au premier lancement suivant une mise à
    /// jour de version. À compléter à chaque bump de &lt;Version&gt; dans le .csproj.
    ///
    /// Traduction (24.0) : Entries reste la source canonique en français, et
    /// EnglishChanges ne couvre que les versions à partir de la version courante
    /// au moment de ce passage (1.16.0). L'historique antérieur reste en
    /// français — son intérêt est surtout archivistique, et une annonce ne
    /// remonte en pratique que de quelques versions (voir GetChangesSince).
    /// Un rendu multi-versions peut donc mélanger les deux langues : c'est le
    /// repli voulu, chaque version étant résolue indépendamment par GetChanges.
    /// </summary>
    public static class Changelog
    {
        public static readonly (string Version, string[] Changes)[] Entries =
        {
            ("1.0.0", new[]
            {
                "Migration du projet vers .NET 10.",
                "Barre de progression en mode indéterminé pendant l'enregistrement d'un live.",
                "Sélection de la qualité source (meilleure / moyenne 720p / minimale).",
                "Choix du codec de sortie (copie / H.264 / H.265, réencodé après coup sans bloquer l'UI).",
                "Choix du conteneur de sortie (MP4 / MKV / MOV).",
                "Choix du dossier de sauvegarde des enregistrements.",
                "Vérification du dossier d'exécution (réseau, temporaire, compressé).",
                "Détection des ACL permissives sur les dossiers sensibles.",
                "Vérification du hash du QR code de don.",
            }),
            ("1.1.0", new[]
            {
                "Enregistrement multi-stream : plusieurs lives en parallèle sans ouvrir plusieurs instances.",
                "Bouton \"Ouvrir\" par enregistrement (accès direct à la page du stream).",
                "Import de cookies navigateur (cookies.txt) pour accéder au contenu réservé à un compte connecté.",
                "Champ proxy SOCKS5/HTTP (confidentialité de l'IP vis-à-vis du site distant).",
                "Icône d'application.",
                "Publication sur GitHub (dépôt public + première release).",
            }),
            ("1.2.0", new[]
            {
                "Bouton \"Rechercher une mise à jour\" (vérifie et installe les nouvelles releases GitHub).",
                "Site web du projet (GitHub Pages) et bouton \"Site web\" dans l'appli.",
                "Guide de démarrage pas-à-pas au premier lancement, réutilisable via \"Guide de démarrage\".",
            }),
            ("1.3.0", new[]
            {
                "Watchdog anti-freeze sur yt-dlp/ffmpeg : arrêt automatique et log explicite si plus aucune sortie pendant un délai configurable (120s par défaut).",
                "Génération de miniature déplacée hors du thread UI (ne bloque plus l'interface pendant l'extraction ffmpeg).",
                "Logs structurés au format JSON (JSONL), avec horodatage ISO 8601 et source auto-détectée.",
            }),
            ("1.4.0", new[]
            {
                "Rotation automatique des logs : suppression des fichiers de plus de 14 jours au démarrage, et rotation (renommage horodaté) au-delà de 20 Mo par fichier.",
            }),
            ("1.5.0", new[]
            {
                "Icônes vectorielles sur les boutons principaux (rendu SVG à la volée, nettes à toute résolution).",
                "Mode simple / mode avancé : bouton de bascule masquant qualité/codec/format, dossier, cookies/proxy, thème, guide, mises à jour, favoris, don et logs.",
                "Notifications (zone de notification) : enregistrement terminé, erreur d'enregistrement, favori ajouté.",
                "Barre de progression : couleur dynamique selon l'état (bleu en cours, vert terminé, rouge erreur, gris arrêté) et effet de pulsation au démarrage.",
            }),
            ("1.6.0", new[]
            {
                "Reconnexion automatique (optionnelle, par enregistrement) : si le live se termine de façon inattendue, nouvelle tentative après un délai configurable, jusqu'à un nombre maximal de tentatives, avec possibilité d'annuler.",
                "Historique des enregistrements : liste des vidéos capturées (nom, taille, durée si ffprobe.exe est présent, date), avec bouton \"Ouvrir dossier\".",
            }),
            ("1.7.0", new[]
            {
                "Fenêtre redimensionnable (agrandir/réduire/maximiser) : le contenu défile automatiquement si la fenêtre devient plus petite que sa taille naturelle, au lieu d'être coupé.",
            }),
            ("1.8.0", new[]
            {
                "Suite de tests xUnit (sandbox URL, sandbox chemins, hash binaire, parsing progression, correspondance SAN TLS).",
                "Correctif : la vérification TLS (pinning serveur) comparait le nom d'hôte à du texte localisé par l'OS et ne fonctionnait donc jamais correctement sur un Windows non-anglais. Remplacé par un décodage ASN.1 direct, indépendant de la langue.",
            }),
            ("1.9.0", new[]
            {
                "Nouveau format de release \"portable\" : exécutable autonome (runtime .NET inclus, aucune installation requise sur la machine cible), en plus du format habituel dépendant du runtime.",
            }),
            ("1.10.0", new[]
            {
                "Modernisation de l'interface (palette Windows 11) : boutons en couleur d'accent sans bordure, coins arrondis, police Segoe UI, titres de section en gras, espacement généreux entre les sections (20px).",
                "Correctif : le texte de plusieurs boutons (\"Démarrer\", \"Tout arrêter\", \"Parcourir...\", \"Supprimer favori\", \"Rechercher une mise à jour\") était tronqué — largeurs ajustées.",
                "Correctif thème sombre : le fond de certains panneaux restait clair au lieu de suivre le thème actif.",
            }),
            ("1.11.0", new[]
            {
                "Sections de l'interface : les cadres classiques (GroupBox) sont remplacés par des panneaux à bordure arrondie et ombre légère, façon carte \"Fluent\".",
                "Barre de progression : remplissage animé (au lieu d'un saut instantané) à la fin d'un enregistrement.",
                "Boutons : couleurs distinctes au survol et à l'appui de la souris.",
            }),
            ("1.12.0", new[]
            {
                "Fondu d'ouverture au démarrage de l'application, et clignotement léger lors du changement de mode simple/avancé.",
                "Transition animée (au lieu d'un changement instantané) lors du passage entre thème clair et thème sombre.",
                "Boutons : la couleur au survol et à l'appui change désormais en douceur plutôt que d'un coup.",
            }),
            ("1.13.0", new[]
            {
                "Sélecteur de langue (Français / English) : traduit les libellés, boutons, cases à cocher et en-têtes de colonnes de l'interface principale. Choix mémorisé entre les lancements.",
            }),
            ("1.13.1", new[]
            {
                "Correctif : la miniature (et le réencodage) d'un enregistrement pouvait être associée au mauvais fichier lorsqu'un même salon était enregistré plusieurs fois (le fichier était retrouvé par \"le plus récent portant ce nom\", ambigu ; il est maintenant identifié par son nom exact, fixé au démarrage de l'enregistrement).",
                "Correctif : élargir la fenêtre ne redimensionnait pas le contenu (panneaux, listes, champs) qui restait figé à sa largeur d'origine.",
                "Correctif : les boutons \"Site web\", \"Ouvrir dossier\" et \"Supprimer favori\" avaient leur texte rogné.",
            }),
            ("1.14.0", new[]
            {
                "Nouvelle fenêtre \"Paramètres\" : thème, langue, dossier de sauvegarde, cookies, proxy et reconnexion automatique par défaut (désormais mémorisée) y sont regroupés, séparément de la fenêtre principale.",
                "Fermer la fenêtre principale la réduit maintenant dans la zone de notification au lieu de quitter l'application — les enregistrements en cours continuent en arrière-plan. Menu clic droit sur l'icône : Ouvrir / Paramètres / Fermer.",
                "Bouton \"Signaler un bug\" qui ouvre un ticket GitHub pré-rempli (version, système, dossier de capture).",
                "Le site du projet (tomoushie.github.io/ChaturbateRecorder) est désormais disponible en français et en anglais, via un bouton de bascule.",
            }),
            ("1.14.1", new[]
            {
                "Correctif : \"Rechercher une mise à jour\" pouvait télécharger la mauvaise variante (standard/portable) quand une release en propose plusieurs — l'exécutable en cours est maintenant détecté pour choisir le bon ZIP.",
            }),
            ("1.15.0", new[]
            {
                "Crash Reporter : les erreurs inattendues (thread principal ou non) sont désormais capturées, journalisées dans un fichier de rapport dédié, et affichées dans un dialogue proposant d'ouvrir le dossier des logs ou de redémarrer proprement l'application.",
            }),
            ("1.15.1", new[]
            {
                "Correctif : le hash attendu de yt-dlp.exe/ffmpeg.exe étant figé à la version testée par le mainteneur, toute mise à jour de ces outils (fréquente pour yt-dlp) bloquait le démarrage d'un enregistrement. Un dialogue propose désormais explicitement de faire confiance à la nouvelle version détectée ; ce choix est mémorisé.",
            }),
            ("1.16.0", new[]
            {
                "Nouveau bouton \"Diagnostic\" (mode avancé) : panneau affichant les versions (.NET, application, yt-dlp, ffmpeg), l'intégrité des binaires externes, l'état des ACL et du dossier d'exécution, et la joignabilité réseau — copiable en un clic pour un rapport de bug.",
            }),
            ("1.17.0", new[]
            {
                "Traduction étendue : les messages d'erreur, les dialogues, les notifications, le guide de démarrage et l'annonce des nouveautés suivent désormais la langue choisie, au lieu de rester en français quel que soit le réglage.",
                "Les lignes de la zone \"Enregistrements en cours\" (statut, bouton Stop / Retirer / Annuler, bouton Ouvrir) suivent également la langue choisie, y compris lorsqu'elle est changée pendant un enregistrement.",
                "Le panneau de diagnostic et le rapport d'erreur restent volontairement en français : ils sont destinés à être collés dans un ticket, où des rapports en deux langues compliqueraient le dépouillement.",
            }),
            ("1.18.0", new[]
            {
                "Le dialogue \"Nouveautés\" annonce désormais toutes les versions franchies depuis la dernière utilisation, regroupées par version, au lieu de la seule version installée. Passer directement de la 1.14.0 à la 1.18.0 ne fait donc plus disparaître les nouveautés intermédiaires.",
                "\"Nouveautés\" s'affiche maintenant dans une fenêtre redimensionnable qui défile, au lieu d'une boîte de dialogue système qui tronquait le texte au-delà de la hauteur de l'écran. Son ascenseur suit le thème clair ou sombre, ce que ne fait pas un ascenseur Windows classique.",
            }),
            ("1.19.0", new[]
            {
                "Nouveau bouton \"Sponsoriser (GitHub)\" dans le panneau \"Soutenir le projet\" : ouvre la page GitHub Sponsors du projet, en complément du don PayPal déjà proposé.",
                "Correctif : sur chaque ligne de la zone \"Enregistrements en cours\", le bas des lettres des boutons \"Ouvrir\" et \"Stop\" était coupé — les boutons étaient trop courts d'environ 6 pixels pour la police, jambages compris. Défaut présent depuis l'origine.",
                "Correctif : en anglais, le bouton \"Remove\" d'un enregistrement terminé s'affichait tronqué en \"Remov\" après un changement de thème, faute de largeur suffisante. Les deux boutons de la ligne ont été élargis.",
                "Correctif : le même défaut de hauteur touchait les boutons de la barre du haut (\"Paramètres\", \"Mode simple\", \"Guide de démarrage\", \"Rechercher une mise à jour\", \"Signaler un bug\", \"Diagnostic\"), dont le bas des lettres à jambage était lui aussi coupé. Ces boutons ont été rehaussés et les trois rangées repositionnées.",
                "Correctif : les lignes de la zone \"Enregistrements en cours\" ne prenaient pas le thème à leur création — leurs boutons gardaient l'apparence Windows par défaut au lieu de la couleur d'accent, et une ligne ajoutée en thème sombre restait claire jusqu'au changement de thème suivant.",
            }),
            ("1.19.1", new[]
            {
                "Correctif : la couleur de la barre de progression selon l'état (bleu en cours, vert terminé, rouge erreur, gris arrêté), annoncée en 1.5.0, ne s'est en réalité jamais affichée — Windows impose sa propre couleur aux barres de progression dès que les styles visuels sont actifs, et les quatre états apparaissaient donc tous en vert. La barre est désormais dessinée par l'application : les quatre états se distinguent enfin, et l'effet de pulsation au démarrage d'un enregistrement devient visible lui aussi.",
                "La piste de la barre de progression suit désormais le thème clair ou sombre, au lieu de rester blanche sur fond sombre comme le fait une barre Windows classique.",
            }),
            ("1.20.0", new[]
            {
                "Minuteur d'arrêt automatique : un nouveau menu \"Durée maximale\" (mode avancé) permet de choisir au démarrage combien de temps un enregistrement doit durer — 15 minutes, 30 minutes, 1, 2, 4 ou 8 heures — au terme desquelles il s'arrête tout seul. Le réglage vaut par enregistrement, comme la qualité ou le format, et reste sur \"Illimité\" par défaut.",
                "Le temps restant s'affiche sur la ligne de l'enregistrement concerné et se met à jour en continu. Rien n'apparaît pour les enregistrements sans minuteur.",
                "Une reconnexion automatique ne repousse pas l'échéance : une durée maximale de 2 heures désigne bien 2 heures de temps écoulé, et non 2 heures par tentative.",
            }),
            ("1.21.0", new[]
            {
                "Recherche automatique des mises à jour : l'application vérifie désormais d'elle-même toutes les heures si une nouvelle version est disponible, au lieu d'attendre un clic sur \"Rechercher une mise à jour\".",
                "Quand une version est trouvée, une notification apparaît dans la zone de notification ; cliquer dessus propose l'installation, avec le même avertissement qu'avant si des enregistrements sont en cours. Chaque version n'est annoncée qu'une fois.",
                "Nouvelle case \"Rechercher automatiquement les mises à jour\" dans les Paramètres, cochée par défaut : la décocher désactive immédiatement le seul appel réseau que l'application effectue d'elle-même.",
                "Une recherche automatique qui échoue (hors ligne, coupure réseau) n'affiche plus rien du tout et se contente d'une ligne de log — seul le bouton, où une réponse est attendue, signale encore les erreurs.",
            }),
            ("1.22.0", new[]
            {
                "Trois nouveaux boutons dans le panneau \"Soutenir le projet\" : X et Reddit ouvrent un message de partage du projet pré-rempli, GitHub ouvre le dépôt.",
                "Rien n'est publié depuis l'application : elle ouvre simplement la page de publication du réseau concerné dans le navigateur, où le message reste modifiable avant envoi.",
            }),
            ("1.22.1", new[]
            {
                "Correctif : ajouter un live hors ligne déclenchait une notification d'erreur toutes les 30 secondes, indéfiniment, et elle continuait même après avoir annulé et retiré l'enregistrement de la liste. La limite de 5 tentatives de reconnexion ne s'appliquait en réalité jamais, et retirer une ligne laissait yt-dlp tourner en arrière-plan.",
                "Correctif : lancer l'exécutable plusieurs fois ouvrait autant d'instances, empilant les icônes dans la barre des tâches. Un second lancement réaffiche désormais la fenêtre déjà ouverte — utile depuis que la fenêtre se masque dans la zone de notification au lieu de se fermer.",
                "Correctif : le texte du bouton \"Site web\" n'était pas centré comme celui des boutons \"Sponsoriser\" et \"Faire un don\" juste au-dessus.",
            }),
            ("1.22.2", new[]
            {
                "Le bouton \"Site web\" n'est plus collé à la bordure basse du panneau \"Soutenir le projet\" : le panneau a été agrandi pour lui laisser la même marge que celle du haut.",
            }),
            ("1.23.0", new[]
            {
                "Nouveau bouton \"Importer favoris\", destiné à récupérer les modèles suivis sur ton compte Chaturbate. ATTENTION : la fonctionnalité ne fonctionne pas en l'état — Chaturbate refuse les requêtes qui ne viennent pas d'un vrai navigateur, et le bouton se solde par un message d'erreur. Elle est conservée en vue d'une correction ; en attendant, ajoute tes favoris avec \"+ Favori\".",
                "L'import réutilise le fichier cookies que tu as déjà configuré dans les Paramètres : tes favoris sont privés, seule une session connectée peut les lire, et c'est ce fichier qui la porte.",
                "Chaque cause d'échec a son propre message : aucun fichier cookies, mauvais format, session expirée, site injoignable, ou page reçue sans salon reconnu. Aucun échec n'est silencieux.",
            }),
            ("1.23.1", new[]
            {
                "Correctif : le bouton \"Importer favoris\", ajouté la version précédente, ne réagissait pas au clic dès que la fenêtre était élargie — il restait à sa position d'origine pendant que la liste des favoris s'étendait par-dessus, et les clics partaient dans la liste. L'import ne se lançait jamais, sans le moindre message.",
            }),
            ("1.23.2", new[]
            {
                "L'import des favoris envoie désormais le même jeu d'en-têtes qu'un vrai navigateur, plutôt que le seul identifiant de navigateur.",
                "Un refus 403 de Chaturbate a maintenant son propre message : il signale la protection anti-robots du site, et non un problème de compte ou de cookies — deux situations qu'il ne faut pas confondre, puisque la première ne se corrige pas côté utilisateur.",
            }),
            ("1.23.3", new[]
            {
                "Correction de l'annonce de la 1.23.0, qui présentait l'import des favoris comme fonctionnel : il ne l'est pas. Deux essais sur un vrai compte, avec des cookies valides, se sont soldés par un refus du site. L'entrée correspondante a été rectifiée plutôt que laissée telle quelle.",
            }),
            ("1.24.0", new[]
            {
                "Le bouton \"Importer favoris\" est retiré. La seule façon de le faire fonctionner aurait été d'embarquer un moteur de navigateur pour contourner la protection anti-robots de Chaturbate — une dépendance supplémentaire imposée à tous, pour contourner délibérément une protection du site. Ce n'est pas la direction voulue pour ce projet.",
                "Les favoris s'ajoutent avec le bouton \"+ Favori\", comme avant.",
            }),
            ("1.25.0", new[]
            {
                "Nouveau panneau \"Surveillance\" : ajoute un salon, et l'enregistrement démarre tout seul dès qu'il passe en ligne. Une notification prévient à chaque démarrage.",
                "Les salons surveillés sont contrôlés toutes les 2 minutes par défaut, réglable dans les Paramètres (1, 2, 5 ou 10 minutes). Ils sont vérifiés l'un après l'autre, jamais tous en même temps.",
                "Un salon dont l'état ne peut pas être déterminé (réseau coupé, salon banni) est affiché comme indéterminé et ne déclenche jamais d'enregistrement.",
                "La liste surveillée est volontairement distincte des favoris : un favori est un raccourci de saisie, un salon surveillé engage l'application à interroger le site en boucle.",
            }),
            ("1.26.0", new[]
            {
                "Nouveau bouton \"Légalité\", visible en mode simple comme en mode avancé : il affiche une note sur le cadre légal belge — ce que le logiciel fait et ne fait pas, l'exception de copie privée, et l'interdiction pénale de diffuser un enregistrement sans consentement.",
                "La même note est publiée sur le site, dans les deux README et dans le wiki, pour qu'elle soit trouvable avant même d'installer l'application.",
            }),
            ("1.26.1", new[]
            {
                "Le fichier cookies est désormais contrôlé au moment où tu le choisis. Un fichier que yt-dlp refuserait est signalé tout de suite, avec la ligne fautive et ce qu'il faut corriger, au lieu de faire échouer silencieusement tous les enregistrements.",
                "Ce contrôle vient d'un cas réel : un simple dièse manquant devant \"HttpOnly_\" invalidait 5 cookies sur 6, dont celui de session. Tous les enregistrements échouaient, et la surveillance automatique restait muette, sans que rien ne désigne les cookies.",
                "Correctif : ajouter un favori déjà présent affichait \"URL invalide ou déjà présente\", laissant deviner laquelle des deux causes s'appliquait. Les deux cas ont maintenant leur propre message.",
            }),
            ("1.27.0", new[]
            {
                "Correctif majeur : sur une machine sans disque E:, l'application affichait \"erreur fatale\" et se fermait avant même d'apparaître. Les dossiers par défaut pointaient vers un chemin propre au poste du développeur. Ils sont désormais dans tes dossiers personnels : le sous-dossier \"Chaturbate Recorder\" de tes Vidéos, pour les enregistrements.",
                "Un dossier de sauvegarde devenu inaccessible (disque débranché, dossier supprimé) ne fait plus planter l'application : elle prévient, bascule sur le dossier par défaut et continue.",
                "Les logs et les rapports de plantage vont maintenant dans AppData, qui existe toujours et reste inscriptible — c'est ce qui empêchait le rapport détaillé d'être enregistré quand l'application plantait au démarrage.",
                "Si tu enregistrais déjà ailleurs, ton dossier est conservé : seule la valeur par défaut change.",
            }),
            ("1.28.0", new[]
            {
                "Nouvel installateur, à télécharger depuis les releases GitHub : il propose installation classique ou version portable, télécharge et vérifie yt-dlp et ffmpeg pour toi, et n'exige aucun runtime .NET.",
                "La mise à jour automatique vérifie désormais l'empreinte du fichier téléchargé avant de remplacer quoi que ce soit. En cas de non-correspondance, rien n'est installé.",
                "Correctif : une mise à jour qui échouait (application encore ouverte, fichier verrouillé) relançait l'ancienne version sans rien dire — on pouvait croire avoir mis à jour. Le délai d'attente passe de 15 secondes à 2 minutes, et tout échec est désormais écrit dans les logs.",
                "Après une mise à jour, la fiche « Applications installées » de Windows affiche enfin la bonne version.",
            }),
            ("1.28.1", new[]
            {
                "Correctif : la mise à jour de la version précédente ne remplaçait en réalité aucun fichier, tout en annonçant avoir réussi. Vérifié cette fois en exécutant une vraie mise à jour de bout en bout.",
                "Correctif : la fiche « Applications installées » recevait l'ancien numéro de version au lieu du nouveau, donc restait inchangée.",
            }),
            ("1.29.0", new[]
            {
                "L'historique affiche enfin les miniatures de tes enregistrements. Elles étaient générées après chaque capture depuis la version 1.3.0, déposées à côté de la vidéo… et n'étaient affichées nulle part.",
                "Nouveau bouton \"Ouvrir fichier\" : lance la vidéo sélectionnée dans ton lecteur habituel, sans passer par l'explorateur.",
                "Le panneau d'historique est plus haut et ses colonnes ont été réajustées : la date n'est plus tronquée et la barre de défilement horizontale a disparu.",
            }),
            ("1.30.0", new[]
            {
                "Mode dégradé : quand un composant est défaillant, l'application le désactive, te dit lequel et pourquoi, et continue de fonctionner — au lieu d'échouer plus tard sans explication.",
                "Concrètement : ffmpeg absent n'empêche plus d'enregistrer (seuls le réencodage et les miniatures sont suspendus), et un fichier cookies illisible est ignoré au lieu de faire échouer toutes les captures en silence.",
                "Nouvelle section \"Fonctionnalités\" dans les Paramètres : tu peux désactiver toi-même le réencodage, les cookies, le proxy, les enregistrements simultanés ou la surveillance — utile pour isoler un problème.",
                "Réactiver une fonctionnalité à la main annule aussi la désactivation automatique : la vérification sera refaite au prochain démarrage.",
            }),

            ("1.31.0", new[]
            {
                "Interface repensée : les panneaux deviennent de vraies cartes posées sur le fond de la fenêtre, avec une ombre douce et leur titre à l'intérieur — au lieu d'un simple liseré qui donnait un écran tout plat.",
                "Un seul bouton coloré par zone : « Démarrer » et « Sponsoriser » ressortent, tout le reste passe en bouton neutre. Avant, dix-huit boutons bleus se disputaient l'attention.",
                "Ce qui interrompt ou supprime (Stop, Tout arrêter, Supprimer favori, Ne plus surveiller) se reconnaît désormais à son rouge discret.",
                "Listes et champs suivent enfin le thème sombre : plus d'en-tête de colonnes blanc au-dessus d'une liste noire, ni de bordure claire autour des zones de saisie et des menus déroulants.",
                "Les intitulés de champ passent au second plan et le pourcentage d'un enregistrement s'affiche en nombre entier : la décimale changeait plusieurs fois par seconde sans rien apprendre à personne.",
                "Les libellés de boutons sont réellement centrés avec leur icône, quel que soit leur largeur.",
            }),

            ("1.32.0", new[]
            {
                "Trois nouvelles plateformes : Twitch, YouTube et TikTok. Colle l'adresse du live comme tu le ferais pour Chaturbate — le champ s'appelle désormais « URL du live ».",
                "La surveillance sait maintenant dire qu'une chaîne n'existe pas, au lieu d'attendre indéfiniment une adresse mal saisie.",
                "Sur YouTube, seuls les vrais directs déclenchent un enregistrement : une vidéo déjà publiée n'est plus prise pour un live.",
                "Les enregistrements YouTube portent enfin le nom de la chaîne ou de la vidéo, au lieu de s'appeler tous « watch ».",
                "La note de légalité couvre les nouvelles plateformes : sur celles qui ne sont pas adultes, l'enjeu est le droit d'auteur, et enfreindre des conditions d'utilisation reste contractuel, pas pénal.",
            }),

            ("1.33.0", new[]
            {
                "Les plateformes prises en charge sont affichées à côté du champ d'adresse, et chaque ligne surveillée porte l'icône de la sienne.",
                "Correction : dans l'historique, la taille, la durée et la date disparaissaient jusqu'au prochain clic.",
                "Correction : les coins des listes déroulantes changeaient brièvement d'apparence au passage de la souris.",
                "Correction : les boutons du guide de démarrage s'affichaient mal depuis la refonte de l'interface.",
                "Le guide de démarrage et la fenêtre Diagnostic suivent enfin le thème sombre, au lieu de s'ouvrir en clair.",
                "Les ascenseurs des listes ne sont plus blancs en thème sombre.",
            }),
            ("1.34.0", new[]
            {
                "Nouveau bouton \"Remerciements\" dans le panneau \"Soutenir le projet\" : il affiche les personnes dont les dons soutiennent le projet, sans jamais mentionner de montant.",
                "La liste des donateurs est aussi récupérée sur le site du projet à l'ouverture de la fenêtre : un nom peut donc être ajouté sans attendre une nouvelle version. Sans connexion, la liste embarquée s'affiche.",
                "Un nom n'est ajouté qu'avec l'accord de la personne concernée, et la liste est triée alphabétiquement pour que son ordre ne puisse pas se lire comme un classement.",
            }),
        };

        /// <summary>
        /// Traductions anglaises, à partir de 1.16.0 uniquement (voir le
        /// commentaire de classe). Une version absente ici retombe
        /// silencieusement sur son entrée française : c'est le comportement
        /// voulu pour l'historique ancien, jamais une erreur.
        /// </summary>
        private static readonly Dictionary<string, string[]> EnglishChanges = new()
        {
            ["1.16.0"] = new[]
            {
                "New \"Diagnostics\" button (advanced mode): a panel showing versions (.NET, application, yt-dlp, ffmpeg), the integrity of external binaries, the state of the ACLs and of the execution folder, and network reachability — copied in one click for a bug report.",
            },
            ["1.17.0"] = new[]
            {
                "Extended translation: error messages, dialogs, notifications, the getting started guide and the what's-new announcement now follow the selected language, instead of staying in French whatever the setting.",
                "Rows in the \"Active recordings\" area (status, Stop / Remove / Cancel button, Open button) also follow the selected language, including when it is changed while a recording is running.",
                "The diagnostics panel and the crash report deliberately stay in French: they are meant to be pasted into an issue, where reports in two languages would make triage harder.",
            },
            ["1.18.0"] = new[]
            {
                "The \"What's new\" dialog now announces every version crossed since the application was last used, grouped by version, instead of the installed version only. Going straight from 1.14.0 to 1.18.0 therefore no longer hides the intermediate changes.",
                "\"What's new\" is now shown in a resizable, scrolling window, instead of a system dialog box that truncated any text taller than the screen. Its scroll bar follows the light or dark theme, which a standard Windows scroll bar does not.",
            },
            ["1.19.0"] = new[]
            {
                "New \"Sponsor (GitHub)\" button in the \"Support the project\" panel: it opens the project's GitHub Sponsors page, alongside the PayPal donation already offered.",
                "Fix: on every row of the \"Active recordings\" area, the bottom of the letters on the \"Open\" and \"Stop\" buttons was cut off — the buttons were about 6 pixels too short for the font, descenders included. This defect had been there from the start.",
                "Fix: in English, the \"Remove\" button of a finished recording was truncated to \"Remov\" after a theme change, for lack of width. Both buttons on the row have been widened.",
                "Fix: the same height defect affected the buttons of the top bar (\"Settings\", \"Simple mode\", \"Getting started guide\", \"Check for updates\", \"Report a bug\", \"Diagnostics\"), where the bottom of descending letters was cut off too. Those buttons have been made taller and the three rows repositioned.",
                "Fix: rows in the \"Active recordings\" area did not pick up the theme when created — their buttons kept the default Windows look instead of the accent colour, and a row added while the dark theme was active stayed light until the next theme change.",
            },
            ["1.19.1"] = new[]
            {
                "Fix: the progress bar's state colour (blue while recording, green when finished, red on error, grey when stopped), announced back in 1.5.0, was in fact never displayed — Windows forces its own colour on progress bars as soon as visual styles are enabled, so all four states showed up green. The bar is now drawn by the application itself: the four states are finally distinguishable, and the pulse effect when a recording starts becomes visible too.",
                "The progress bar's track now follows the light or dark theme, instead of staying white on a dark background as a standard Windows bar does.",
            },
            ["1.20.0"] = new[]
            {
                "Automatic stop timer: a new \"Maximum duration\" menu (advanced mode) lets you choose, when starting a recording, how long it should run — 15 minutes, 30 minutes, 1, 2, 4 or 8 hours — after which it stops on its own. The setting applies per recording, like quality or format, and stays on \"Unlimited\" by default.",
                "The remaining time is shown on the row of the recording concerned and updates continuously. Nothing appears for recordings without a timer.",
                "An automatic reconnection does not push the deadline back: a maximum duration of 2 hours means 2 hours of elapsed time, not 2 hours per attempt.",
            },
            ["1.21.0"] = new[]
            {
                "Automatic update check: the application now checks on its own every hour whether a new version is available, instead of waiting for a click on \"Check for updates\".",
                "When a version is found, a notification appears in the notification area; clicking it offers to install, with the same warning as before if recordings are in progress. Each version is announced only once.",
                "New \"Automatically check for updates\" option in Settings, enabled by default: unchecking it immediately disables the only network call the application makes on its own.",
                "A failed automatic check (offline, network outage) no longer displays anything at all and just writes a log line — only the button, where an answer is expected, still reports errors.",
            },
            ["1.22.0"] = new[]
            {
                "Three new buttons in the \"Support the project\" panel: X and Reddit open a pre-filled share message for the project, GitHub opens the repository.",
                "Nothing is posted from the application: it simply opens the relevant network's submission page in the browser, where the message stays editable before sending.",
            },
            ["1.22.1"] = new[]
            {
                "Fix: adding an offline live triggered an error notification every 30 seconds, indefinitely, and it kept coming even after the recording had been cancelled and removed from the list. The 5-attempt reconnection limit in fact never applied, and removing a row left yt-dlp running in the background.",
                "Fix: launching the executable several times opened as many instances, stacking icons in the taskbar. A second launch now brings the already-open window back — useful since the window hides in the notification area instead of closing.",
                "Fix: the \"Website\" button's text was not centred like that of the \"Sponsor\" and \"Donate\" buttons just above it.",
            },
            ["1.22.2"] = new[]
            {
                "The \"Website\" button is no longer flush against the bottom border of the \"Support the project\" panel: the panel was made taller to give it the same margin as the top.",
            },
            ["1.23.0"] = new[]
            {
                "New \"Import favorites\" button, meant to fetch the models you follow on your Chaturbate account. WARNING: the feature does not work as it stands — Chaturbate refuses requests that do not come from a real browser, and the button ends in an error message. It is kept pending a fix; meanwhile, add your favorites with \"+ Favorite\".",
                "The import reuses the cookies file you already configured in Settings: your favorites are private, only a signed-in session can read them, and that file carries it.",
                "Every failure cause has its own message: no cookies file, wrong format, expired session, site unreachable, or page received with no recognisable room. No failure is silent.",
            },
            ["1.23.1"] = new[]
            {
                "Fix: the \"Import favorites\" button added in the previous version did not respond to clicks as soon as the window was widened — it stayed at its original position while the favorites list expanded over it, and clicks went to the list. The import never ran, without any message.",
            },
            ["1.23.2"] = new[]
            {
                "The favorites import now sends the same full set of headers as a real browser, instead of the user-agent alone.",
                "A 403 refusal from Chaturbate now has its own message: it reports the site's anti-bot protection rather than an account or cookie problem — two situations that must not be confused, since the former cannot be fixed on the user's side.",
            },
            ["1.23.3"] = new[]
            {
                "Correction of the 1.23.0 announcement, which presented the favorites import as working: it is not. Two attempts on a real account, with valid cookies, were refused by the site. The corresponding entry has been rectified rather than left as it was.",
            },
            ["1.24.0"] = new[]
            {
                "The \"Import favorites\" button has been removed. The only way to make it work would have been to embed a browser engine to get around Chaturbate's anti-bot protection — an extra dependency imposed on everyone, in order to deliberately circumvent a site protection. That is not the direction intended for this project.",
                "Favorites are added with the \"+ Favorite\" button, as before.",
            },
            ["1.25.0"] = new[]
            {
                "New \"Monitoring\" panel: add a room, and recording starts on its own as soon as it goes online. A notification warns you on every start.",
                "Monitored rooms are checked every 2 minutes by default, adjustable in Settings (1, 2, 5 or 10 minutes). They are checked one after another, never all at once.",
                "A room whose state cannot be determined (network down, banned room) is shown as unknown and never triggers a recording.",
                "The monitored list is deliberately separate from favorites: a favorite is an input shortcut, a monitored room commits the application to polling the site in a loop.",
            },
            ["1.26.0"] = new[]
            {
                "New \"Legality\" button, visible in both simple and advanced mode: it shows a note on the Belgian legal framework — what the software does and does not do, the private-copy exception, and the criminal prohibition on distributing a recording without consent.",
                "The same note is published on the website, in both READMEs and in the wiki, so that it can be found before even installing the application.",
            },
            ["1.26.1"] = new[]
            {
                "The cookies file is now checked when you select it. A file that yt-dlp would reject is reported straight away, with the offending line and what to fix, instead of silently making every recording fail.",
                "This check comes from a real case: a single missing hash before \"HttpOnly_\" invalidated 5 cookies out of 6, including the session one. Every recording failed, and automatic monitoring stayed silent, with nothing pointing at the cookies.",
                "Fix: adding a favorite that was already present showed \"invalid URL or already present\", leaving you to guess which of the two applied. Both cases now have their own message.",
            },
            ["1.27.0"] = new[]
            {
                "Major fix: on a machine without an E: drive, the application showed \"fatal error\" and closed before even appearing. The default folders pointed at a path specific to the developer's machine. They now live in your own user folders: the \"Chaturbate Recorder\" subfolder of your Videos, for recordings.",
                "A save folder that became unreachable (drive unplugged, folder deleted) no longer crashes the application: it warns you, switches to the default folder and carries on.",
                "Logs and crash reports now go to AppData, which always exists and stays writable — that is what prevented the detailed report from being saved when the app crashed at startup.",
                "If you were already recording elsewhere, your folder is kept: only the default value changes.",
            },
            ["1.28.0"] = new[]
            {
                "New installer, downloadable from the GitHub releases: it offers a regular install or a portable version, downloads and verifies yt-dlp and ffmpeg for you, and requires no .NET runtime.",
                "The automatic update now verifies the checksum of the downloaded file before replacing anything. On mismatch, nothing is installed.",
                "Fix: an update that failed (application still open, file locked) restarted the old version without saying anything — you could believe you had updated. The wait goes from 15 seconds to 2 minutes, and any failure is now written to the logs.",
                "After an update, the Windows \"Installed apps\" entry finally shows the right version.",
            },
            ["1.28.1"] = new[]
            {
                "Fix: the previous version's update actually replaced no file at all, while reporting success. Verified this time by running a real end-to-end update.",
                "Fix: the \"Installed apps\" entry was given the old version number instead of the new one, so it stayed unchanged.",
            },
            ["1.29.0"] = new[]
            {
                "The history finally shows thumbnails of your recordings. They had been generated after every capture since version 1.3.0, saved next to the video… and displayed nowhere.",
                "New \"Open file\" button: plays the selected video in your usual player, without going through the explorer.",
                "The history panel is taller and its columns were readjusted: the date is no longer truncated and the horizontal scrollbar is gone.",
            },
            ["1.30.0"] = new[]
            {
                "Degraded mode: when a component is faulty, the application disables it, tells you which one and why, and keeps working — instead of failing later with no explanation.",
                "In practice: a missing ffmpeg no longer prevents recording (only re-encoding and thumbnails are suspended), and an unreadable cookies file is ignored instead of silently making every capture fail.",
                "New \"Features\" section in Settings: you can turn off re-encoding, cookies, proxy, simultaneous recordings or monitoring yourself — handy to isolate a problem.",
                "Re-enabling a feature by hand also clears the automatic shutdown: the check will run again on next startup.",
            },
            ["1.31.0"] = new[]
            {
                "Redesigned interface: panels are now real cards sitting on the window background, with a soft shadow and their title inside — instead of a thin outline that made the whole screen look flat.",
                "One coloured button per area: \"Start\" and \"Sponsor\" stand out, everything else becomes a neutral button. Before, eighteen blue buttons competed for attention.",
                "Anything that interrupts or deletes (Stop, Stop all, Remove favorite, Stop monitoring) is now recognisable by its discreet red.",
                "Lists and fields finally follow the dark theme: no more white column header above a black list, and no more light border around text fields and drop-down menus.",
                "Field labels step back visually, and a recording's percentage is shown as a whole number: the decimal changed several times a second without telling anyone anything.",
                "Button labels are genuinely centred together with their icon, whatever the button width.",
            },
            ["1.32.0"] = new[]
            {
                "Three new platforms: Twitch, YouTube and TikTok. Paste the stream address just as you would for Chaturbate — the field is now called \"Stream URL\".",
                "Monitoring can now tell you that a channel does not exist, instead of waiting forever on a mistyped address.",
                "On YouTube, only actual live streams start a recording: an already published video is no longer mistaken for a live one.",
                "YouTube recordings are finally named after the channel or the video, instead of all being called \"watch\".",
                "The legality note now covers the new platforms: on non-adult ones the issue is copyright, and breaching terms of service remains contractual, not criminal.",
            },
            ["1.33.0"] = new[]
            {
                "The supported platforms are shown next to the address field, and every monitored row carries the icon of its own.",
                "Fixed: in the history, size, duration and date disappeared until the next click.",
                "Fixed: the corners of the drop-down lists briefly changed appearance as the mouse passed over them.",
                "Fixed: the buttons in the getting started guide were badly drawn since the interface redesign.",
                "The getting started guide and the Diagnostics window finally follow the dark theme, instead of opening in light.",
                "List scrollbars are no longer white in dark theme.",
            },
            ["1.34.0"] = new[]
            {
                "New \"Thanks\" button in the \"Support the project\" panel: it lists the people whose donations support the project, and never mentions an amount.",
                "The list of donors is also fetched from the project website when the window opens, so a name can be added without waiting for a new version. With no connection, the built-in list is shown.",
                "A name is only added with that person's consent, and the list is sorted alphabetically so its order cannot be read as a ranking.",
            },
        };

        /// <summary>
        /// Retourne les nouveautés d'une version dans la langue demandée, avec
        /// repli sur le français si cette version n'a pas de traduction.
        /// Prend un bool plutôt qu'un AppLanguage à dessein : Config est la
        /// couche basse (référencée par Security/Services) et n'a pas à
        /// dépendre de UI.
        /// </summary>
        public static string[] GetChanges(string version, bool english)
        {
            if (english && EnglishChanges.TryGetValue(version, out var translated))
                return translated;

            foreach (var entry in Entries)
                if (entry.Version == version)
                    return entry.Changes;

            return System.Array.Empty<string>();
        }

        /// <summary>
        /// Nouveautés à annoncer à quelqu'un qui passe de
        /// <paramref name="sinceVersion"/> (dernière version qu'il a réellement
        /// vue) à <paramref name="upToVersion"/> : toutes les entrées
        /// strictement postérieures à la première et jusqu'à la seconde
        /// incluse, de la plus récente à la plus ancienne.
        ///
        /// N'afficher que l'entrée de la version courante perdait tout
        /// l'intermédiaire, et ce n'est pas théorique : rien n'oblige un
        /// utilisateur à installer chaque release, et le projet en publie
        /// souvent plusieurs de suite. Qui mettait à jour depuis la 1.14.0
        /// n'apprenait jamais l'existence du Crash Reporter (1.15.0) ni du mode
        /// Diagnostic (1.16.0), pourtant présents dans le binaire qu'il venait
        /// d'installer.
        ///
        /// Repli volontaire vers la seule entrée de <paramref name="upToVersion"/>
        /// quand la plage est vide : borne basse absente ou illisible
        /// (settings.json édité à la main) et retour à une version antérieure.
        /// Déverser tout l'historique dans ces cas-là serait pire que de ne
        /// rien dire. Tableau vide seulement quand ce repli ne trouve rien non
        /// plus, c'est-à-dire quand upToVersion n'a aucune entrée — noter
        /// qu'une version installée sans entrée annonce quand même ce qui la
        /// précède tant que la borne basse, elle, est exploitable.
        ///
        /// Chaque version est traduite indépendamment : une annonce couvrant
        /// des versions d'avant et d'après 1.16.0 est donc légitimement mixte
        /// en anglais (voir le commentaire de classe).
        /// </summary>
        public static (string Version, string[] Changes)[] GetChangesSince(
            string? sinceVersion, string upToVersion, bool english)
        {
            var announced = Entries
                .Where(e => IsAnnounced(e.Version, sinceVersion, upToVersion))
                .OrderByDescending(e => e.Version, VersionOrder)
                .ToArray();

            if (announced.Length == 0)
                announced = Entries.Where(e => e.Version == upToVersion).ToArray();

            return announced
                .Select(e => (e.Version, Changes: GetChanges(e.Version, english)))
                .ToArray();
        }

        private static bool IsAnnounced(string version, string? sinceVersion, string upToVersion)
        {
            // Une borne illisible ne doit pas ouvrir la plage en grand : on
            // laisse GetChangesSince retomber sur la seule version courante.
            if (!System.Version.TryParse(sinceVersion, out _)) return false;
            if (!System.Version.TryParse(upToVersion, out _)) return false;

            return CompareVersions(version, sinceVersion) > 0
                && CompareVersions(version, upToVersion) <= 0;
        }

        private static readonly IComparer<string> VersionOrder =
            Comparer<string>.Create((a, b) => CompareVersions(a, b));

        /// <summary>
        /// Comparaison numérique de versions — "1.9.0" est antérieure à
        /// "1.10.0", ce qu'une comparaison de chaînes affirme exactement à
        /// l'envers. Une version illisible est classée avant toutes les autres
        /// plutôt que de faire lever la comparaison.
        /// Exposé aux tests : voir ChangelogTests.
        /// </summary>
        internal static int CompareVersions(string? a, string? b)
        {
            var parsedA = System.Version.TryParse(a, out var va) ? va : null;
            var parsedB = System.Version.TryParse(b, out var vb) ? vb : null;

            if (parsedA == null) return parsedB == null ? 0 : -1;
            if (parsedB == null) return 1;

            return parsedA.CompareTo(parsedB);
        }

        /// <summary>Exposé aux tests : voir ChangelogTests.</summary>
        internal static IReadOnlyDictionary<string, string[]> AllEnglishChanges => EnglishChanges;
    }
}
