using System;
using System.Globalization;
using System.Windows.Controls;
using NuGet.Versioning;

namespace PackageExplorer
{
    public class SemanticVersionValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string stringValue = (string)value;
            if (String.IsNullOrEmpty(stringValue))
            {
                return ValidationResult.ValidResult;
            }

            if (stringValue.Contains("$"))
            {
                return ValidationResult.ValidResult;
            }

            SemanticVersion version;
            if (SemanticVersion.TryParse(stringValue, out version))
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "Version is in incorrect format. Examples of valid versions include '1.0', '2.0.1-alpha', '1.2.3.4-RC'.");
            }
        }
    }
}
