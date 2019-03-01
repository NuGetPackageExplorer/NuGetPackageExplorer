using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using NuGet.Frameworks;

namespace PackageExplorer
{
    public class FrameworkNameConverter : IValueConverter
    {
        private static readonly string[] _wellknownPackageFolders = new string[] { "content", "lib", "tools", "build", "ref" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string)value;
            var name = Path.GetFileName(path);

            var parts = path.Split('\\');
            if (parts.Length == 2 &&
                _wellknownPackageFolders.Any(s => s.Equals(parts[0], StringComparison.OrdinalIgnoreCase)))
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
