using System;
using System.Text;
using NuGet.Packaging;

#if HAS_UNO || USE_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using _CultureInfo = System.String;
#else
using System.Windows;
using System.Windows.Data;

using _CultureInfo = System.Globalization.CultureInfo;
#endif

namespace PackageExplorer
{
#if !HAS_UNO && !USE_WINUI
    [ValueConversion(typeof(Uri), typeof(Visibility))]
#endif
    public class LicenseUrlToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            if (value is Uri licenseUrl)
            {
                return licenseUrl != LicenseMetadata.LicenseFileDeprecationUrl
                    ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture) => throw new NotSupportedException("Only one-way conversion is supported.");
    }
}
