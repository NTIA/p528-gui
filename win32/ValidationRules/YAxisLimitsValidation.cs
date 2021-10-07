using P528GUI.Windows;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace P528GUI.ValidationRules
{
    class YAxisLimitsValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!(value is BindingGroup bindingGroup))
                return new ValidationResult(false, "Invalid use of validation rule.");

            if (bindingGroup.Items.Count == 0)
                return ValidationResult.ValidResult;

            var wndw = (AxisLimitsWindow)bindingGroup.Items[0];

            if (wndw.YAxisMaximum <= wndw.YAxisMinimum)
                return new ValidationResult(false, "Y-Axis values are invalid");

            return new ValidationResult(true, null);
        }
    }
}
