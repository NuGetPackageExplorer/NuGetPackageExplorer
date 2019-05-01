using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGet.Versioning;
using NuGetPe;

namespace PackageExplorer
{
    public class VersionSpecConverter : IValueConverter
    {
        #region IValueConverter Members

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var versionSpec = (VersionRange)value;
            return versionSpec == null ? null : ManifestUtility.ReplaceMetadataWithToken(versionSpec.ToShortString());
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string?)value;
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            else
            {
                stringValue = ManifestUtility.ReplaceTokenWithMetadata(stringValue);

                if (VersionRange.TryParse(stringValue, out var versionSpec))
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
