using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ConvertFileSizeToString((long)value, culture);
        }

        internal static string ConvertFileSizeToString(long fileSize, CultureInfo culture)
        {
            long[] sizes = new long[] { 1024L * 1024 * 1024, 1024 * 1024, 1024, 1 };
            string[] unit = new string[] { " GB", " MB", " KB", " bytes" };

            for (int i = 0; i < sizes.Length; i++)
            {
                if (fileSize >= sizes[i])
                {
                    if (fileSize % sizes[i] == 0)
                    {
                        long f = fileSize / sizes[i];
                        return f.ToString(culture) + unit[i];
                    }
                    else
                    {
                        double f = fileSize * 1.0 / sizes[i];
                        return f.ToString("F0", culture) + unit[i];
                    }
                }
            }

            return fileSize.ToString(culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
