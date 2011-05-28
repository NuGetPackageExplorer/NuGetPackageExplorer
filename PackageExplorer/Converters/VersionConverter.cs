using System;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer {

    [ValueConversion(typeof(Version), typeof(string))]
    public class VersionConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            Version version = (Version)value;
            return version.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string stringValue = (string)value;
            if (String.IsNullOrWhiteSpace(stringValue)) {
                return null;
            }
            else {
                Version version;
                if (Version.TryParse(stringValue, out version)) {
                    return version;
                }
                else {
                    return DependencyProperty.UnsetValue;
                }
            }
        }
    }
}
