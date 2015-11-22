using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer
{
    [ValueConversion(typeof(SemanticVersion), typeof(string))]
    public class VersionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = (SemanticVersion) value;
            return version.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string) value;
            if (String.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }
            else
            {
                SemanticVersion version;
                if (SemanticVersion.TryParse(stringValue, out version))
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