using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class SubtracterConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double doubleValue = System.Convert.ToDouble(value);
            double subtract = parameter == null ? 0 : System.Convert.ToDouble(parameter);
            return Math.Max(0, doubleValue - subtract);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}