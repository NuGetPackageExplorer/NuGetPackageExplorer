using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer
{
    [ValueConversion(typeof(Version), typeof(string))]
    public class NetVersionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = (Version)value;
            return version == null ? String.Empty : version.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string)value;
            if (String.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }
            else
            {
                Version version;
                if (Version.TryParse(stringValue, out version))
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