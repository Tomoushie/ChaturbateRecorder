using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ChaturbateRecorderApp.Converters
{
    /// <summary>
    /// Chemin de fichier → miniature décodée, ou <c>null</c>.
    ///
    /// **`CacheOption = OnLoad` est le point entier de cette classe.** Lier un
    /// chemin directement à <c>Image.Source</c> laisse WPF ouvrir le fichier et
    /// le GARDER OUVERT tant que l'image vit : supprimer un enregistrement
    /// depuis l'explorateur échouerait alors sur un fichier « utilisé par un
    /// autre processus », sans que rien dans l'application ne le laisse
    /// deviner. `OnLoad` lit tout en mémoire puis referme.
    ///
    /// `DecodePixelWidth` borne le coût : cinquante JPEG de pleine résolution
    /// décodés pour des vignettes de 64 px, c'est plusieurs centaines de Mo
    /// d'images dont on n'affiche que le centième.
    ///
    /// **Le pendant WinForms de ce fichier a coûté deux plantages en v1.35.0**
    /// (le piège `ImageList` de 103.0) : là-bas les bitmaps devaient être
    /// libérés à la main, et l'ordre entre l'ajout et la libération décidait du
    /// plantage. Ici l'image est gelée puis oubliée — il n'y a plus rien à
    /// libérer, donc plus d'ordre à respecter.
    /// </summary>
    public sealed class PathToThumbnailConverter : IValueConverter
    {
        /// <summary>Largeur de décodage. La vignette est affichée à 64 px.</summary>
        private const int LargeurDecodee = 128;

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string chemin || string.IsNullOrEmpty(chemin)) return null;
            if (!File.Exists(chemin)) return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                image.DecodePixelWidth = LargeurDecodee;
                image.UriSource = new Uri(chemin, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception)
            {
                // Un JPEG tronqué — capture interrompue pendant l'extraction —
                // ne doit pas faire tomber la liste entière. Pas de miniature,
                // la ligne s'affiche quand même.
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
