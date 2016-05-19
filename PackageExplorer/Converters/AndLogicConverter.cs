using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    public class AndLogicConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(v => v == null || v == DependencyProperty.UnsetValue))
            {
                return false;
            }

            return values.Cast<bool>().All(a => a);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}