using System;
using System.Windows;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer {
    public class VersionSpecConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            IVersionSpec versionSpec = (IVersionSpec)value;
            return versionSpec == null ? null : versionSpec.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string stringValue = (string)value;
            if (String.IsNullOrEmpty(stringValue)) {
                return null;
            }
            else {
                IVersionSpec versionSpec;
                if (VersionUtility.TryParseVersionSpec(stringValue, out versionSpec)) {
                    return versionSpec;
                }
                else {
                    return DependencyProperty.UnsetValue;
                }
            }
        }
    }
}
