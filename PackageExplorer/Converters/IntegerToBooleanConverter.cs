using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    internal class IntegerToBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var intValue = (int)value;
            if (intValue != 12 && intValue != 14 && intValue != 16 && intValue != 18)
            {
                intValue = 12;
            }

            var compareValue = System.Convert.ToInt32(parameter, culture);
            return intValue == compareValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}