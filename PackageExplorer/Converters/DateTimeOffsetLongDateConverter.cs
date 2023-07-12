using System;
using System.Globalization;
using NuGet.ProjectManagement;

#if HAS_UNO
using Microsoft.UI.Xaml.Data;
using _CultureInfo = System.String;
#else
using System.Windows.Data;
using _CultureInfo = System.Globalization.CultureInfo;
#endif

namespace PackageExplorer
{
    public class DateTimeOffsetLongDateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object? Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                if (dateTimeOffset != Constants.Unpublished)
                {
                    var format = parameter as string;
                    if (!string.IsNullOrWhiteSpace(format))
                    {
#if HAS_UNO
                        return dateTimeOffset.LocalDateTime.ToString(format, new CultureInfo(culture));
#else
                        return dateTimeOffset.LocalDateTime.ToString(format, culture);
#endif
                    }

                    return dateTimeOffset.LocalDateTime.ToLongDateString();
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
