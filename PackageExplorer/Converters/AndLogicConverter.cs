using System;
using System.Linq;
using System.Windows.Data;

namespace PackageExplorer {
    public class AndLogicConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return values.Cast<bool>().All(a => a);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
