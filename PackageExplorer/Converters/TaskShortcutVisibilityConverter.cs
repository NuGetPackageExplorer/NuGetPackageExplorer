using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    public class TaskShortcutVisibilityConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var visible = values[0] == null && (bool) values[1];
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}