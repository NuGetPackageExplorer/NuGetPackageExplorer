using System;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer
{
    public class FrameworkNameConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string) value;
            string name = Path.GetFileName(path);

            string[] parts = path.Split('\\');
            if (parts.Length == 2 && parts[0].Equals("LIB", StringComparison.OrdinalIgnoreCase))
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