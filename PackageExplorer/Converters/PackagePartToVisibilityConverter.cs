using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public class PackagePartToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var type = (string)parameter;
            if (type == "file")
            {
                return BoolToVisibility(value is PackageFile);
            }
            else if (type == "folder")
            {
                return BoolToVisibility(value is PackageFolder);
            }
            else
            {
                return BoolToVisibility(value is PackagePart);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        private static Visibility BoolToVisibility(bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
