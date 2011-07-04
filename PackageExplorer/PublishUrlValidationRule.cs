using System;
using System.Windows.Controls;

namespace PackageExplorer {
    public class PublishUrlValidationRule : ValidationRule {

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) {
            string stringValue = (string)value;
            Uri url;
            if (Uri.TryCreate(stringValue, UriKind.Absolute, out url)) {
                if (url.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    url.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)) {
                    return ValidationResult.ValidResult;
                }
                else {
                    return new ValidationResult(false, "Publish url must be an HTTP or HTTPS address.");
                }
            }
            else {
                return new ValidationResult(false, "Invalid publish url.");
            }
        }
    }
}
