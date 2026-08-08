using System;
using System.IO;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Resout un dossier de travail au demarrage, avec repli.
    ///
    /// **Pourquoi ce service existe** : jusqu'a la v1.26.x, le demarrage faisait
    /// un `Directory.CreateDirectory` nu sur le dossier de capture. Sur une
    /// machine ou ce chemin designait un disque absent, l'exception tuait
    /// l'application AVANT qu'elle n'affiche quoi que ce soit — et le rapport de
    /// crash ne pouvait pas s'ecrire non plus, puisqu'il va dans le dossier de
    /// logs, injoignable pour la meme raison. Un utilisateur ayant pourtant tout
    /// installe correctement se retrouvait avec une fenetre d'erreur fatale et
    /// aucune piste.
    ///
    /// La regle retenue : un dossier de travail injoignable est un probleme
    /// reel, mais il ne justifie jamais de refuser de demarrer.
    /// </summary>
    public static class DirectoryResolver
    {
        /// <summary>
        /// Cree <paramref name="wanted"/> s'il est joignable, sinon
        /// <paramref name="fallback"/>, sinon <paramref name="lastResort"/>.
        /// <paramref name="fellBack"/> vaut vrai des que le dossier retourne
        /// n'est pas celui demande — c'est ce qui declenche l'avertissement a
        /// l'utilisateur et la correction du reglage persiste.
        /// </summary>
        public static string EnsureOrFallback(string? wanted, string fallback, string lastResort, out bool fellBack)
        {
            fellBack = false;

            if (!string.IsNullOrWhiteSpace(wanted) && TryCreate(wanted))
                return wanted;

            fellBack = true;
            if (TryCreate(fallback)) return fallback;

            // Le dossier de l'application existe forcement : on s'y execute.
            Logger.Log($"Repli '{fallback}' impossible : utilisation de '{lastResort}'.", LogLevel.ERROR);
            return lastResort;
        }

        private static bool TryCreate(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                // Disque absent, droits refuses, chemin devenu invalide, nom
                // trop long : toutes ces causes se traitent pareil — on replie.
                Logger.Log($"Dossier '{path}' injoignable ({ex.GetType().Name}) : {ex.Message}", LogLevel.WARN);
                return false;
            }
        }
    }
}
