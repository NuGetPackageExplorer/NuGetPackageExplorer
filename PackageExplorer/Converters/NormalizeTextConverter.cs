using System;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace PackageExplorer {
    public class NormalizeTextConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string stringValue = (string)value;
            if (String.IsNullOrEmpty(stringValue)) {
                return stringValue;
            }

            // replace a series of whitepaces with a single whitespace
            // REVIEW: Should we avoid regex and just do this manually?
            return Regex.Replace(stringValue, @"\s+", " ");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}