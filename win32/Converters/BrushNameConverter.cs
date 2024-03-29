﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace P528GUI.Converters
{
    public class BrushNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = (SolidColorBrush)value;
            return Tools.LineColors[color];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
