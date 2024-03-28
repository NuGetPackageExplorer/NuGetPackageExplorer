using System;
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
    public class FrameworkAssemblyReferenceDisplayConverter : IValueConverter
    {
        #region IValueConverter Members

        public object? Convert(object value, Type targetType, object parameter, _CultureInfo culture)
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

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
