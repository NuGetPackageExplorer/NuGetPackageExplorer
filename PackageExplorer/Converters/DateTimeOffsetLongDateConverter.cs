using System;
using System.Globalization;
using System.Windows.Data;
using NuGet.ProjectManagement;

namespace PackageExplorer
{
    public class DateTimeOffsetLongDateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                if (dateTimeOffset != Constants.Unpublished)
                {
                    var format = parameter as string;
                    if (!string.IsNullOrWhiteSpace(format))
                    {
                        return dateTimeOffset.LocalDateTime.ToString(format);
                    }

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
