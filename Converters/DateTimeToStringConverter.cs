using System;
using System.Globalization;
using System.Windows.Data;

namespace ChaturbateRecorderApp.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                // Format à votre convenance
                return dateTime.ToString("dd/MM/yyyy HH:mm:ss"); // Exemple
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}