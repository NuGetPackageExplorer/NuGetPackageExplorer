using System;
using System.Windows.Data;

namespace PackageExplorer
{
    public class StringShortenerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null)
            {
                return value;
            }
            var stringValue = (string)value;
            var maxLength = System.Convert.ToInt32(parameter, culture);
            if (maxLength < 5)
            {
                throw new ArgumentOutOfRangeException("parameter");
            }

            if (stringValue.Length <= maxLength)
            {
                return stringValue;
            }

            var prefixLength = (maxLength - 3) / 2;
            var suffixLength = maxLength - 3 - prefixLength;
            if (suffixLength < 0) suffixLength = 0;

            return stringValue.Substring(0, prefixLength) + "..." + stringValue[^suffixLength];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
