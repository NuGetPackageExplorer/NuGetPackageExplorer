using System;
using System.Text;
using NuGet.Packaging;

#if HAS_UNO
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
#if !HAS_UNO
    [ValueConversion(typeof(LicenseMetadata), typeof(string))]
#endif
    public class LicenseMetadataConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, _CultureInfo culture)
        {
            if (value is LicenseMetadata metadata)
            {
                if (parameter as string != "detailed")
                {
                    // note: We can't distinguish between an explicit v1.0.0 vs undeclared.
                    return metadata.Version != LicenseMetadata.EmptyVersion
                        ? $"{metadata.License} v{metadata.Version}"
                        : metadata.License;
                }
                else
                {
                    var sb = new StringBuilder();

                    if (metadata.Type == LicenseType.Expression)
                    {
                        sb
                            .AppendLine($"{Resources.Dialog_LicenseExpression} {metadata.LicenseExpression}")
                            .AppendLine($"{Resources.Dialog_LicenseExpressionType} {metadata.Type}")
                            .AppendLine($"{Resources.Dialog_LicenseExpressionVersion} {metadata.Version}");
                    }
                    else if (metadata.Type == LicenseType.File)
                    {
                        sb
                            .AppendLine($"License: {metadata.License}")
                            .AppendLine($"Type: {metadata.Type}")
                            .AppendLine($"License Url: {metadata.LicenseUrl}");
                    }

                    return sb.ToString().TrimEnd();
                }
            }

            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, _CultureInfo culture) => throw new NotSupportedException("Only one-way conversion is supported.");
    }
}
