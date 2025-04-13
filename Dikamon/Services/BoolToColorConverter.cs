using System;
using System.Globalization;

namespace Dikamon.Services
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "#4CAF50" : "#BDBDBD"; // Green if true, Gray if false
            }
            return "#BDBDBD"; // Default to gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}