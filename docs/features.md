---
layout: default
title: Fonctionnalités
---

<link rel="stylesheet" href="assets/lang-toggle.css">
<button id="langToggle" class="lang-toggle-btn" type="button"></button>

<div class="lang-fr" markdown="1">

# Fonctionnalités

Détail des mécanismes internes de Chaturbate Recorder. Pour une vue
d'ensemble plus courte, voir la [page d'accueil](index.html).

## 🧱 Sandbox

- **Chemins de fichiers** : interdiction des chemins UNC, des chemins étendus
  (`\\?\`, `\\.\`), de l'espace de noms `\Device\`, des flux ADS, des noms
  réservés Windows, et des symlinks/reparse points — sur le chemin final
  comme sur chaque dossier parent.
- **URLs de live** : validation stricte du schéma, de l'hôte, des segments
  et de la query string (schémas `javascript`/`file`/`ftp`/`data`/`blob`
  bloqués).
- **Dossier d'exécution** : l'application refuse de tourner depuis un
  partage réseau (UNC ou lecteur mappé), un dossier temporaire/éphémère
  (`%TEMP%`, Téléchargements, Bureau, Corbeille), ou un dossier compressé.

## 🔒 Sécurité

- **Intégrité des binaires externes** (`yt-dlp.exe`, `ffmpeg.exe`) : hash
  SHA256, signature Authenticode, chaîne de certification, et pinning CA
  optionnel avant chaque lancement.
- **ACL permissives** : détection si un groupe largement partagé (Everyone,
  BUILTIN\Users, Authenticated Users) dispose d'un droit d'écriture sur un
  dossier sensible — un autre compte local pourrait sinon substituer les
  binaires.
- **TLS du serveur distant** : vérification explicite du certificat et de
  son SAN (Subject Alternative Name), décodé directement en ASN.1 plutôt
  que via une API dont la sortie dépend de la langue du système.

## 📝 Logs

- Un objet JSON structuré par ligne (JSONL), dans un fichier de session
  horodaté par jour — facile à parser ou à agréger.
- Rotation et purge automatiques des logs les plus anciens.
- Affichage en direct dans l'interface en plus de l'écriture sur disque.

## 🎨 Interface

- Thème clair/sombre avec transition de couleur animée, palette Windows 11.
- Mode simple/avancé (les options qualité/codec/format restent visibles
  dans les deux, ce sont des choix par enregistrement).
- Fenêtre **Paramètres** séparée : thème, langue, dossier de sauvegarde,
  cookies, proxy, reconnexion automatique par défaut.
- Sélecteur de langue Français/English pour l'interface principale.
- Réduction dans la zone de notification (au lieu de fermer l'app) pour
  laisser un enregistrement en cours se terminer en arrière-plan.
- Icônes vectorielles (rendu SVG), notifications toast, barre de
  progression animée.

## 🕒 Historique

- Liste des enregistrements passés : nom, taille, durée, date.
- Chaque miniature/réencodage est associé à son fichier vidéo par un nom
  de sortie exact fixé au démarrage de l'enregistrement — pas par
  heuristique "fichier le plus récent", qui devient ambiguë dès qu'un même
  salon est enregistré plusieurs fois.
- Favoris pour relancer un enregistrement en un clic.

## 🔄 Vérification des mises à jour

- Recherche automatique toutes les heures, en arrière-plan : une notification
  cliquable annonce la nouvelle version, sans jamais interrompre un
  enregistrement en cours. Chaque version n'est signalée qu'une fois, et le
  réglage se désactive dans les Paramètres (c'est le seul appel réseau que
  l'application effectue d'elle-même).
- Bouton "Rechercher une mise à jour" qui interroge l'API GitHub Releases
  (aucune authentification requise, dépôt public) et propose l'installation
  automatique si une version plus récente existe.
- Chaque release attache deux variantes (standard/portable) ; l'application
  détecte celle qu'elle exécute actuellement pour toujours télécharger la
  bonne, même quand les deux sont proposées.
- Le remplacement des fichiers se fait via un script détaché (Windows ne
  permet pas à un processus de remplacer son propre `.exe` pendant qu'il
  tourne), qui relance l'application une fois la mise à jour copiée.

## 🐕 Watchdog anti-freeze

- Si `yt-dlp`/`ffmpeg` ne produisent plus aucune ligne de sortie pendant un
  délai configurable (120 secondes par défaut), le processus est considéré
  figé et tué proprement (arbre de processus complet), avec un log explicite.
- Tourne hors du thread d'interface : un blocage du processus externe ne
  gèle jamais l'application.
- Un enregistrement arrêté par le watchdog est marqué "Échoué" plutôt que
  "Arrêté", pour le distinguer d'un arrêt volontaire.

---

[← Retour à l'accueil](index.html) · [Captures d'écran](screenshots.html) · [Roadmap](roadmap.html) · [SentinelGuard](sentinelguard.html)

</div>

<div class="lang-en" markdown="1" style="display:none">

# Features

Details of Chaturbate Recorder's internal mechanisms. For a shorter
overview, see the [home page](index.html).

## 🧱 Sandbox

- **File paths**: UNC paths, extended-length paths (`\\?\`, `\\.\`), the
  `\Device\` namespace, ADS streams, reserved Windows names, and
  symlinks/reparse points are all blocked — on the final path as well as
  on every parent directory.
- **Live URLs**: strict validation of scheme, host, segments, and query
  string (the `javascript`/`file`/`ftp`/`data`/`blob` schemes are
  blocked).
- **Execution directory**: the app refuses to run from a network share
  (UNC or mapped drive), a temporary/ephemeral folder (`%TEMP%`,
  Downloads, Desktop, Recycle Bin), or a compressed folder.

## 🔒 Security

- **External binary integrity** (`yt-dlp.exe`, `ffmpeg.exe`): SHA256 hash,
  Authenticode signature, certificate chain, and optional CA pinning
  before every launch.
- **Permissive ACLs**: detects if a broadly shared group (Everyone,
  BUILTIN\Users, Authenticated Users) has write access to a sensitive
  folder — another local account could otherwise substitute the binaries.
- **Remote server TLS**: explicit verification of the certificate and its
  SAN (Subject Alternative Name), decoded directly as ASN.1 rather than
  through an API whose output depends on the system's language.

## 📝 Logs

- One structured JSON object per line (JSONL), in a session file
  timestamped per day — easy to parse or aggregate.
- Automatic rotation and purging of the oldest logs.
- Live display in the UI in addition to being written to disk.

## 🎨 Interface

- Light/dark theme with an animated color transition, Windows 11 palette.
- Simple/advanced mode (quality/codec/format options stay visible in
  both, since they're per-recording choices).
- Separate **Settings** window: theme, language, save folder, cookies,
  proxy, default auto-reconnect.
- French/English language selector for the main interface.
- Minimizing to the notification area instead of closing the app, so an
  ongoing recording keeps running in the background.
- Vector icons (SVG rendering), toast notifications, animated progress
  bar.

## 🕒 History

- List of past recordings: name, size, duration, date.
- Each thumbnail/re-encode is matched to its video file by an exact
  output name fixed at the start of the recording — not by a "most
  recent file" heuristic, which becomes ambiguous as soon as the same
  room is recorded more than once.
- Favorites to relaunch a recording in one click.

## 🔄 Update checking

- Automatic hourly check in the background: a clickable notification
  announces the new version without ever interrupting an ongoing
  recording. Each version is announced only once, and the option can be
  turned off in Settings (it is the only network call the application
  makes on its own).
- "Check for updates" button that queries the GitHub Releases API (no
  authentication needed, public repo) and offers automatic installation
  if a newer version exists.
- Each release attaches two variants (standard/portable); the app
  detects which one it's currently running to always download the
  matching one, even when both are offered.
- File replacement happens through a detached script (Windows doesn't
  let a process replace its own `.exe` while it's running), which
  relaunches the app once the update has been copied.

## 🐕 Anti-freeze watchdog

- If `yt-dlp`/`ffmpeg` stop producing any output line for a configurable
  delay (120 seconds by default), the process is considered frozen and
  killed cleanly (full process tree), with an explicit log entry.
- Runs off the UI thread: a hang in the external process never freezes
  the app.
- A recording stopped by the watchdog is marked "Failed" rather than
  "Stopped", to distinguish it from a voluntary stop.

---

[← Home](index.html) · [Screenshots](screenshots.html) · [Roadmap](roadmap.html) · [SentinelGuard](sentinelguard.html)

</div>

<script src="assets/lang-toggle.js"></script>
