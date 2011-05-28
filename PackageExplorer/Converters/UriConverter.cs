using System;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer {
    public class UriConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            Uri uri = (Uri)value;
            return uri == null ? null : uri.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string stringValue = (string)value;
            if (String.IsNullOrEmpty(stringValue)) {
                return null;
            }
            else {
                Uri uri;
                if (Uri.TryCreate(stringValue, UriKind.Absolute, out uri)) {
                    return uri;
                }
                else {
                    return DependencyProperty.UnsetValue;
                }
            }
        }
    }
}
