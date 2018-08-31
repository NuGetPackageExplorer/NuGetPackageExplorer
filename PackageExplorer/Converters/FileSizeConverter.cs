using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class FileSizeConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertFileSizeToString((long)value, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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