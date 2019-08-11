using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace p528_gui.UserControls
{
    public partial class SingleCurveInputsControl : UserControl
    {
        private Units _units;
        public Units Units
        {
            get { return _units; }
            set
            {
                _units = value;
                tb_t1.Text = "Terminal 1 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
                tb_t2.Text = "Terminal 2 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
            }
        }

        public double H1 { get; private set; }

        public double H2 { get; private set; }

        public double FMHZ { get; set; }

        public double TIME { get; set; }

        public SingleCurveInputsControl()
        {
            InitializeComponent();

            img_t1.ToolTip = Messages.TerminalHeightWarning;
            img_t2.ToolTip = Messages.TerminalHeightWarning;
        }

        /// <summary>
        /// Validate user specified inputs
        /// </summary>
        /// <returns>Did input validation succeed?</returns>
        internal bool AreInputsValid()
        {
            if (String.IsNullOrEmpty(tb_h1.Text) ||
                !Double.TryParse(tb_h1.Text, out double h1) ||
                h1 < ConvertMetersToSpecifiedUnits(1.5))
            {
                Tools.ValidationError(tb_h1);
                MessageBox.Show("Terminal 1 must be at least " + ((_units == Units.Meters) ? "1.5 meters" : "5 feet"));
                return false;
            }
            else
            {
                Tools.ValidationSuccess(tb_h1);
                H1 = h1;

                img_t1.Visibility = (ConvertSpecifiedUnitsToKm(H1) <= Constants.TOP_OF_ATMOSPHERE__KM) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (String.IsNullOrEmpty(tb_h2.Text) ||
                !Double.TryParse(tb_h2.Text, out double h2) ||
                h2 < ConvertMetersToSpecifiedUnits(1.5))
            {
                Tools.ValidationError(tb_h2);
                MessageBox.Show("Terminal 2 must be at least " + ((_units == Units.Meters) ? "1.5 meters" : "5 feet"));
                return false;
            }
            else
            {
                Tools.ValidationSuccess(tb_h2);
                H2 = h2;

                img_t2.Visibility = (ConvertSpecifiedUnitsToKm(H2) <= Constants.TOP_OF_ATMOSPHERE__KM) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (h1 > h2)
            {
                Tools.ValidationError(tb_h1);
                Tools.ValidationError(tb_h2);
                MessageBox.Show(Messages.Terminal1LessThan2Error);
            }
            else
            {
                Tools.ValidationSuccess(tb_h1);
                Tools.ValidationSuccess(tb_h2);
            }

            if (String.IsNullOrEmpty(tb_freq.Text) ||
                !Double.TryParse(tb_freq.Text, out double f__mhz) ||
                f__mhz < 100 ||
                f__mhz > 15500)
            {
                Tools.ValidationError(tb_freq);
                MessageBox.Show(Messages.FrequencyRangeError);
                return false;
            }
            else
            {
                Tools.ValidationSuccess(tb_freq);
                FMHZ = f__mhz;
            }

            if (String.IsNullOrEmpty(tb_time.Text) ||
                !Double.TryParse(tb_time.Text, out double time) ||
                time < 1 ||
                time > 99)
            {
                Tools.ValidationError(tb_time);
                MessageBox.Show(Messages.TimeRangeError);
                return false;
            }
            else
            {
                Tools.ValidationSuccess(tb_time);
                TIME = time / 100;
            }

            return true;
        }

        private double ConvertMetersToSpecifiedUnits(double meters)
        {
            return (_units == Units.Meters) ? meters : (meters / Constants.METER_PER_FOOT);
        }

        private double ConvertSpecifiedUnitsToKm(double value)
        {
            var value__meters = (_units == Units.Meters) ? value : (value * Constants.METER_PER_FOOT);

            return value__meters / 1000.0;
        }
    }
}
