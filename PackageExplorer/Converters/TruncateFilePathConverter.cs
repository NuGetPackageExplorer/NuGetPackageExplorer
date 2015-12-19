using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace PackageExplorer
{
    public class TruncateFilePathConverter : IValueConverter
    {
        private const int MaxLength = 50;

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string) value;
            if (path == null)
            {
                return null;
            }
            else if (path.Length <= MaxLength)
            {
                return path;
            }
            else
            {
                return Truncate(path);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        private static string Truncate(string path)
        {
            char separator;
            string prefix = "";

            if (Path.IsPathRooted(path))
            {
                separator = Path.DirectorySeparatorChar;
                int index = path.IndexOf(Path.VolumeSeparatorChar);
                if (index > -1)
                {
                    prefix = path.Substring(0, Math.Min(path.Length, index + 2));
                    path = path.Substring(index + 2);
                }
            }
            else
            {
                separator = '/';
                int index = path.IndexOf(Uri.SchemeDelimiter, StringComparison.OrdinalIgnoreCase);
                if (index > -1)
                {
                    prefix = path.Substring(0, Math.Min(path.Length, index + 3));
                    path = path.Substring(index + 3);
                }
            }

            string[] parts = path.Split(new[] {separator}, StringSplitOptions.RemoveEmptyEntries);
            int remainingLength = MaxLength - prefix.Length - 3; // 3 is the length of '...'
            string res = "";
            for (int i = parts.Length - 1; i >= 0; --i)
            {
                if (res.Length + parts[i].Length + 1 <= remainingLength)
                {
                    res = separator + parts[i] + res;
                }
                else
                {
                    break;
                }
            }

            if (res.Length == 0 && parts.Length > 0)
            {
                string lastPart = parts[parts.Length - 1];
                res = lastPart.Substring(Math.Max(0, lastPart.Length - remainingLength));
            }

            return prefix + "..." + res;
        }
    }
}