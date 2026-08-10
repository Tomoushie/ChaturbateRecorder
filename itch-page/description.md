# Fiche itch.io — texte à copier

Page : <https://tomoushie.itch.io/chaturbate-recorder>

Ce fichier est la **source** du texte de la fiche. itch.io n'a pas d'API pour
la description : elle se colle à la main dans l'éditeur. Le garder ici évite de
la réécrire de mémoire à chaque refonte, et de laisser diverger le site,
le README et la fiche.

**Une seule fiche pour les deux langues** : itch.io ne gère pas le
multilingue. Le français d'abord — le public visé est francophone et le
mainteneur l'est — puis l'anglais sous un séparateur.

---

## Champ « Short description or tagline »

Limité à environ 140 caractères, affiché sous le titre et dans les résultats.

```
Enregistre les lives de Chaturbate, Twitch, YouTube et TikTok. Gratuit, open source, sans publicité ni collecte de données.
```

---

## Champ « Details » (corps de la fiche)

### Version à coller

**Chaturbate Recorder** enregistre les diffusions en direct sur votre disque,
sans passer par un service tiers et sans créer de compte.

**Ce qu'il fait**

- Enregistre depuis **Chaturbate, Twitch, YouTube et TikTok**
- **Plusieurs enregistrements en parallèle**, dans une seule fenêtre
- **Surveille des salons** et démarre tout seul dès que l'un passe en ligne
- Qualité, codec (copie / H.264 / H.265), format (MP4 / MKV / MOV) et durée
  maximale réglables **par enregistrement**
- Historique avec miniatures, favoris, thème clair et sombre, français et
  anglais

**Ce qu'il ne fait pas**

- Aucune publicité, aucun compte, aucune télémétrie, aucune collecte
- Rien n'est envoyé nulle part : les fichiers restent chez vous

**Sécurité**

L'application lance yt-dlp et ffmpeg, et le prend au sérieux : leur empreinte
SHA-256 et leur signature sont vérifiées avant chaque lancement, les chemins et
les adresses sont validés, et l'application refuse de tourner depuis un dossier
réseau, temporaire ou compressé. Ces contrôles sont publiés séparément en
paquet NuGet réutilisable, SentinelGuard.

**Installation**

Téléchargez `setup.exe` et lancez-le. Il installe l'application, yt-dlp et
ffmpeg, vérifie leurs empreintes, et ne demande aucun droit administrateur.
Il propose au premier écran l'installation classique ou un déploiement
portable.

Windows affichera « Éditeur inconnu » : le projet n'a pas de certificat de
signature payant. Cliquez sur *Informations complémentaires* puis
*Exécuter quand même*.

La variante portable ne contient ni yt-dlp ni ffmpeg — la licence GPL de ffmpeg
interdit de le redistribuer ici. Placez-les vous-même à côté de l'exécutable,
ou prenez `setup.exe`, qui s'en charge.

**Légalité**

Ce logiciel enregistre un flux que vous êtes déjà autorisé à regarder.
Enregistrer pour un usage privé n'est pas diffuser : rediffuser, republier ou
revendre un enregistrement relève du droit d'auteur et, selon les plateformes,
du droit à l'image des personnes filmées. Enfreindre les conditions
d'utilisation d'un site est une inexécution contractuelle, pas une infraction
pénale. À vous de rester du bon côté.

**Gratuit, et le restant**

Le code est ouvert sous double licence MIT / Apache 2.0. Aucune fonctionnalité
n'est réservée à qui donne : l'application est identique pour tout le monde.

Code source, documentation et signalement de bugs :
<https://github.com/Tomoushie/ChaturbateRecorder>

---

**Chaturbate Recorder** records live streams to your own disk, with no
third-party service and no account.

**What it does**

- Records from **Chaturbate, Twitch, YouTube and TikTok**
- **Several recordings at once**, in a single window
- **Monitors rooms** and starts on its own as soon as one goes live
- Quality, codec (copy / H.264 / H.265), format (MP4 / MKV / MOV) and maximum
  duration, all set **per recording**
- History with thumbnails, favourites, light and dark themes, French and
  English

**What it does not do**

- No ads, no account, no telemetry, no data collection
- Nothing is sent anywhere: the files stay with you

**Security**

The application runs yt-dlp and ffmpeg, and takes that seriously: their SHA-256
checksum and signature are verified before every launch, paths and URLs are
validated, and the application refuses to run from a network, temporary or
compressed folder. These checks ship separately as a reusable NuGet package,
SentinelGuard.

**Installing**

Download `setup.exe` and run it. It installs the application, yt-dlp and
ffmpeg, verifies their checksums, and needs no administrator rights. Its first
screen offers either a classic install or a portable deployment.

Windows will say "Unknown publisher": the project has no paid signing
certificate. Click *More info*, then *Run anyway*.

The portable build contains neither yt-dlp nor ffmpeg — ffmpeg's GPL licence
means it is not redistributed here. Place them next to the executable yourself,
or take `setup.exe`, which handles it.

**Legality**

This software records a stream you are already allowed to watch. Recording for
private use is not broadcasting: re-streaming, republishing or selling a
recording falls under copyright and, depending on the platform, the image
rights of the people filmed. Breaking a site's terms of service is a breach of
contract, not a criminal offence. Staying on the right side is up to you.

**Free, and staying that way**

The code is open source under a dual MIT / Apache 2.0 licence. No feature is
ever reserved for those who donate: the application is the same for everyone.

Source code, documentation and bug reports:
<https://github.com/Tomoushie/ChaturbateRecorder>

---

## Réglages de la fiche

| Champ | Valeur |
| --- | --- |
| Kind of project | **Downloadable** — sinon itch.io attend un jeu HTML et refuse les fichiers |
| Classification | **Tool** |
| Pricing | **No payments** (ou *Donation* si tu veux un bouton de soutien) |
| Platforms | Windows |
| Cover image | `itch-cover.png` — 630×500, le format qu'itch.io impose pour l'affichage en liste |
| Screenshots | `screenshots/` de ce dossier |
| Tags suggérés | `recorder`, `streaming`, `twitch`, `youtube`, `open-source`, `windows`, `utility` |
| Community | À ton choix — sinon les retours passent déjà par le bouton « Signaler un bug » de l'application |

**Contenu adulte** : le logiciel n'en contient pas, c'est un utilitaire. Mais
son nom porte celui d'une plateforme adulte, et c'est la modération d'itch.io
qui tranche. Si la fiche est signalée, la réponse à donner est celle-ci : aucun
contenu n'est distribué, seul un outil de capture l'est.
