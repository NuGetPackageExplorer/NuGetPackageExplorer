using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGetPe;

namespace PackageExplorer
{
    public class PackageDependencyConverter : IValueConverter
    {
        VersionRangeFormatter formatter = new VersionRangeFormatter();
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var dependency = (PackageDependency)value;
            

            var range = formatter.Format("P", dependency.VersionRange, culture);

            return $"{dependency.Id} {range}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
