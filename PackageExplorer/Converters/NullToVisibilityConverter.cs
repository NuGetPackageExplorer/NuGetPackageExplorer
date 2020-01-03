using System;
using System.Globalization;
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
