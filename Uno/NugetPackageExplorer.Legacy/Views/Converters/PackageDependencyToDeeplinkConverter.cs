using System;
using System.Collections.Generic;
using System.Text;

using NuGet.Packaging.Core;

using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Converters
{
    public class PackageDependencyToDeeplinkConverter : IValueConverter
	{
		public enum DeeplinkType { SearchLink, PackageLink }

		public DeeplinkType ConvertTo { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not PackageDependency dependency) return null;

            var path = ConvertTo switch
            {
                DeeplinkType.SearchLink => $"/packages/?q={dependency.Id}",
                DeeplinkType.PackageLink => $"/packages/{dependency.Id}/{dependency.VersionRange.MinVersion}",

                _ => throw new NotImplementedException($"Conversion for '{ConvertTo}' is not implemented"),
            };

            return new Uri(path, UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
    }
}
