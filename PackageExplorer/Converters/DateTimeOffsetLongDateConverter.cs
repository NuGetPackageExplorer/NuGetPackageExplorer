using NuGet;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class DateTimeOffsetLongDateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTimeOffset)
            {
                DateTimeOffset dateTimeOffset = (DateTimeOffset)value;

                if (dateTimeOffset != Constants.Unpublished)
                {
                    return dateTimeOffset.LocalDateTime.ToLongDateString();
                }
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
