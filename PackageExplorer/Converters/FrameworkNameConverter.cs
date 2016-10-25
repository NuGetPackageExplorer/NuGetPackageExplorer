using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Data;
using NuGetPe;

namespace PackageExplorer
{
    public class FrameworkNameConverter : IValueConverter
    {
        private static string[] WellknownPackageFolders = new string[] { "content", "lib", "tools", "build" };

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string) value;
            string name = Path.GetFileName(path);

            string[] parts = path.Split('\\');
            if (parts.Length == 2 && 
                WellknownPackageFolders.Any(s => s.Equals(parts[0], StringComparison.OrdinalIgnoreCase)))
            {
                FrameworkName frameworkName;
                try
                {
                    frameworkName = NuGet.VersionUtility.ParseFrameworkName(name);
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
                        return String.Empty;
                    }
                }

                if (frameworkName != NuGet.VersionUtility.UnsupportedFrameworkName)
                {
                    return " (" + frameworkName + ")";
                }
                else if (!parts[0].Equals("content", StringComparison.OrdinalIgnoreCase))
                {
                    return " (Unrecognized framework)";
                }
            }

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}