using System;
using System.Globalization;
using System.Windows.Controls;

namespace PackageExplorer
{
    public class NetSemanticVersionValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var stringValue = (string)value;
            if (string.IsNullOrEmpty(stringValue))
            {
                return ValidationResult.ValidResult;
            }

            if (stringValue.Contains("$"))
            {
                return ValidationResult.ValidResult;
            }

            if (Version.TryParse(stringValue, out _))
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "Version is in incorrect format. Examples of valid versions include '1.0', '2.0.1', '1.2.3.4'.");
            }
        }
    }
}
