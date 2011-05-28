using System;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer {
    public class NullToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (targetType == typeof(Visibility)) {

                string stringValue = value as string;
                if (stringValue != null) {
                    return String.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
                }

                return value == null ? Visibility.Collapsed : Visibility.Visible;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
