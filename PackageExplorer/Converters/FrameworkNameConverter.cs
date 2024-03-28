using System;
using System.IO;
using System.Linq;
using NuGet.Frameworks;

#if HAS_UNO
using Microsoft.UI.Xaml.Data;

using _CultureInfo = System.String;
#else
using System.Windows.Data;

using _CultureInfo = System.Globalization.CultureInfo;
#endif

namespace PackageExplorer
{
    public class FrameworkNameConverter : IValueConverter
    {
        private static readonly string[] WellknownPackageFolders = new string[] { "content", "lib", "tools", "build", "ref" };

        public object Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            if (value is NuGetFramework framework)
            {
                return framework.DotNetFrameworkName;
            }

            var path = (string)value;
            var name = Path.GetFileName(path);

            if (string.IsNullOrEmpty(path))
                return string.Empty;

            var parts = path.Split('\\');
            if (parts.Length == 2 &&
                WellknownPackageFolders.Any(s => s.Equals(parts[0], StringComparison.OrdinalIgnoreCase)))
            {
                NuGetFramework frameworkName;
                try
                {
                    frameworkName = NuGetFramework.Parse(name);
                }
                catch (ArgumentException)
                {
                    if (parts[0].Equals("lib", StringComparison.OrdinalIgnoreCase) ||
                        parts[0].Equals("build", StringComparison.OrdinalIgnoreCase))
                    {
                        return " (Invalid framework)";
                    }
                    else
                    {
                        return string.Empty;
                    }
                }

                if (!frameworkName.IsUnsupported)
                {
                    return $" ({frameworkName.DotNetFrameworkName})";
                }
                else if (!parts[0].Equals("content", StringComparison.OrdinalIgnoreCase))
                {
                    return " (Unrecognized framework)";
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
