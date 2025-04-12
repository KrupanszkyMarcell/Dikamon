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

            // Check if the image source is a URL
            if (Uri.TryCreate(imageSource, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                // If it's a URL, return an ImageSource from URI
                return ImageSource.FromUri(new Uri(imageSource));
            }

            // If not a URL, it's a local resource
            return imageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}