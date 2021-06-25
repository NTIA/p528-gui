using System;
using System.Globalization;
using System.Windows.Data;

namespace p528_gui.Converters
{
    /// <summary>
    /// This performs the conversion based on the number of errors, and whether validation has
    /// has passed.  It is not converting based on standard integer representations of True/False.
    /// </summary>
    public class IntegerToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Convert from Integer to Boolean
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value == 0;
        }

        /// <summary>
        /// Convert from Boolean to Integer
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
