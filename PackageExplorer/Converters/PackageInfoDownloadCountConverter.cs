using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer {
    public class PackageInfoDownloadCountConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            var items = (IEnumerable<object>)value;
            return items.OfType<PackageInfo>().Aggregate<PackageInfo, int>(0, (sum, p) => sum + p.VersionDownloadCount).ToString(culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
