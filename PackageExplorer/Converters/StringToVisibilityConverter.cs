using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    public class StringToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string) value;
            var parameterValue = (string) parameter;

            return String.Equals(stringValue, parameterValue, StringComparison.OrdinalIgnoreCase)
                       ? Visibility.Visible
                       : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}