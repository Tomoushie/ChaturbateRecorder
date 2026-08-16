using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChaturbateRecorderApp.Converters
{
    /// <summary>
    /// Chaîne vide ou blanche → <see cref="Visibility.Collapsed"/>, sinon
    /// <see cref="Visibility.Visible"/>.
    ///
    /// `Collapsed` et non `Hidden` : un message d'erreur masqué doit rendre sa
    /// place, sinon la liste de salons est repoussée d'une ligne en permanence
    /// pour un texte qui n'apparaît presque jamais.
    /// </summary>
    public sealed class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
