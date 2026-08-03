---
layout: default
title: Fonctionnalités
---

# Fonctionnalités

Détail des mécanismes internes de Chaturbate Recorder. Pour une vue
d'ensemble plus courte, voir la [page d'accueil](./).

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

[← Retour à l'accueil](index.html) · [Captures d'écran](screenshots.html) · [Roadmap](roadmap.html)
