using System;
using System.Globalization;
using System.Windows.Controls;

namespace PackageExplorer
{
    public class PublishUrlValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var stringValue = (string)value;
            if (Uri.TryCreate(stringValue, UriKind.Absolute, out var url))
            {
                if (url.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    url.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationResult.ValidResult;
                }
                else
                {
                    return new ValidationResult(false, "Publish url must be an HTTP or HTTPS address.");
                }
            }
            else
            {
                return new ValidationResult(false, "Invalid publish url.");
            }
        }
    }
}