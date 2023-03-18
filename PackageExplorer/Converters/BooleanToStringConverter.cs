using System;

#if HAS_UNO
using Windows.UI.Xaml.Data;

using _CultureInfo = System.String;
#else
using _CultureInfo = System.Globalization.CultureInfo;
using System.Windows.Data;
#endif

namespace PackageExplorer
{
    public class BooleanToStringConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Yes" : "No";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}