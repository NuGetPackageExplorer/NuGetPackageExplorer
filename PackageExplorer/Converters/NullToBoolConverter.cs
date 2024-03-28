using System;
using System.Globalization;

#if HAS_UNO
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using _CultureInfo = System.String;
#else
using System.Windows;
using _CultureInfo = System.Globalization.CultureInfo;
using System.Windows.Data;
#endif

namespace PackageExplorer
{
    public class NullToBoolConverter : IValueConverter
    {
#region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrWhiteSpace(stringValue);
            }

            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            throw new NotImplementedException();
        }

#endregion
    }
}
