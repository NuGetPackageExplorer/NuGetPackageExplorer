using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

using Windows.UI.Xaml.Media.Imaging;

namespace PackageExplorer
{
    public class IconUrlConverterHelper
    {
        private const int DecodePixelWidth = 32;

        public object? ConvertToBitmapSource(object value, object parameter)
        {
            var iconUrl = value as string;
            var defaultPackageIcon = parameter as BitmapSource;
            if (string.IsNullOrWhiteSpace(iconUrl))
            {
                return defaultPackageIcon!;
            }

#pragma warning disable IDE0007 // Use implicit type
            BitmapImage? iconBitmapImage = new BitmapImage();
#pragma warning restore IDE0007 // Use implicit type

            iconBitmapImage.UriSource = new Uri(iconUrl);

            // Instead of scaling larger images and keeping larger image in memory, this makes it so we scale it down, and throw away the bigger image.
            // Only need to set this on one dimension, to preserve aspect ratio
            iconBitmapImage.DecodePixelWidth = DecodePixelWidth;

            iconBitmapImage.ImageFailed += IconBitmapImage_ImageFailed;

            // TODO: Add logic to handle unavailable icon url
            return iconBitmapImage ?? defaultPackageIcon!;
        }

        private void IconBitmapImage_ImageFailed(object sender, Windows.UI.Xaml.ExceptionRoutedEventArgs e)
        {
        }
    }
}
