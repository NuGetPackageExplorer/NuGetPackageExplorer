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

            return values.Cast<bool>().All(a => a);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
