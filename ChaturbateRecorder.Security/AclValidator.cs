using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ChaturbateRecorder.Security
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
