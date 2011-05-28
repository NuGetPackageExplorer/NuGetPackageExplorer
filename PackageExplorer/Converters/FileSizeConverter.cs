using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long fileSize = (long)value;

            long[] sizes = new long[] { 1024L * 1024 * 1024, 1024 * 1024, 1024, 1 };
            string[] unit = new string[] { " GB", " MB", " KB", " bytes" };

            for (int i = 0; i < sizes.Length; i++)
            {
                if (fileSize >= sizes[i])
                {
                    if (fileSize % sizes[i] == 0)
                    {
                        long f = fileSize / sizes[i];
                        return f.ToString(CultureInfo.CurrentCulture) + unit[i];
                    }
                    else
                    {
                        double f = fileSize * 1.0 / sizes[i];
                        return f.ToString("F2", CultureInfo.CurrentCulture) + unit[i];
                    }
                }
            }

            return fileSize.ToString(CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
