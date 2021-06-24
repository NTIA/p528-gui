﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace p528_gui.ValidationRules
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