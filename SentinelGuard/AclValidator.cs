using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SentinelGuard
{
    /// <summary>
    /// Detects whether a broad group (Everyone, BUILTIN\Users, Authenticated
    /// Users) holds write access on a directory — otherwise another local
    /// account could swap a binary you just verified, or tamper with sensitive
    /// files.
    /// </summary>
    /// <remarks>
    /// Deliberately informational rather than blocking: it returns a boolean
    /// plus the offending rule, and you decide what to do. On most non-hardened
    /// Windows installations, <c>NT AUTHORITY\Authenticated Users</c> inherits
    /// Modify rights on a great many directories — treating that as fatal would
    /// stop the application on perfectly ordinary machines.
    /// </remarks>
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
                            details = $"{name} has write access ({rule.FileSystemRights}) on '{directoryPath}'";
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                details = $"Could not read the ACLs of '{directoryPath}': {ex.Message}";
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
