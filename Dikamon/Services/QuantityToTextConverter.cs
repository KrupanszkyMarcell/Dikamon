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

                    if (parameter is Stores storedItem && storedItem.StoredItem != null)
                    {
                        return $"{quantity} {storedItem.StoredItem.Unit ?? "db"}";
                    }


                    var contextStore = parameter as Stores;
                    if (contextStore?.StoredItem != null)
                    {
                        return $"{quantity} {contextStore.StoredItem.Unit ?? "db"}";
                    }
                    return $"{quantity} db";
                }
            }
            catch (Exception ex)
            {
            }

            return $"{value} db";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}