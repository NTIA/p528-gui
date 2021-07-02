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
        static public double ConvertToMeters(double value)
            => (GlobalState.Units == Units.Meters) ? value : (value * Constants.METER_PER_FOOT);

        static public double ConvertFromMeters(double value)
            => (GlobalState.Units == Units.Meters) ? value : (value / Constants.METER_PER_FOOT);

        static public double ConvertToKm(double value)
            => (GlobalState.Units == Units.Meters) ? value : (value * Constants.KM_PER_NAUTICAL_MILE);

        static public double ConvertFromKm(double value)
            => (GlobalState.Units == Units.Meters) ? value : (value / Constants.KM_PER_NAUTICAL_MILE);

        static public double ConvertMetersToSpecifiedUnits(double meters, Units units)
        {
            return (units == Units.Meters) ? meters : (meters / Constants.METER_PER_FOOT);
        }

        static public double ConvertSpecifiedUnitsToKm(double value, Units units)
        {
            var value__meters = (units == Units.Meters) ? value : (value * Constants.METER_PER_FOOT);

            return value__meters / 1000.0;
        }

        static public Brush GetBrush(int index)
        {
            int i = index % LineColors.Count;

            return LineColors.ElementAt(i).Key;
        }

        /// <summary>
        /// List of support line colors
        /// </summary>
        public static Dictionary<SolidColorBrush, string> LineColors = new Dictionary<SolidColorBrush, string>()
        {
            { Brushes.Blue, "Blue" },
            { Brushes.Red, "Red" },
            { Brushes.Black, "Black" },
            { Brushes.Green, "Green" },
            { Brushes.Purple, "Purple" },
            { Brushes.Orange, "Orange" },
            { Brushes.Brown, "Brown" },
            { Brushes.SkyBlue, "Sky Blue" },
            { Brushes.Pink, "Pink" }
        };
    }
}
