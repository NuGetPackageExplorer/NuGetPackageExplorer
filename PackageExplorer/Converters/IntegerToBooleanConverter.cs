using System;
using System.Windows.Data;

namespace PackageExplorer {
    class IntegerToBooleanConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            int intValue = (int)value;
            if (intValue != 12 && intValue != 14 && intValue != 16 && intValue != 18) {
                intValue = 12;
            }

            int compareValue = System.Convert.ToInt32(parameter);
            return intValue == compareValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
