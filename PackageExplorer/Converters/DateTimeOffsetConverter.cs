using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class DateTimeOffsetConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTimeOffset)
            {
                return ((DateTimeOffset)value).DateTime.ToLocalTime().ToString();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
