using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Dikamon.Services
{
    public class ImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var imageSource = value.ToString();


            if (Uri.TryCreate(imageSource, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {

                return ImageSource.FromUri(new Uri(imageSource));
            }


            return imageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}