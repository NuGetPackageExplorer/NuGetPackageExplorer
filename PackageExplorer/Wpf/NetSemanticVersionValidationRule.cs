using System;
using System.Globalization;
using System.Windows.Controls;
using NuGet;

namespace PackageExplorer
{
    public class NetSemanticVersionValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string stringValue = (string)value;
            if (String.IsNullOrEmpty(stringValue))
            {
                return ValidationResult.ValidResult;
            }

            Version version;
            if (Version.TryParse(stringValue, out version))
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
