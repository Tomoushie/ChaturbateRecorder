using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChaturbateRecorderApp.Converters
{
    /// <summary>
    /// Faux → <see cref="Visibility.Visible"/>, vrai → <see cref="Visibility.Collapsed"/>.
    ///
    /// Le pendant du <see cref="BooleanToVisibilityConverter"/> natif de WPF,
    /// qui n'a pas de variante inversée. Même raison que
    /// <see cref="InverseBooleanConverter"/> : évite une propriété miroir sur
    /// le modèle de vue à chaque fois qu'une liaison a besoin du contraire
    /// d'un état, ici pour de la visibilité et non un booléen brut.
    /// </summary>
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
