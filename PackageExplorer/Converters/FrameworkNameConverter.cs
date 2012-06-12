using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer
{
    public class FrameworkNameConverter : IValueConverter
    {
        private static string[] WellknownPackageFolders = new string[] { "content", "lib", "tools" };

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string) value;
            string name = Path.GetFileName(path);

            string[] parts = path.Split('\\');
            if (parts.Length == 2 && 
                WellknownPackageFolders.Any(s => s.Equals(parts[0], StringComparison.OrdinalIgnoreCase)))
            {
                FrameworkName frameworkName = VersionUtility.ParseFrameworkName(name);
                if (frameworkName != VersionUtility.UnsupportedFrameworkName)
                {
                    return " (" + frameworkName + ")";
                }
                else
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