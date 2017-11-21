using System;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Data;
using NuGet.Frameworks;

namespace PackageExplorer
{
    public class TargetFrameworkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            NuGetFramework framework = (NuGetFramework)value;
            if (framework == null) 
            {
                return null;
            }
            
            return framework.GetShortFolderName();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string stringValue = (string)value;
            if (String.IsNullOrEmpty(stringValue))
            {
                return null;
            }

            var framework = NuGetFramework.Parse(stringValue);
            if (framework.IsUnsupported)
            {
                return DependencyProperty.UnsetValue;
            }

            return framework;
        }
    }
}