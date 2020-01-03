using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGet.Versioning;
using NuGetPe;

namespace PackageExplorer
{
    [ValueConversion(typeof(NuGetVersion), typeof(string))]
    public class NuGetVersionConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = value as NuGetVersion;
            return ManifestUtility.ReplaceMetadataWithToken(version?.ToFullString());
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string?)value;
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }
            else
            {
                stringValue = ManifestUtility.ReplaceTokenWithMetadata(stringValue);
                if (NuGetVersion.TryParse(stringValue, out var version))
                {
                    return version;
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }
        }
    }
}
