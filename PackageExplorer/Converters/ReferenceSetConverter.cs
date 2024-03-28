using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging;

#if HAS_UNO
using Microsoft.UI.Xaml.Data;

using _CultureInfo = System.String;
#else
using System.Windows.Data;

using _CultureInfo = System.Globalization.CultureInfo;
#endif

namespace PackageExplorer
{
    public class ReferenceSetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            var referenceSets = (ICollection<PackageReferenceSet>)value;
            if (referenceSets.Any(d => d.TargetFramework != null))
            {
                // if there is at least one dependency set with non-null target framework,
                // we show the dependencies grouped by target framework.
                return referenceSets;
            }

            // otherwise, flatten the groups into a single list of dependencies
            return referenceSets.SelectMany(d => d.References);
        }

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
