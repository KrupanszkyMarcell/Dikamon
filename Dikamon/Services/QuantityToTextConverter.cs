using System;
using System.Globalization;
using Dikamon.Models;

namespace Dikamon.Services
{
    public class QuantityToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int quantity)
            {
                // The context might be a Stores object which contains a StoredItem property
                if (parameter is Stores storedItem && storedItem.StoredItem != null)
                {
                    return $"{quantity} {storedItem.StoredItem.Unit ?? "db"}";
                }

                // Default unit if we don't have context
                return $"{quantity} db";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}