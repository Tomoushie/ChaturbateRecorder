using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Encadre les champs de saisie et les listes (39.0).
    ///
    /// **Pourquoi le cadre est dessiné par le PARENT** : une ListView, une
    /// ListBox, un TextBox ou une PictureBox ne savent pas porter une bordure
    /// de couleur choisie — la leur est peinte par le système, et restait
    /// blanche en thème sombre (l'un des défauts les plus voyants de la
    /// fenêtre avant 39.0). ThemeManager met donc leur <c>BorderStyle</c> à
    /// <c>None</c>, et le conteneur trace le cadre juste à côté.
    ///
    /// Les cartes (<see cref="RoundedGroupPanel"/>) appellent
    /// <see cref="DrawAll"/> depuis leur propre rendu ; pour tout autre
    /// conteneur — une fenêtre de dialogue, par exemple — <see cref="Attach"/>
    /// s'abonne à son évènement Paint, une seule fois.
    /// </summary>
    internal static class InputFrame
    {
        // Écart entre le champ et son cadre. Plus large horizontalement : un
        // TextBox sans bordure colle son texte à 1 px de son bord gauche.
        private const int InsetX = 4;
        private const int InsetY = 3;
        private const int Radius = 5;

        private sealed class State
        {
            public Color Color;
            public bool Wired;
        }

        private static readonly ConditionalWeakTable<Control, State> _states = new();

        public static bool NeedsFrame(Control control) =>
            control is ListView or ListBox or TextBox or PictureBox;

        /// <summary>
        /// Fait en sorte que le parent de <paramref name="child"/> dessine les
        /// cadres. Sans effet si le parent est une carte : elle s'en charge
        /// déjà dans son propre rendu, et un second tracé doublerait le trait.
        /// </summary>
        public static void Attach(Control child, Color color)
        {
            var parent = child.Parent;
            if (parent == null || parent is RoundedGroupPanel) return;

            var state = _states.GetValue(parent, _ => new State());
            state.Color = color;

            if (!state.Wired)
            {
                state.Wired = true;
                var container = parent;
                container.Paint += (s, e) => DrawAll(e.Graphics, container, state.Color);
            }

            parent.Invalidate();
        }

        public static void DrawAll(Graphics g, Control container, Color color)
        {
            var smoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (Control child in container.Controls)
            {
                if (!child.Visible || !NeedsFrame(child)) continue;

                var rect = new Rectangle(
                    child.Left - InsetX, child.Top - InsetY,
                    child.Width + 2 * InsetX - 1, child.Height + 2 * InsetY - 1);
                if (rect.Width <= 0 || rect.Height <= 0) continue;

                using var pen = new Pen(color);
                using var path = ThemedButton.RoundedRect(rect, Radius);
                g.DrawPath(pen, path);
            }

            g.SmoothingMode = smoothing;
        }
    }
}
