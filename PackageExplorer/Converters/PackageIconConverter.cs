using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public class PackageIconConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length > 1 && values[0] is PackageViewModel package && values[1] is string str && !string.IsNullOrEmpty(str))
            {
                var metadata = package.PackageMetadata;

                if (!string.IsNullOrEmpty(metadata.Icon))
                {
                    foreach (var file in package.RootFolder.GetFiles())
                    {
                        if (file.Path == metadata.Icon)
                        {
                            var image = new BitmapImage();
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.StreamSource = file.GetStream();
                            image.EndInit();
                            return image;
                        }
                    }

                }

                if (metadata.IconUrl != null)
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = metadata.IconUrl;
                    image.EndInit();
                    return image;
                }
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
