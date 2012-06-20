using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer
{
    public class DependencySetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dependencySets = (ICollection<PackageDependencySet>)value;
            if (dependencySets.Any(d => d.TargetFramework != null))
            {
                // if there is at least one dependeny set with non-null target framework,
                // we show the dependencies grouped by target framework.
                return dependencySets;
            }

            // otherwise, flatten the groups into a single list of dependencies
            return dependencySets.SelectMany(d => d.Dependencies);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
