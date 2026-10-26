using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ChaturbateRecorderApp.Converters
{
    /// <summary>
    /// Résout un nom de pictogramme (« Icon.Camera ») en <see cref="ControlTemplate"/>
    /// du dictionnaire de ressources.
    ///
    /// Il existe pour que le modèle de vue reste en TYPES SIMPLES : lui faire
    /// exposer directement un ControlTemplate le ferait dépendre de
    /// System.Windows.Controls, donc de WPF, et il ne serait plus testable sans
    /// application. C'est la même règle que celle qui régit le liage du module
    /// payant, où la surface est volontairement réduite à des primitives.
    ///
    /// Un nom inconnu rend `null` — la ligne s'affiche alors sans pictogramme.
    /// Le contrôle WinForms faisait le même choix explicitement : « un
    /// pictogramme manquant ne doit pas empêcher de naviguer ».
    /// </summary>
    public sealed class IconTemplateConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string cle || string.IsNullOrEmpty(cle)) return null;
            return Application.Current?.TryFindResource(cle) as ControlTemplate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
