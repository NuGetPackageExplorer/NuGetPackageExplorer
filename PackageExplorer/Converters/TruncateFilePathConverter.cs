using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.IO;

namespace PackageExplorer
{
    public class TruncateFilePathConverter : IValueConverter
    {
        const int MaxLength = 50;

        public TruncateFilePathConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string path = (string)value;
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

            string[] parts = path.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            int remainingLength = MaxLength - prefix.Length - 3;    // 3 is the length of '...'
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

            return prefix + "..." + res;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
