using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ChaturbateRecorderApp.Converters
{
    /// <summary>
    /// Résout une clé de ressource (« Brush.Success ») en pinceau.
    ///
    /// Il existe pour la même raison que <see cref="IconTemplateConverter"/> :
    /// garder les modèles de vue en types simples. Une propriété de type
    /// <see cref="Brush"/> sur un modèle de vue le rendrait dépendant de WPF,
    /// donc intestable sans application.
    ///
    /// **Attention, ce que ce convertisseur rend est FIGÉ dans le temps** : la
    /// liaison ne se réévalue qu'au changement de la clé, pas au changement de
    /// thème. C'est acceptable ici parce que la clé change à chaque changement
    /// d'état de la carte, mais un bandeau de couleur d'état resterait à
    /// l'ancienne teinte si l'utilisateur basculait le thème sans rien faire
    /// d'autre — d'où <c>ThemeManager.Applied</c>, que les vues écoutent pour
    /// rafraîchir leurs liaisons.
    /// </summary>
    public sealed class ResourceKeyToBrushConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string cle || string.IsNullOrEmpty(cle)) return null;
            return Application.Current?.TryFindResource(cle) as Brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
