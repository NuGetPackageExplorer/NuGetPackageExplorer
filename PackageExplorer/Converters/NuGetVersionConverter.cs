﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NuGet.Packaging;
using NuGet.Versioning;
using NuGetPe;
using NuGetPe.Utility;

namespace PackageExplorer
{
    [ValueConversion(typeof(NuGetVersion), typeof(string))]
    public class NuGetVersionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version =  value as NuGetVersion;
            return ManifestUtility.ReplaceMetadataWithToken(version?.ToFullString());
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
                stringValue = ManifestUtility.ReplaceTokenWithMetadata(stringValue);
                NuGetVersion version;
                if (NuGetVersion.TryParse(stringValue, out version))
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