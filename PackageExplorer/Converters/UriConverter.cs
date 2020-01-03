using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer
{
    public class UriConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var uri = (Uri)value;
            return uri?.ToString();
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string)value;
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            else
            {
                if (Uri.TryCreate(stringValue, UriKind.Absolute, out var uri))
                {
                    return uri;
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }
        }
    }
}
