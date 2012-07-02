using System;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer
{
    public class TargetFrameworkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            FrameworkName framework = (FrameworkName)value;
            if (framework == null) 
            {
                return null;
            }

            return VersionUtility.GetShortFrameworkName(framework);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string stringValue = (string)value;
            if (String.IsNullOrEmpty(stringValue))
            {
                return null;
            }

            FrameworkName framework = VersionUtility.ParseFrameworkName(stringValue);
            if (framework == VersionUtility.UnsupportedFrameworkName)
            {
                return DependencyProperty.UnsetValue;
            }

            return framework;
        }
    }
}