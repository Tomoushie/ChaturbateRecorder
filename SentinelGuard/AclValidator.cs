using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SentinelGuard
{
    /// <summary>
    /// Détecte si un groupe largement partagé (Everyone, BUILTIN\Users,
    /// Authenticated Users) dispose d'un droit d'écriture sur un dossier
    /// donné — un autre compte local pourrait sinon remplacer un binaire
    /// vérifié par un binaire malveillant, ou altérer des fichiers sensibles.
    /// Volontairement non bloquant par nature (retourne un booléen + détail,
    /// à toi de décider quoi en faire) : "AUTORITE NT\Utilisateurs authentifiés"
    /// hérite de droits Modify par défaut sur la plupart des dossiers Windows
    /// non durcis.
    /// </summary>
    public static class AclValidator
    {
        private const FileSystemRights WriteMask =
            FileSystemRights.Write | FileSystemRights.Modify | FileSystemRights.FullControl |
            FileSystemRights.WriteData | FileSystemRights.CreateFiles;

        /// <summary>
        /// Looks for an NTFS access rule granting write access to a broad group
        /// (<c>Everyone</c>, <c>Authenticated Users</c>, <c>Users</c>) on
        /// <paramref name="directoryPath"/> — the kind of permission that lets
        /// another local account swap out a binary you are about to run.
        /// </summary>
        /// <param name="directoryPath">
        /// The directory to inspect. A path that does not exist returns
        /// <see langword="false"/> rather than throwing.
        /// </param>
        /// <param name="details">
        /// A description of the offending rule when one is found; an empty
        /// string otherwise.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a broad write permission was found.
        /// Note the inverted sense compared to the other validators here: this
        /// method reports a <em>problem</em>, it does not certify safety.
        /// </returns>
        /// <remarks>
        /// Deliberately advisory: on most non-hardened Windows installs,
        /// <c>Authenticated Users</c> inherits Modify on many directories, so
        /// treating a hit as fatal would block legitimate setups. Warn, log, or
        /// harden — but think twice before refusing to start.
        /// </remarks>
        public static bool TryFindBroadWriteAccess(string directoryPath, out string details)
        {
            details = "";

            if (!Directory.Exists(directoryPath))
                return false;

            try
            {
                var security = new DirectoryInfo(directoryPath).GetAccessControl();
                var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

                var dangerousSids = new[]
                {
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                    new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                };

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (rule.AccessControlType != AccessControlType.Allow) continue;
                    if ((rule.FileSystemRights & WriteMask) == 0) continue;

                    var identity = rule.IdentityReference as SecurityIdentifier;
                    if (identity == null) continue;

                    foreach (var sid in dangerousSids)
                    {
                        if (identity.Equals(sid))
                        {
                            var name = TryTranslate(identity);
                            details = $"{name} a un droit d'écriture ({rule.FileSystemRights}) sur '{directoryPath}'";
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                details = $"Impossible de vérifier les ACL de '{directoryPath}' : {ex.Message}";
                return false;
            }
        }

        private static string TryTranslate(SecurityIdentifier sid)
        {
            try
            {
                return sid.Translate(typeof(NTAccount)).Value;
            }
            catch
            {
                return sid.Value;
            }
        }
    }
}
