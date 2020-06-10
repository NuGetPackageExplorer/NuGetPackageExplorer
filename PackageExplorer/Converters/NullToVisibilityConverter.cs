using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Inverted { get; set; }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                Visibility returnValue;

                if (value is string stringValue)
                {
                    returnValue = string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
                }
                else if(value is ICollection collection)
                {
                    returnValue = collection.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else if (value is IEnumerable enu)
                {
                    returnValue = enu.Cast<object>().Any() ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    returnValue = value == null ? Visibility.Collapsed : Visibility.Visible;
                }

                if (Inverted)
                {
                    if (returnValue == Visibility.Visible)
                    {
                        returnValue = Visibility.Collapsed;
                    }
                    else
                    {
                        returnValue = Visibility.Visible;
                    }
                }

                return returnValue;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
