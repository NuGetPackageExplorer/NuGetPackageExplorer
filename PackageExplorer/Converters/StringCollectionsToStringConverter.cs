using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class StringCollectionsToStringConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(string))
            {
                var stringValue = value as string;
                if (stringValue != null)
                {
                    return stringValue;
                }
                else
                {
                    var parts = (IEnumerable<string>) value;
                    if (parts != null)
                    {
                        return String.Join(", ", parts);
                    }
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}