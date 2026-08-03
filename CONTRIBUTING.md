# Contribuer

🇫🇷 Français (cette section) · 🇬🇧 [English](#contributing)

Le projet est ouvert aux contributions via pull request. Guide plus
détaillé (avec captures/exemples) sur la page
[Contribuer du wiki](https://github.com/Tomoushie/ChaturbateRecorder/wiki/Contribuer).

## Signaler un bug

Depuis l'application (mode avancé) : bouton **Signaler un bug** — ouvre
un ticket GitHub pré-rempli avec version/système/dossier de capture.
Sinon, [nouveau ticket](https://github.com/Tomoushie/ChaturbateRecorder/issues/new)
directement. **Pour une faille de sécurité, voir [SECURITY.md](SECURITY.md)
plutôt qu'un ticket public.**

## Proposer un changement de code

Prérequis : [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows).
Voir la section **Pour les développeurs** du [README](README.md#pour-les-développeurs)
pour le build, les tests et la structure du projet.

Avant de proposer un changement :

1. `dotnet build` et `dotnet test Tests/ChaturbateRecorderApp.Tests.csproj`
   doivent passer sans erreur.
2. Pas de reformattage massif du code existant dans une PR qui touche
   autre chose — les commentaires expliquent le **pourquoi**, pas le
   quoi.
3. Pour un point explicitement hors périmètre actuel (signature
   Authenticode, NativeAOT, traduction complète de l'UI en anglais...),
   ouvre une [issue](https://github.com/Tomoushie/ChaturbateRecorder/issues/new)
   pour en discuter avant de coder.

## Discussions

Pour une question, une idée ou un retour qui n'est pas un bug précis,
utilise plutôt les [Discussions](https://github.com/Tomoushie/ChaturbateRecorder/discussions)
que les issues.

---

# Contributing

🇬🇧 English (this section) · 🇫🇷 [Français](#contribuer)

The project is open to contributions via pull request. A more detailed
guide (with examples) is on the wiki's
[Contributing page](https://github.com/Tomoushie/ChaturbateRecorder/wiki/Contribuer-EN).

## Reporting a bug

From the app (advanced mode): **Report a bug** button — opens a
pre-filled GitHub issue with version/system/capture folder. Otherwise,
[open a new issue](https://github.com/Tomoushie/ChaturbateRecorder/issues/new)
directly. **For a security vulnerability, see [SECURITY.md](SECURITY.md)
instead of a public issue.**

## Proposing a code change

Prerequisite: [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows).
See the **For developers** section of the [README](README.en.md#for-developers)
for build, tests, and project structure.

Before proposing a change:

1. `dotnet build` and `dotnet test Tests/ChaturbateRecorderApp.Tests.csproj`
   must pass without errors.
2. No massive reformatting of existing code in a PR that touches
   something else — comments explain the **why**, not the what.
3. For anything explicitly out of current scope (Authenticode signing,
   NativeAOT, full English translation of the UI...), open an
   [issue](https://github.com/Tomoushie/ChaturbateRecorder/issues/new)
   to discuss it before coding.

## Discussions

For a question, idea, or feedback that isn't a specific bug, use
[Discussions](https://github.com/Tomoushie/ChaturbateRecorder/discussions)
rather than issues.
