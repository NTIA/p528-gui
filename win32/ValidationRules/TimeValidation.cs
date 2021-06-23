using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace p528_gui.ValidationRules
{
    class TimeValidation : ValidationRule
    {
        private const double MINIMUM = 1;
        private const double MAXIMUM = 99;

        private readonly string InvalidInput = $"Time percentage must be between {MINIMUM} and {MAXIMUM}, inclusive.";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty((string)value) ||
                !Double.TryParse(value.ToString(), out double time) ||
                time < MINIMUM ||
                time > MAXIMUM)
                return new ValidationResult(false, InvalidInput);

            return new ValidationResult(true, null);
        }
    }
}
