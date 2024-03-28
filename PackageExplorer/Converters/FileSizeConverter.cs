using System;
using System.Globalization;

#if HAS_UNO
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using _CultureInfo = System.String;
#else
using System.Windows;
using _CultureInfo = System.Globalization.CultureInfo;
using System.Windows.Data;
#endif

namespace PackageExplorer
{
    public class FileSizeConverter : IValueConverter
    {
#region IValueConverter Members

        public object Convert(object value, Type targetType, object? parameter, _CultureInfo culture)
        {
#if HAS_UNO
            var cultureInfo = new CultureInfo(culture);
#else
            var cultureInfo = culture;
#endif

            return ConvertFileSizeToString((long)value, cultureInfo);
        }

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            throw new NotImplementedException();
        }

#endregion

        internal static string ConvertFileSizeToString(long fileSize, CultureInfo culture)
        {
            var sizes = new[] { 1024L * 1024 * 1024, 1024 * 1024, 1024, 1 };
            var unit = new[] { " GB", " MB", " KB", " bytes" };

            for (var i = 0; i < sizes.Length; i++)
            {
                if (fileSize >= sizes[i])
                {
                    if (fileSize % sizes[i] == 0)
                    {
                        var f = fileSize / sizes[i];
                        return f.ToString(culture) + unit[i];
                    }
                    else
                    {
                        var f = fileSize * 1.0 / sizes[i];
                        return f.ToString("F0", culture) + unit[i];
                    }
                }
            }

            return fileSize.ToString(culture);
        }
    }
}
