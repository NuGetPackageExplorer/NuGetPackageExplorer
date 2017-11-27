using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using NuGet.Packaging;

namespace PackageExplorer
{
    public class DependencySetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dependencySets = (ICollection<PackageDependencyGroup>)value;
            if (dependencySets.Any(d => (d.TargetFramework != null && !d.TargetFramework.IsAny)))
            {
                // if there is at least one dependeny set with non-null target framework,
                // we show the dependencies grouped by target framework.
                return dependencySets;
            }

            // otherwise, flatten the groups into a single list of dependencies
            return dependencySets.SelectMany(d => d.Packages);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
