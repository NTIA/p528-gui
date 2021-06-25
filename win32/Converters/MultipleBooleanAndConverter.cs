using System;
using System.Globalization;
using System.Windows.Data;

namespace p528_gui.Converters
{
    public class MultipleBooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool rtn = true;

            for (int i = 0; i < values.Length; i++)
                rtn &= (bool)values[i];

            return rtn;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
