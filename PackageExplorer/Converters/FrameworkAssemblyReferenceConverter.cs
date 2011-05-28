using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Data;
using NuGet;

namespace PackageExplorer {
    public class FrameworkAssemblyReferenceConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            var frameworkNames = (IEnumerable<FrameworkName>)value;
            return frameworkNames == null ? String.Empty : String.Join("; ", frameworkNames);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string stringValue = (string)value;
            if (!String.IsNullOrEmpty(stringValue)) {
                string[] parts = stringValue.Split(new char[] {';', ','}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0) {
                    FrameworkName[] names = new FrameworkName[parts.Length];
                    for (int i = 0; i < parts.Length; i++) {
                        try {
                            names[i] = VersionUtility.ParseFrameworkName(parts[i]);
                            if (names[i] == VersionUtility.UnsupportedFrameworkName) {
                                return DependencyProperty.UnsetValue;
                            }
                        }
                        catch (ArgumentException) {
                            return DependencyProperty.UnsetValue;
                        }
                    }
                    return names;
                }
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
