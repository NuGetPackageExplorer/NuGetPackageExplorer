using System;
using System.Globalization;
using System.Windows.Data;
using Humanizer;

namespace PackageExplorer
{
    public class NumberToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                return i.ToMetric(decimals: 1);
            }
            if (value is double dbl)
            {
                return dbl.ToMetric(decimals: 1);
            }
            if (value != null)
            {
                var number = double.Parse(value.ToString(), culture);
                return number.ToMetric(decimals: 1);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
