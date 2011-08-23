using System;
using System.Linq;
using System.Windows.Data;
using System.Windows;

namespace PackageExplorer {
    public class AndLogicConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (values.Any(v => v == null || v == DependencyProperty.UnsetValue)) {
                return false;
            }

            if (targetType == typeof(bool)) {
                return values.Cast<bool>().All(a => a);
            }
            else if (targetType == typeof(Visibility)) {
                bool notVisible = values.Cast<Visibility>().Any(v => v != Visibility.Visible);
                return notVisible ? Visibility.Collapsed : Visibility.Visible;
            }
            else {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
