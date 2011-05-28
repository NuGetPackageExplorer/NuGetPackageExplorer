using System;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer {
    public class StringToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string stringValue = (string)value;
            string parameterValue = (string)parameter;

            return String.Equals(stringValue, parameterValue, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
