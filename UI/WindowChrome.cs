using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Met la BARRE DE TITRE au thème de l'application.
    ///
    /// **Elle est dessinée par Windows, pas par WPF** : aucun style, aucun
    /// pinceau et aucun `DynamicResource` ne l'atteint. Une fenêtre entièrement
    /// sombre gardait donc un bandeau blanc en haut — signalé sur capture par
    /// le mainteneur, et c'est le seul morceau de thème clair qui subsistait.
    ///
    /// C'est l'exact pendant du `SetWindowTheme(..., "DarkMode_Explorer")` que
    /// la version WinForms devait appeler pour ses ascenseurs (114.0) : même
    /// famille de problème, même solution — un appel système, parce que la zone
    /// concernée n'appartient pas à l'application.
    /// </summary>
    internal static class WindowChrome
    {
        /// <summary>
        /// `DWMWA_USE_IMMERSIVE_DARK_MODE`. La valeur est 20 depuis Windows 10
        /// 20H1 ; elle valait 19 sur les versions antérieures, d'où les deux
        /// tentatives — un attribut inconnu est REFUSÉ, pas ignoré.
        /// </summary>
        private const int AttributModeSombre = 20;
        private const int AttributModeSombreAncien = 19;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attribut, ref int valeur, int taille);

        /// <summary>
        /// Applique le thème courant à la fenêtre, et le réapplique à chaque
        /// changement de thème.
        ///
        /// L'abonnement est retiré à la fermeture : <c>ThemeManager.Applied</c>
        /// est statique, et une fenêtre modale oubliée dedans y resterait pour
        /// toute la durée du processus.
        /// </summary>
        internal static void Suivre(Window fenetre)
        {
            void Appliquer() => Poser(fenetre, ThemeManager.Current == AppTheme.Dark);

            // La poignée n'existe pas avant `SourceInitialized` : appeler plus
            // tôt vise IntPtr.Zero et ne fait rien, en silence.
            if (new WindowInteropHelper(fenetre).Handle != IntPtr.Zero) Appliquer();
            else fenetre.SourceInitialized += (s, e) => Appliquer();

            void SurTheme() => fenetre.Dispatcher.Invoke(Appliquer);
            ThemeManager.Applied += SurTheme;
            fenetre.Closed += (s, e) => ThemeManager.Applied -= SurTheme;
        }

        private static void Poser(Window fenetre, bool sombre)
        {
            try
            {
                var poignee = new WindowInteropHelper(fenetre).Handle;
                if (poignee == IntPtr.Zero) return;

                var valeur = sombre ? 1 : 0;
                if (DwmSetWindowAttribute(poignee, AttributModeSombre, ref valeur, sizeof(int)) != 0)
                    DwmSetWindowAttribute(poignee, AttributModeSombreAncien, ref valeur, sizeof(int));
            }
            catch (Exception ex)
            {
                // Une barre de titre claire est un défaut d'aspect ; une
                // exception ici serait un défaut de fonctionnement. Sur un
                // Windows trop ancien, `dwmapi.dll` peut manquer.
                Services.Logger.Log($"Barre de titre : thème non appliqué — {ex.Message}",
                    Services.LogLevel.WARN);
            }
        }
    }
}
