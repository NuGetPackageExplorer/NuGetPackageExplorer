using System;
using System.Globalization;
using NuGet.Versioning;
using NuGetPe;

#if HAS_UNO || USE_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using _CultureInfo = System.String;
#else
using System.Windows;
using _CultureInfo = System.Globalization.CultureInfo;
using System.Windows.Data;
#endif

namespace PackageExplorer
{
#if !HAS_UNO && !USE_WINUI
    [ValueConversion(typeof(NuGetVersion), typeof(string))]
#endif
    public class NuGetVersionConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            var version = value as NuGetVersion;
            return ManifestUtility.ReplaceMetadataWithToken(version?.ToFullString());
        }

        public object? ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
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
