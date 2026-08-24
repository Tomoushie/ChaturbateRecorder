using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChaturbateRecorderApp.Converters
{
    /// <summary>
    /// Vrai → <c>Button.Primary</c>, faux → <c>Button.Secondary</c> — pour
    /// indiquer visuellement lequel de deux boutons bascule (mode, tri) est
    /// actif.
    ///
    /// **Un Setter externe sur `Button.Background`/`Foreground` ne suffit
    /// pas** : le `ControlTemplate` de `Button.Secondary` peint son fond en
    /// dur (`Background="{DynamicResource Brush.Btn.Secondary.Fill}"` sur le
    /// `Border` du gabarit), jamais via `{TemplateBinding Background}` —
    /// exactement le piège WPF n°8 déjà payé (un Setter de style ne traverse
    /// pas le ControlTemplate). Changer le STYLE ENTIER est le seul moyen qui
    /// atteigne réellement le pinceau affiché.
    /// </summary>
    public sealed class BoolToButtonStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var actif = value is true;
            var cle = actif ? "Button.Primary" : "Button.Secondary";
            return Application.Current.TryFindResource(cle) ?? Application.Current.FindResource("Button.Secondary");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
