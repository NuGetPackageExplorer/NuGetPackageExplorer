using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Data;
using NuGetPe;

namespace PackageExplorer
{
    public class FrameworkAssemblyReferenceConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var frameworkNames = (IEnumerable<FrameworkName>) value;
            return frameworkNames == null ? String.Empty : String.Join("; ", frameworkNames);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string) value;
            if (!String.IsNullOrEmpty(stringValue))
            {
                string[] parts = stringValue.Split(new[] {';', ','}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    var names = new FrameworkName[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        try
                        {
                            names[i] = NuGet.VersionUtility.ParseFrameworkName(parts[i]);
                            if (names[i] == NuGet.VersionUtility.UnsupportedFrameworkName)
                            {
                                return DependencyProperty.UnsetValue;
                            }
                        }
                        catch (ArgumentException)
                        {
                            return DependencyProperty.UnsetValue;
                        }
                    }
                    return names;
                }
            }
            return new FrameworkName[0];
        }

        #endregion
    }
}