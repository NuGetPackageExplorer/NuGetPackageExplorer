﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGet.Versioning;

namespace PackageExplorer
{
    public class VersionSpecConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var versionSpec = (VersionRange) value;
            return versionSpec == null ? null : versionSpec.ToShortString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string) value;
            if (String.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            else
            {
                if (VersionRange.TryParse(stringValue, out VersionRange versionSpec))
                {
                    return versionSpec;
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