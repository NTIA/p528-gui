using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace p528_gui
{
    static class Tools
    {
        static private SolidColorBrush[] _listOfBrushes = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Purple, Brushes.Pink, Brushes.Orange, Brushes.Brown };

        static public bool ValidationError(TextBox tb)
        {
            tb.Background = Brushes.LightPink;
            return false;
        }

        static public void ValidationSuccess(TextBox tb)
        {
            tb.Background = Brushes.White;
        }

        static public double ConvertMetersToSpecifiedUnits(double meters, Units units)
        {
            return (units == Units.Meters) ? meters : (meters / Constants.METER_PER_FOOT);
        }

        static public double ConvertSpecifiedUnitsToKm(double value, Units units)
        {
            var value__meters = (units == Units.Meters) ? value : (value * Constants.METER_PER_FOOT);

            return value__meters / 1000.0;
        }

        static public bool ValidateH1(string H1, Units units, out double h_1)
        {
            if (String.IsNullOrEmpty(H1) ||
                !Double.TryParse(H1, out h_1) ||
                h_1 < Tools.ConvertMetersToSpecifiedUnits(1.5, units))
            {
                MessageBox.Show("Terminal 1 must be at least " + ((units == Units.Meters) ? "1.5 meters" : "5 feet"));
                h_1 = -1;
                return false;
            }

            return true;
        }

        static public bool ValidateH2(string H2, Units units, out double h_2)
        {
            if (String.IsNullOrEmpty(H2) ||
                !Double.TryParse(H2, out h_2) ||
                h_2 < Tools.ConvertMetersToSpecifiedUnits(1.5, units))
            {
                MessageBox.Show("Terminal 2 must be at least " + ((units == Units.Meters) ? "1.5 meters" : "5 feet"));
                h_2 = -1;
                return false;
            }

            return true;
        }

        static public bool ValidateFMHZ(string FMHZ, out double f__mhz)
        {
            if (String.IsNullOrEmpty(FMHZ) ||
                !Double.TryParse(FMHZ, out f__mhz) ||
                f__mhz < 100 ||
                f__mhz > 30000)
            {
                MessageBox.Show(Messages.FrequencyRangeError);
                f__mhz = -1;
                return false;
            }

            return true;
        }

        static public bool ValidateTIME(string TIME, out double time)
        {
            if (String.IsNullOrEmpty(TIME) ||
                !Double.TryParse(TIME, out time) ||
                time < 1 ||
                time > 99)
            {
                MessageBox.Show(Messages.TimeRangeError);
                time = -1;
                return false;
            }
            
            return true;
        }

        static public Brush GetBrush(int index)
        {
            int i = index % _listOfBrushes.Length;

            return _listOfBrushes[i];
        }
    }
}
