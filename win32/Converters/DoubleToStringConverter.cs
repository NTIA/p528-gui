using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace p528_gui.Converters
{
    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICollection<double>)
            {
                var l = new List<string>();
                foreach (var v in (value as ICollection<Double>))
                    l.Add(v.ToString());

                return l;
            }
            else
            {
                double v = (double)value;
                return v.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string v = (string)value;
            if (String.IsNullOrEmpty(v))
                return null;

            return Double.Parse(v);
        }
    }
}
