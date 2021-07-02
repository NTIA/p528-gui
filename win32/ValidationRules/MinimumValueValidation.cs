using System;
using System.Globalization;
using System.Windows.Controls;

namespace P528GUI.ValidationRules
{
    class MinimumValueValidation : ValidationRule
    {
        public double MinimumValue { get; set; }

        public bool IncludeMinimumValue { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (String.IsNullOrEmpty(value as String))
                return new ValidationResult(false, "Missing input value.");

            if (!Double.TryParse(value.ToString(), out double val))
                return new ValidationResult(false, "Value must be a number.");

            if (IncludeMinimumValue)
            {
                if (val < MinimumValue)
                    return new ValidationResult(false, $"Value must be >= {MinimumValue}.");
            }
            else
            {
                if (val <= MinimumValue)
                    return new ValidationResult(false, $"Value must be > {MinimumValue}.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
