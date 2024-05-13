using System;
using System.ComponentModel;

#if HAS_UNO || USE_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using _CultureInfo = System.String;
#else
using System.Windows;
using System.Windows.Data;

using _CultureInfo = System.Globalization.CultureInfo;
#endif

namespace PackageExplorer
{
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            if (value == null) return DependencyProperty.UnsetValue;

            return GetDescription((Enum)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            return value;
        }

        private static string GetDescription(Enum en)
        {
            var type = en.GetType();
            var memInfo = type.GetMember(en.ToString());
            if (memInfo != null && memInfo.Length > 0)
            {
                var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            return en.ToString();
        }
    }
}
