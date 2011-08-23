using System;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer {
    public class TaskShortcutVisibilityConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            bool visible = values[0] == null && (bool)values[1];
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
