using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace PackageExplorer
{
    public class NormalizeTextConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string)value;
            if (string.IsNullOrEmpty(stringValue))
            {
                return stringValue;
            }

            // replace a series of whitepaces with a single whitespace
            // REVIEW: Should we avoid regex and just do this manually?
            return Regex.Replace(stringValue, @"[\f\t\v\x85\p{Z}]+", " ");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}