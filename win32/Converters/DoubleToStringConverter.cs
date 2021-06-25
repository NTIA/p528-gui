using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace p528_gui.Converters
{
    public class DoubleToStringConverter : IValueConverter
    {
        /// <summary>
        /// Convert from a Double to a String
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICollection<double>)
            {
                var l = new List<string>();
                foreach (var v in value as ICollection<double>)
                    l.Add(v.ToString());

                return l;
            }
            else
            {
                double v = (double)value;
                return v.ToString();
            }
        }

        /// <summary>
        /// Convert from a String to a Double
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (string)value;
            if (String.IsNullOrEmpty(s))
                return null;

            return Double.Parse(s);
        }
    }
}
