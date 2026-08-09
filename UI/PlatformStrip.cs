using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Rangée des plateformes prises en charge (103.0), affichée à côté du
    /// champ d'URL.
    ///
    /// **Ce qu'elle règle** : rien n'indiquait à l'utilisateur ce qu'il pouvait
    /// coller dans ce champ. Le libellé disait « URL Chaturbate », donc une
    /// seule plateforme ; il dit maintenant « URL du live », ce qui est exact
    /// mais ne dit plus lesquelles. Quatre pictogrammes le disent d'un coup
    /// d'œil, sans occuper de place ni ajouter une ligne de texte.
    ///
    /// **Un contrôle dédié plutôt que quatre PictureBox** : ThemeManager
    /// encadre les PictureBox (elles comptent parmi les champs), ce qui aurait
    /// dessiné un cadre autour de chaque icône. Ici tout est peint d'un bloc,
    /// et les icônes suivent la couleur du thème comme le reste.
    /// </summary>
    public class PlatformStrip : Control
    {
        private const int IconSize = 18;
        private const int Gap = 10;

        private readonly List<(StreamPlatform Platform, string Icon, string Label)> _items = new();
        private readonly Dictionary<string, Bitmap> _rendered = new();
        private readonly ToolTip _tip = new();
        private Color _iconColor = Color.Empty;

        public PlatformStrip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = false;

            foreach (var platform in Platforms.Supported)
            {
                var (icon, label) = Platforms.Badge(platform);
                _items.Add((platform, icon, label));
            }

            Size = new Size(_items.Count * (IconSize + Gap), IconSize + 2);
        }

        /// <summary>Couleur des pictogrammes, posée par ThemeManager.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color IconColor
        {
            get => _iconColor;
            set
            {
                if (_iconColor == value) return;
                _iconColor = value;
                DisposeRendered();
                Invalidate();
            }
        }

        /// <summary>
        /// Infobulle listant les plateformes. Passe par Localization pour que
        /// le changement de langue en cours de session la retraduise.
        /// </summary>
        public void RefreshTooltip()
        {
            var names = string.Join(", ", _items.ConvertAll(i => i.Label));
            _tip.SetToolTip(this, Localization.Format("platforms.tooltip", names));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_iconColor.IsEmpty) return;

            var x = 0;
            foreach (var item in _items)
            {
                var bitmap = Render(item.Icon);
                if (bitmap != null)
                    e.Graphics.DrawImage(bitmap, x, (Height - IconSize) / 2, IconSize, IconSize);
                x += IconSize + Gap;
            }
        }

        private Bitmap? Render(string icon)
        {
            if (_rendered.TryGetValue(icon, out var cached)) return cached;

            try
            {
                var bitmap = IconManager.Render(icon, IconSize, _iconColor);
                _rendered[icon] = bitmap;
                return bitmap;
            }
            catch (Exception ex)
            {
                // Une icône manquante ne doit pas empêcher la fenêtre de
                // s'afficher : c'est un ornement, pas une fonctionnalité.
                Services.Logger.Log($"Icône de plateforme '{icon}' indisponible : {ex.Message}", Services.LogLevel.WARN);
                return null;
            }
        }

        private void DisposeRendered()
        {
            foreach (var bitmap in _rendered.Values) bitmap.Dispose();
            _rendered.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeRendered();
                _tip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
