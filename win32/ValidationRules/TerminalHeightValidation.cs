using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace p528_gui.ValidationRules
{
    class TerminalHeightValidation : ValidationRule
    {
        private const double MINIMUM = 1.5;
        private const double MAXIMUM = 20000;

        private readonly string InvalidInput = $"Terminal height must be between {MINIMUM} and {MAXIMUM} meters, inclusive.";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty((string)value) ||
                !Double.TryParse(value.ToString(), out double height) ||
                height < MINIMUM ||
                height > MAXIMUM)
                return new ValidationResult(false, InvalidInput);

            return new ValidationResult(true, null);
        }
    }
}
