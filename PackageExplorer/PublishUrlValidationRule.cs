using System;
using System.Windows.Controls;

namespace PackageExplorer {
    public class PublishUrlValidationRule : ValidationRule {

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) {
            string stringValue = (string)value;
            Uri url;
            if (Uri.TryCreate(stringValue, UriKind.Absolute, out url)) {
                return ValidationResult.ValidResult;
            }
            else {
                return new ValidationResult(false, "Invalid Url.");
            }
        }
    }
}
