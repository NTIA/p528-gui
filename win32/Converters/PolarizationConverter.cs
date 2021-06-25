using System;
using System.Globalization;
using System.Windows.Data;
using ITS.Propagation;

namespace p528_gui.Converters
{
    class PolarizationConverter : IValueConverter
    {
        /// <summary>
        /// Convert from GUI Polarization to P528.Polarization
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var vs = (Polarization[])value;
            var ls = new P528.Polarization[2];

            for (int i = 0; i < vs.Length; i++)
                ls[i] = (P528.Polarization)((int)vs[i]);

            return ls;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
