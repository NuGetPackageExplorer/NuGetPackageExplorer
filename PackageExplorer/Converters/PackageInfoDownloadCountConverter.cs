using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer
{
    public class PackageInfoDownloadCountConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var items = (IEnumerable<object>) value;
            return items.OfType<PackageInfo>().Aggregate(0, (sum, p) => sum + p.VersionDownloadCount).ToString("N0", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}