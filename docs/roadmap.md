---
layout: default
title: Roadmap
---

<link rel="stylesheet" href="assets/lang-toggle.css">
<button id="langToggle" class="lang-toggle-btn" type="button"></button>

<div class="lang-fr" markdown="1">

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
  [`SentinelGuard`](https://www.nuget.org/packages/SentinelGuard).

## 🚧 Prévu

- **Portage macOS** de l'application.
- **Extension navigateur** détectant automatiquement les sources
  compatibles pour les intégrer au logiciel, puis portée sur macOS/Safari
  une fois disponible sur Windows.
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

[← Retour à l'accueil](index.html) · [Fonctionnalités](features.html) · [Captures d'écran](screenshots.html) · [SentinelGuard](sentinelguard.html)

</div>

<div class="lang-en" markdown="1" style="display:none">

# Roadmap

Project progress — see also the
[full release changelog](https://github.com/Tomoushie/ChaturbateRecorder/releases)
and the [GitHub tracking board](https://github.com/users/Tomoushie/projects/2).

## ✅ Already in place

- **Security**: path/URL sandboxing, external binary integrity
  verification, permissive-ACL detection, remote server TLS/SAN — see the
  [Features page](features.html).
- **Robustness**: anti-freeze watchdog, `ffmpeg`/`yt-dlp` handling off the
  UI thread, structured JSON logs with rotation.
- **Modernized interface**: animated light/dark theme, simple/advanced
  mode, toast notifications, vector icons, dedicated Settings window.
- **Multi-language**: French/English selector for the main interface.
- **Built-in updates**: automatic checking and installation of new GitHub
  releases.
- **CI/CD**: automatic build + tests, automated release publishing
  (build, standard/portable ZIP, upload), security analysis (CodeQL +
  dependency audit), automatic deployment of this site.
- **Distribution**: releases in standard format (requires the .NET
  runtime) and portable format (self-contained, single-file).
- **NuGet package**: the security validators are also available
  separately in
  [`SentinelGuard`](https://www.nuget.org/packages/SentinelGuard).

## 🚧 Planned

- **macOS port** of the application.
- **Browser extension** auto-detecting compatible sources to feed into
  the app, later ported to macOS/Safari once available on Windows.
- Windows **installer** with graphical setup steps, in addition to the
  current portable format.

## ⛔ Dropped or blocked

- **Authenticode signing of the executable**: requires a code-signing
  certificate, not currently available — no purely software-side
  solution exists.
- **NativeAOT**: not supported by WinForms at this time — a framework
  limitation, not a configuration choice.
- **Pastel color palette**: the current Windows 11 blue palette was
  deliberately kept instead.

---

[← Home](index.html) · [Features](features.html) · [Screenshots](screenshots.html) · [SentinelGuard](sentinelguard.html)

</div>

<script src="assets/lang-toggle.js"></script>
