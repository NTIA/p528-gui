using System;
using System.Globalization;
using System.Windows.Controls;

namespace P528GUI.ValidationRules
{
    class TerminalHeightValidation : ValidationRule
    {
        private const double MIN_METERS = 1.5;
        private const double MAX_METERS = 80000;

        private readonly string InvalidInput__Meter = $"Terminal height must be between {MIN_METERS} and {MAX_METERS} meters, inclusive.";
        private readonly string InvalidInput__Feet = $"Terminal height must be between {Math.Round(MIN_METERS / Constants.METER_PER_FOOT, 0)} and {Math.Round(Math.Floor(MAX_METERS / Constants.METER_PER_FOOT), 0)} feet, inclusive.";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double height;
            if (value is string)
            {
                if (string.IsNullOrEmpty((string)value) ||
                    !Double.TryParse(value.ToString(), out height))
                    return new ValidationResult(false, GetValidationError());
            }
            else
                height = (double)value;

            // convert to meters
            double h__meter;
            if (GlobalState.Units == Units.Meters)
                h__meter = height;
            else
                h__meter = height * Constants.METER_PER_FOOT;

            if (h__meter < MIN_METERS || h__meter > MAX_METERS)
                return new ValidationResult(false, GetValidationError());

            return ValidationResult.ValidResult;
        }

        private string GetValidationError()
            => (GlobalState.Units == Units.Meters) ? InvalidInput__Meter : InvalidInput__Feet;
    }
}
