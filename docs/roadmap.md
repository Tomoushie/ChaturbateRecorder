---
layout: default
title: Roadmap
---

# Roadmap

État d'avancement du projet — voir aussi le
[changelog complet des releases](https://github.com/Tomoushie/ChaturbateRecorder/releases)
et le [tableau de suivi GitHub](https://github.com/users/Tomoushie/projects/2).

## ✅ Déjà en place

- **Sécurité** : sandbox de chemins/URLs, vérification d'intégrité des
  binaires externes, détection d'ACL permissives, TLS/SAN du serveur
  distant — voir la [page Fonctionnalités](features.html).
- **Robustesse** : watchdog anti-freeze, traitement `ffmpeg`/`yt-dlp` hors
  thread d'interface, logs JSON structurés avec rotation.
- **Interface modernisée** : thème clair/sombre animé, mode simple/avancé,
  notifications toast, icônes vectorielles, fenêtre Paramètres dédiée.
- **Multi-langue** : sélecteur Français/English pour l'interface
  principale.
- **Mises à jour intégrées** : vérification et installation automatique
  des nouvelles releases GitHub.
- **CI/CD** : build + tests automatiques, publication de release
  automatisée (build, ZIP standard/portable, upload), analyse de sécurité
  (CodeQL + audit des dépendances), déploiement automatique de ce site.
- **Distribution** : releases au format standard (nécessite le runtime
  .NET) et portable (autonome, single-file).
- **Package NuGet** : les validateurs de sécurité sont aussi disponibles
  séparément dans
  [`ChaturbateRecorder.Security`](https://github.com/Tomoushie/ChaturbateRecorder/pkgs/nuget/ChaturbateRecorder.Security).

## 🚧 Prévu

- **Portage macOS** de l'application.
- **Extension navigateur** (déjà disponible sur Windows) portée sur
  macOS/Safari.
- **Installateur** Windows avec étapes d'installation graphiques, en plus
  du format portable actuel.

## ⛔ Écarté ou bloqué

- **Signature Authenticode de l'exécutable** : nécessite un certificat de
  signature de code, non disponible actuellement — aucune solution
  purement logicielle possible.
- **NativeAOT** : non supporté par WinForms à ce jour, ce n'est pas un
  choix de configuration mais une limitation du framework.
- **Palette de couleurs pastel** : la palette bleu Windows 11 actuelle a
  été délibérément conservée.

---

[← Retour à l'accueil](index.html) · [Fonctionnalités](features.html) · [Captures d'écran](screenshots.html)
