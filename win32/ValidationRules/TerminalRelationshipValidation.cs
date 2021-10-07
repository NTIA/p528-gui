using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace P528GUI.ValidationRules
{
    class TerminalRelationshipValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!(value is BindingGroup bindingGroup))
                return new ValidationResult(false, "Invalid use of validation rule.");

            if (bindingGroup.Items.Count == 0)
                return ValidationResult.ValidResult;

            // grab the terminal heights
            double h_1 = Convert.ToDouble((bindingGroup.BindingExpressions[0].Target as TextBox).Text);
            double h_2 = Convert.ToDouble((bindingGroup.BindingExpressions[1].Target as TextBox).Text);

            if (h_1 > h_2)
                return new ValidationResult(false, "Terminal 1 must be less than or equal to Terminal 2.");

            return ValidationResult.ValidResult;
        }
    }
}
