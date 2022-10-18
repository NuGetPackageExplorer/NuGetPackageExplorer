using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public class PackageIconConverter : IMultiValueConverter
    {
        private static readonly IconUrlToImageCacheConverter IconUrlConverter = new IconUrlToImageCacheConverter();

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length > 1 && values[0] is PackageViewModel package && !package.IsDisposed && values[1] is string str && !string.IsNullOrEmpty(str))
            {
                var metadata = package.PackageMetadata;

                if (!string.IsNullOrEmpty(metadata.Icon))
                {
                    // Normalize any directories to match what's the package
                    // We do this here instead of the metadata so that we round-trip
                    // whatever the user originally had when in edit view
                    var iconPath = metadata.Icon.Replace('/', '\\');
                    foreach (var file in package.RootFolder.GetFiles())
                    {
                        if (string.Equals(file.Path, iconPath, StringComparison.OrdinalIgnoreCase))
                        {
                            // catch potential exceptions during image loading from zip file
                            // see 1097
                            try
                            {
                                var image = new BitmapImage();
                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.StreamSource = file.GetStream();
                                image.EndInit();
                                return image;
                            }
                            catch { }
                        }
                    }

                }

                if (metadata.IconUrl != null)
                {
                    return IconUrlConverter.Convert(metadata.IconUrl?.ToString()!, targetType, Images.DefaultPackageIcon, culture);
                }
            }
            return Images.DefaultPackageIcon;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
