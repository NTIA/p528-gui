using System;
using System.Globalization;
using System.Windows.Controls;

namespace P528GUI.ValidationRules
{
    class FrequencyValidation : ValidationRule
    {
        private const double MINIMUM = 100;
        private const double MAXIMUM = 30000;

        private readonly string InvalidInput = $"Frequency must be between {MINIMUM} and {MAXIMUM} MHz, inclusive.";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty((string)value) ||
                !Double.TryParse(value.ToString(), out double frequency) ||
                frequency < MINIMUM ||
                frequency > MAXIMUM)
                return new ValidationResult(false, InvalidInput);

            return ValidationResult.ValidResult;
        }
    }
}
