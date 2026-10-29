using System.Runtime.CompilerServices;

// Permet au projet de tests d'atteindre les membres `internal`.
//
// Plusieurs règles du projet sont délibérément `internal` PARCE QU'ELLES SONT
// TESTABLES sans être publiques : la table de chaînes complète
// (`Localization.AllStrings`), l'ordre de version du changelog
// (`Changelog.CompareVersions`), la traduction d'un rôle de bouton en couleurs
// (`ThemeManager.ResolveButtonColors`). Les exposer en `public` pour les tester
// les offrirait aussi au reste du code, qui n'a aucune raison de les appeler.
//
// Le nom doit correspondre à l'AssemblyName du projet de tests, pas au nom du
// fichier .csproj.
[assembly: InternalsVisibleTo("ChaturbateRecorderApp.Wpf.Tests")]
