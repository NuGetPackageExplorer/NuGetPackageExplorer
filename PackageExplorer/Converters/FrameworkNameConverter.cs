using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer {
    public class FrameworkNameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {

            var path = (string)value;
            var name = Path.GetFileName(path);

            string[] parts = path.Split('\\');
            if (parts.Length == 2 && parts[0].Equals("LIB", StringComparison.OrdinalIgnoreCase)) {
                var frameworkName = VersionUtility.ParseFrameworkName(name);
                if (frameworkName != VersionUtility.UnsupportedFrameworkName) {
                    return " (" + frameworkName.ToString() + ")";
                }
                else {
                    return " (Unrecognized framework)";
                }
            }

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
