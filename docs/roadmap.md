---
layout: default
title: Roadmap
---

<link rel="stylesheet" href="assets/lang-toggle.css">
<button id="langToggle" class="lang-toggle-btn" type="button"></button>

<div class="lang-fr" markdown="1">

# Roadmap

Ce que le projet fait déjà, ce qui est prévu, et ce qui ne se fera pas — avec
la raison dans chaque cas. Voir aussi le
[changelog complet des releases](https://github.com/Tomoushie/ChaturbateRecorder/releases)
et le [tableau de suivi GitHub](https://github.com/users/Tomoushie/projects/2).

## ✅ Déjà en place

- **Quatre plateformes** : Chaturbate, Twitch, YouTube et TikTok. Colle
  l'adresse du live, celle que tu utiliserais dans ton navigateur.
- **Installateur Windows** : `setup.exe` télécharge l'application, yt-dlp et
  ffmpeg, vérifie leurs empreintes, et n'exige aucun droit administrateur. Le
  format portable reste disponible.
- **Enregistrement** : plusieurs lives en parallèle, choix de la qualité, du
  codec et du conteneur, réencodage optionnel sans jamais toucher au fichier
  d'origine, minuteur d'arrêt par enregistrement, reconnexion automatique.
- **Surveillance** : une liste de salons contrôlée à intervalle régulier,
  l'enregistrement démarrant dès qu'un live commence.
- **Mode dégradé** : un composant défaillant est désactivé et signalé au
  démarrage plutôt que de faire échouer une capture plus tard.
- **Sécurité** : empreintes et signatures des binaires externes, bac à sable
  de chemins et d'URL, détection d'ACL permissives, épinglage TLS, contrôle du
  dossier d'exécution — voir la [page Fonctionnalités](features.html).
- **Interface** : thème clair/sombre, cartes, mode simple/avancé, notifications,
  historique avec miniatures, fenêtre Paramètres dédiée, bilingue FR/EN.
- **Mises à jour intégrées** : vérification automatique et installation, avec
  contrôle d'intégrité de l'archive téléchargée.
- **CI/CD** : build et tests automatiques, releases publiées de bout en bout,
  analyse de sécurité (CodeQL, audit des dépendances), SBOM attaché à chaque
  release, déploiement automatique de ce site.
- **Package NuGet** : les garde-fous de sécurité et la supervision de processus
  sont réutilisables séparément dans
  [`SentinelGuard`](https://www.nuget.org/packages/SentinelGuard).

## 🚧 Prévu

- **Mode ligne de commande** pour automatiser un enregistrement sans ouvrir la
  fenêtre (tâches planifiées, scripts).
- **Signalement de bug depuis l'application**, sans compte GitHub.
- **Système de modules** pour ajouter une plateforme sans toucher au cœur du
  logiciel.
- **Portage macOS**, puis **extension navigateur** détectant les sources
  compatibles.

## ⛔ Écarté, et pourquoi

- **Instagram** : mesuré, pas supposé — sans session authentifiée, le site
  redirige vers sa page de connexion et yt-dlp ne peut rien en tirer. La prise
  en charge attend de pouvoir être éprouvée contre un vrai direct.
- **Signature Authenticode de l'exécutable** : nécessite un certificat de
  signature de code payant. Aucune solution purement logicielle n'existe, d'où
  l'avertissement « Éditeur inconnu » de Windows.
- **NativeAOT** : non supporté par WinForms. C'est une limite du framework, pas
  un choix de configuration.
- **Import des favoris du compte** : retiré en v1.24.0. La seule voie technique
  restante revenait à contourner une protection anti-robots du site, ce que le
  projet refuse de faire.
- **Palette pastel** : la palette bleu Windows 11 a été délibérément conservée.

---

[← Retour à l'accueil](index.html) · [Fonctionnalités](features.html) · [Captures d'écran](screenshots.html) · [SentinelGuard](sentinelguard.html)

</div>

<div class="lang-en" markdown="1" style="display:none">

# Roadmap

What the project already does, what is planned, and what will not happen — with
the reason in each case. See also the
[full release changelog](https://github.com/Tomoushie/ChaturbateRecorder/releases)
and the [GitHub tracking board](https://github.com/users/Tomoushie/projects/2).

## ✅ Already in place

- **Four platforms**: Chaturbate, Twitch, YouTube and TikTok. Paste the stream
  address, the same one you would use in your browser.
- **Windows installer**: `setup.exe` downloads the application, yt-dlp and
  ffmpeg, verifies their checksums, and needs no administrator rights. The
  portable format is still available.
- **Recording**: several lives in parallel, choice of quality, codec and
  container, optional re-encoding that never touches the original file, a stop
  timer per recording, automatic reconnection.
- **Monitoring**: a list of rooms checked at a regular interval, with recording
  starting as soon as a stream goes live.
- **Degraded mode**: a faulty component is disabled and reported at startup,
  rather than making a capture fail later on.
- **Security**: checksums and signatures of external binaries, path and URL
  sandboxing, permissive-ACL detection, TLS pinning, execution-location checks
  — see the [Features page](features.html).
- **Interface**: light/dark theme, cards, simple/advanced mode, notifications,
  history with thumbnails, dedicated Settings window, bilingual FR/EN.
- **Built-in updates**: automatic checking and installation, with an integrity
  check on the downloaded archive.
- **CI/CD**: automatic build and tests, end-to-end release publishing, security
  analysis (CodeQL, dependency audit), an SBOM attached to every release,
  automatic deployment of this site.
- **NuGet package**: the security guardrails and the process supervision are
  separately reusable through
  [`SentinelGuard`](https://www.nuget.org/packages/SentinelGuard).

## 🚧 Planned

- **Command-line mode** to automate a recording without opening the window
  (scheduled tasks, scripts).
- **Bug reporting from inside the application**, with no GitHub account.
- **A module system** to add a platform without touching the core of the
  software.
- **macOS port**, then a **browser extension** detecting compatible sources.

## ⛔ Dropped, and why

- **Instagram**: measured, not assumed — without an authenticated session the
  site redirects to its login page and yt-dlp can do nothing with it. Support
  waits until it can be tested against a real live stream.
- **Authenticode signing of the executable**: requires a paid code-signing
  certificate. No software-only solution exists, hence the "Unknown publisher"
  warning from Windows.
- **NativeAOT**: not supported by WinForms. A framework limitation, not a
  configuration choice.
- **Importing account favourites**: removed in v1.24.0. The only remaining
  technical route amounted to circumventing the site's bot protection, which
  this project refuses to do.
- **Pastel colour palette**: the Windows 11 blue palette was deliberately kept.

---

[← Home](index.html) · [Features](features.html) · [Screenshots](screenshots.html) · [SentinelGuard](sentinelguard.html)

</div>

<script src="assets/lang-toggle.js"></script>
