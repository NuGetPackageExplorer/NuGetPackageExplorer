using NuGet.Versioning;

using NuGetPe;

#if HAS_UNO || USE_WINUI
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
#if !HAS_UNO && !USE_WINUI
    [ValueConversion(typeof(NuGetVersion), typeof(Visibility))]
#endif
    public partial class NuGetVersionPreReleaseConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, _CultureInfo language)
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

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo language)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
