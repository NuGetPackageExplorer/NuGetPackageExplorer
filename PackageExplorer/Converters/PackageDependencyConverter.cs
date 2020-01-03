using System;
using System.Windows.Data;
using NuGet.Packaging.Core;
using NuGetPe;

namespace PackageExplorer
{
    public class PackageDependencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var dependency = (PackageDependency)value;

            return $"{dependency.Id} {ManifestUtility.ReplaceMetadataWithToken(dependency.VersionRange.PrettyPrint())}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
