using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer
{
    public class VersionSpecConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var versionSpec = (IVersionSpec) value;
            return versionSpec == null ? null : versionSpec.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string) value;
            if (String.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            else
            {
                IVersionSpec versionSpec;
                if (VersionUtility.TryParseVersionSpec(stringValue, out versionSpec))
                {
                    return versionSpec;
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }
        }

        #endregion
    }
}