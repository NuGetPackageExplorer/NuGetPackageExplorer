using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using NuGet.Packaging;

namespace PackageExplorer
{
    public class FrameworkAssemblyReferenceDisplayConverter : IValueConverter
    {
        #region IValueConverter Members

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var far = (FrameworkAssemblyReference)value;
            if (far == null)
            {
                return null;
            }

            var fxs = string.Join("; ", far.SupportedFrameworks.Select(fn => fn.DotNetFrameworkName));

            if (parameter as string == "includeAssembly")
            {
                return $"{far.AssemblyName} ({fxs})";
            }

            return fxs;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
