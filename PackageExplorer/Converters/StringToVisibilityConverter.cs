using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    public class StringToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string)value;

            var parameterValue = (string)parameter;
            var candidates = parameterValue.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var contains = candidates.Any(s => Matching(s, stringValue));
            return contains ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool Matching(string pattern, string value)
        {
            if (pattern.IndexOf('*') > -1)
            {
                var patternParts = pattern.Split('\\');
                var valueParts = value.Split('\\');

                if (patternParts.Length != valueParts.Length)
                {
                    return false;
                }

                for (var i = 0; i < patternParts.Length; i++)
                {
                    if (patternParts[i] != "*" &&
                        !string.Equals(patternParts[i], valueParts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return string.Equals(pattern, value, StringComparison.OrdinalIgnoreCase);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}