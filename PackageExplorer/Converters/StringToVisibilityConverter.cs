using System;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    public class StringToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string) value;
            
            var parameterValue = (string) parameter;
            string[] candidates = parameterValue.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);

            bool contains = candidates.Any(s => Matching(s, stringValue)); 
            return contains ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool Matching(string pattern, string value)
        {
            if (pattern.IndexOf('*') > -1) 
            {
                string[] patternParts = pattern.Split('\\');
                string[] valueParts = value.Split('\\');

                if (patternParts.Length != valueParts.Length) 
                {
                    return false;
                }

                for (int i = 0; i < patternParts.Length; i++)
                {
                    if (patternParts[i] != "*" && 
                        !String.Equals(patternParts[i], valueParts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }
            else 
            {
                return String.Equals(pattern, value, StringComparison.OrdinalIgnoreCase);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}