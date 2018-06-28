using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using NuGet.Frameworks;

namespace PackageExplorer
{
    public class FrameworkAssemblyReferenceConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var frameworkNames = (IEnumerable<NuGetFramework>)value;
            return frameworkNames == null ? string.Empty : string.Join("; ", frameworkNames.Select(fn => fn.DotNetFrameworkName));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string)value;
            if (!string.IsNullOrEmpty(stringValue))
            {
                var parts = stringValue.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    var names = new NuGetFramework[parts.Length];
                    for (var i = 0; i < parts.Length; i++)
                    {
                        try
                        {
                            names[i] = NuGetFramework.Parse(parts[i]);
                            if (names[i] == NuGetFramework.UnsupportedFramework)
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
            return new NuGetFramework[0];
        }

        #endregion
    }
}