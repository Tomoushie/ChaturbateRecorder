using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Couleur dynamique de la ProgressBar (3.4) : WinForms n'expose aucune
    /// propriété pour ça, mais le contrôle natif Windows la supporte via le
    /// message PBM_SETBARCOLOR — sans owner-draw ni dépendance externe.
    /// </summary>
    public static class ProgressBarColorExtensions
    {
        private const int PBM_SETBARCOLOR = 0x0409;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, int lParam);

        public static void SetBarColor(this ProgressBar bar, Color color)
        {
            if (!bar.IsHandleCreated) return;
            SendMessage(bar.Handle, PBM_SETBARCOLOR, IntPtr.Zero, ColorTranslator.ToWin32(color));
        }
    }
}
