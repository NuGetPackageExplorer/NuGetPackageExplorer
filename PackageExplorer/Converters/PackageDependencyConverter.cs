using System;
using NuGet.Packaging.Core;
using NuGetPe;

#if HAS_UNO
using Microsoft.UI.Xaml.Data;

using _CultureInfo = System.String;
#else
using System.Windows.Data;

using _CultureInfo = System.Globalization.CultureInfo;
#endif

namespace PackageExplorer
{
    public class PackageDependencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var dependency = (PackageDependency)value;

            return $"{dependency.Id} {ManifestUtility.ReplaceMetadataWithToken(dependency.VersionRange.PrettyPrint())}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
