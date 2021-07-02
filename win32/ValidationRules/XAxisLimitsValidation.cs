using P528GUI.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace P528GUI.ValidationRules
{
    class XAxisLimitsValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!(value is BindingGroup bindingGroup))
                return new ValidationResult(false, "Invalid use.");

            if (bindingGroup.Items.Count == 0)
                return ValidationResult.ValidResult;

            var wndw = (AxisLimitsWindow)bindingGroup.Items[0];

            if (wndw.XAxisMaximum <= wndw.XAxisMinimum)
                return new ValidationResult(false, "X-Axis values are invalid");

            return ValidationResult.ValidResult;
        }
    }
}
