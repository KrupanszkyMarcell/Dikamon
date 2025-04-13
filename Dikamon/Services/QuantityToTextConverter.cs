using System;
using System.Globalization;
using Dikamon.Models;
using System.Diagnostics;

namespace Dikamon.Services
{
    public class QuantityToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is int quantity)
                {
                    // Case 1: The parameter is a Stores object
                    if (parameter is Stores storedItem && storedItem.StoredItem != null)
                    {
                        Debug.WriteLine($"QuantityToTextConverter - Case 1: Using unit from parameter: {storedItem.StoredItem.Unit ?? "db"}");
                        return $"{quantity} {storedItem.StoredItem.Unit ?? "db"}";
                    }

                    // Case 2: We're in a DataTemplate with Stores as DataContext, but no parameter passed
                    var contextStore = parameter as Stores;
                    if (contextStore?.StoredItem != null)
                    {
                        Debug.WriteLine($"QuantityToTextConverter - Case 2: Using unit from context: {contextStore.StoredItem.Unit ?? "db"}");
                        return $"{quantity} {contextStore.StoredItem.Unit ?? "db"}";
                    }

                    // Default case: Use "db" as fallback
                    Debug.WriteLine("QuantityToTextConverter - Default case: Using 'db' as fallback unit");
                    return $"{quantity} db";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QuantityToTextConverter - Exception: {ex.Message}");
            }

            return $"{value} db";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}