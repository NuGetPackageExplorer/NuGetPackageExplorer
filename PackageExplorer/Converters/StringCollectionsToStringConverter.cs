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
                if (value is string stringValue)
                {
                    return stringValue;
                }
                else
                {
                    var parts = (IEnumerable<string>)value;
                    if (parts != null)
                    {
                        return string.Join(", ", parts);
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