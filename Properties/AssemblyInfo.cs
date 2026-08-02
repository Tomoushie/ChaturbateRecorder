using System.Runtime.CompilerServices;

// Permet au projet de tests d'accéder aux membres `internal` (ex: la logique
// de correspondance SAN de CertificateValidator, testable sans connexion
// réseau réelle via un certificat auto-signé généré en mémoire).
[assembly: InternalsVisibleTo("ChaturbateRecorderApp.Tests")]
