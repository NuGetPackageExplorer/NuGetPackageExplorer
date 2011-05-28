using System;
using System.Linq;
using System.Windows.Data;

namespace PackageExplorer {
    public class MultiStringToBoolConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return values.Cast<string>().All(s => !String.IsNullOrEmpty(s));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
