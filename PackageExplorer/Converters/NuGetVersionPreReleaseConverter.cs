using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGet.Versioning;
using NuGetPe;

namespace PackageExplorer
{
    [ValueConversion(typeof(NuGetVersion), typeof(Visibility))]
    public class NuGetVersionPreReleaseConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = value as NuGetVersion;

            if (version != null)
            {
                if (!version.IsTokenized() && version.IsPrerelease)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}