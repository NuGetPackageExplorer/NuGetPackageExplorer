using System;
using System.Globalization;
using System.Windows.Controls;

namespace PackageExplorer
{
    public class PublishApiKeyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var key = (string) value;
            key = key.ToUpper(CultureInfo.InvariantCulture);

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                bool isValid = Char.IsDigit(c) || c == '-' || (c >= 'A' && c <= 'F');
                if (!isValid)
                {
                    return new ValidationResult(false, "'" + c + "' is an invalid character.");
                }
            }

            return ValidationResult.ValidResult;
        }
    }
}