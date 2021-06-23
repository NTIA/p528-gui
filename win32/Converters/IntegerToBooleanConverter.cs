using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace p528_gui.Converters
{
    /// <summary>
    /// This performs the conversion based on the number of errors, and whether validation has
    /// has passed.  It is not converting based on standard integer representations of True/False.
    /// </summary>
    public class IntegerToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((int)value == 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
