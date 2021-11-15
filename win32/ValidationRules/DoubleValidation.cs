using System;
using System.Globalization;
using System.Windows.Controls;

namespace P528GUI.ValidationRules
{
    class DoubleValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (String.IsNullOrEmpty(value as String))
                return new ValidationResult(false, "Missing input value.");

            if (!Double.TryParse(value.ToString(), out _))
                return new ValidationResult(false, "Value must be a number.");

            return ValidationResult.ValidResult;
        }
    }
}
