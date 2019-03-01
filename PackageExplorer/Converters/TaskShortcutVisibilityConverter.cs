using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    public class TaskShortcutVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null)
            {
                if (values[1] is bool val1)
                {
                    return val1 ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
