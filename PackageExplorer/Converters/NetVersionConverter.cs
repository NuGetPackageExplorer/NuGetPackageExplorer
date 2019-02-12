using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    [ValueConversion(typeof(Version), typeof(string))]
    public class NetVersionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = (Version)value;
            return version == null ? string.Empty : version.ToString();
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string)value;
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }
            else
            {
                if (Version.TryParse(stringValue, out var version))
                {
                    return version;
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }
        }

        #endregion
    }
}
