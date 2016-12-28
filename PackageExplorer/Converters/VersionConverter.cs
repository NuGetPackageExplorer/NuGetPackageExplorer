using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGetPe;

namespace PackageExplorer
{
    [ValueConversion(typeof(TemplatebleSemanticVersion), typeof(string))]
    public class VersionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version =  value as TemplatebleSemanticVersion;
            return version?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string) value;
            if (String.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }
            else
            {
                TemplatebleSemanticVersion version;
                if (TemplatebleSemanticVersion.TryParse(stringValue, out version))
                {
                    return version;
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }
        }

        #endregion
    }
}