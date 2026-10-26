using System;
using System.Globalization;
using System.Windows.Data;

namespace ChaturbateRecorderApp.Converters
{
    /// <summary>
    /// Inverse un booléen.
    ///
    /// Il évite d'ajouter une propriété « NonPremiere » au modèle de vue à
    /// chaque fois qu'une liaison a besoin du contraire d'un état : ces
    /// propriétés-miroir doivent être resignalées en même temps que l'originale,
    /// et c'est exactement le genre d'oubli qui laisse un bouton actif alors
    /// qu'il ne devrait plus l'être.
    /// </summary>
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && !b;
    }
}
