using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using NuGet.Packaging;

namespace PackageExplorer
{
    public class ReferenceSetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var referenceSets = (ICollection<PackageReferenceSet>)value;
            if (referenceSets.Any(d => d.TargetFramework != null))
            {
                // if there is at least one dependeny set with non-null target framework,
                // we show the dependencies grouped by target framework.
                return referenceSets;
            }

            // otherwise, flatten the groups into a single list of dependencies
            return referenceSets.SelectMany(d => d.References);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
