using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class NumberToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var number = double.Parse(value.ToString());
                return NumberToString(number, culture.NumberFormat);
            }

            return value;
        }

        private string NumberToString(double number, IFormatProvider culture)
        {
            var exp = 0;

            while (number >= 1000)
            {
                number /= 1000;
                ++exp;
            }

            var s = string.Format(culture, "{0:G3}{1}", number, ScalingFactor[exp]);
            return s;
        }

        private static readonly string[] ScalingFactor =
        {
            string.Empty,
            "K", // kilo
            "M", // mega, million
            "G", // giga, billion
            "T" // tera, trillion
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
