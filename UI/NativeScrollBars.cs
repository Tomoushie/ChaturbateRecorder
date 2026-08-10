using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Donne des ascenseurs sombres aux contrôles qui en dessinent eux-mêmes
    /// (114.0).
    ///
    /// **Pourquoi une classe partagée** : l'ascenseur d'un contrôle Windows est
    /// peint par le système en zone NON CLIENTE. Ni <c>BackColor</c> ni le
    /// dessin de l'application ne l'atteignent — seul
    /// <c>SetWindowTheme(handle, "DarkMode_Explorer")</c> le noircit. La 103.0
    /// l'avait appliqué à la seule <see cref="ListView"/>, si bien que
    /// l'historique et la surveillance avaient des ascenseurs sombres pendant
    /// que les Favoris, les Logs et la fenêtre principale gardaient les leurs
    /// en BLANC. C'est ce mélange qui a été signalé, pas la couleur elle-même.
    ///
    /// **Le thème se déduit de la palette et non d'un booléen transmis** : la
    /// palette est interpolée pendant le fondu clair/sombre, et un booléen
    /// ferait basculer les ascenseurs d'un coup au milieu de la transition.
    ///
    /// **Le handle peut ne pas exister au moment de l'appel** — un contrôle
    /// jamais affiché (mode simple, fenêtre pas encore montrée) n'en a pas.
    /// L'application est donc rejouée sur <see cref="Control.HandleCreated"/>,
    /// sans quoi le premier passage serait perdu en silence et le contrôle
    /// resterait clair jusqu'au prochain changement de thème.
    /// </summary>
    internal static class NativeScrollBars
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr handle, string? appName, string? idList);

        private sealed class State
        {
            internal bool Wired;
            internal bool Dark;
        }

        private static readonly ConditionalWeakTable<Control, State> _states = new();

        /// <summary>
        /// Un fond de zone de saisie sombre vaut thème sombre. Seuil sur la
        /// somme des composantes, comme <see cref="ThemedListView"/> l'a
        /// toujours fait — c'est la même décision, elle doit rester la même
        /// valeur.
        /// </summary>
        internal static bool IsDark(ThemeManager.Palette palette) =>
            palette.Input.R + palette.Input.G + palette.Input.B < 384;

        internal static void Apply(Control control, ThemeManager.Palette palette) =>
            Apply(control, IsDark(palette));

        /// <summary>
        /// Ce contrôle a-t-il été pris en charge ? Exposé au projet de tests :
        /// <c>SetWindowTheme</c> ne renvoie rien de lisible et n'agit qu'en
        /// zone non cliente, donc la seule chose vérifiable sans capture
        /// d'écran est que ThemeManager a bien PASSÉ le contrôle par ici. Or
        /// c'est exactement ce qui manquait dans 114.0 : un seul type de
        /// contrôle y passait.
        /// </summary>
        internal static bool IsWired(Control control) => _states.TryGetValue(control, out _);

        internal static void Apply(Control control, bool dark)
        {
            var state = _states.GetValue(control, _ => new State());
            state.Dark = dark;

            if (!state.Wired)
            {
                state.Wired = true;
                control.HandleCreated += (s, e) =>
                {
                    if (s is Control c && _states.TryGetValue(c, out var st)) Push(c, st.Dark);
                };
            }

            if (control.IsHandleCreated) Push(control, dark);
        }

        private static void Push(Control control, bool dark)
        {
            try
            {
                SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch (Exception ex)
            {
                // uxtheme absent ou refusé : le contrôle reste utilisable, seuls
                // ses ascenseurs gardent le rendu clair. Rien à signaler à
                // l'utilisateur, qui n'y peut rien.
                System.Diagnostics.Debug.WriteLine($"SetWindowTheme indisponible : {ex.Message}");
            }
        }
    }
}
