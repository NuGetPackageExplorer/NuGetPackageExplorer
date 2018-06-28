using System;
using System.Globalization;
using System.Windows.Data;
using Humanizer;
using NuGet.ProjectManagement;

namespace PackageExplorer
{
    public class DateTimeOffsetHumanizeConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                if (dateTimeOffset != Constants.Unpublished)
                {
                    return dateTimeOffset.LocalDateTime.Humanize(false, null, culture);
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
