using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public class PackageIconConverter : IValueConverter
    {
        #region IValueConverter Members

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PackageViewModel package)
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

                return metadata.IconUrl;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
