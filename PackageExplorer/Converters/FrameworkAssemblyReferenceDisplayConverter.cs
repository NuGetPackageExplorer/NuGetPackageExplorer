using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Data;
using NuGet.Packaging;

namespace PackageExplorer
{
    public class FrameworkAssemblyReferenceDisplayConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var far = (FrameworkAssemblyReference) value;
            return far == null ? String.Empty : String.Join("; ", far.SupportedFrameworks.Select(fn => fn.DotNetFrameworkName));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}