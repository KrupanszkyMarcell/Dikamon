using System;
using System.Globalization;

namespace Dikamon.Services
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "#4CAF50" : "#F44336"; // Green if true (has enough), Red if false (not enough)
            }
            return "#F44336"; // Default to red (not enough)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}