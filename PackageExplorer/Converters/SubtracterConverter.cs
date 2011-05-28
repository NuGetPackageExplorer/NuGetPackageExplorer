using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace PackageExplorer {
    public class SubtracterConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double doubleValue = System.Convert.ToDouble(value);
            double subtract = parameter == null ? 0 : System.Convert.ToDouble(parameter);
            return Math.Max(0, doubleValue - subtract);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
