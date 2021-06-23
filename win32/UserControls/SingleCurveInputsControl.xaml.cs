using p528_gui.Interfaces;
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
    public partial class SingleCurveInputsControl : UserControl, IUnitEnabled, IInputValidation
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
        public bool AreInputsValid()
        {
            if (!Tools.ValidateH1(tb_h1.Text, _units, out double h1))
                return Tools.ValidationError(tb_h1);
            else
            {
                Tools.ValidationSuccess(tb_h1);
                H1 = h1;

                img_t1.Visibility = (Tools.ConvertSpecifiedUnitsToKm(H1, _units) <= Constants.TOP_OF_ATMOSPHERE__KM) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (!Tools.ValidateH2(tb_h2.Text, _units, out double h2))
                return Tools.ValidationError(tb_h2);
            else
            {
                Tools.ValidationSuccess(tb_h2);
                H2 = h2;

                img_t2.Visibility = (Tools.ConvertSpecifiedUnitsToKm(H2, _units) <= Constants.TOP_OF_ATMOSPHERE__KM) ? Visibility.Collapsed : Visibility.Visible;
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

            if (!Tools.ValidateFMHZ(tb_freq.Text, out double f__mhz))
                return Tools.ValidationError(tb_freq);
            else
            {
                Tools.ValidationSuccess(tb_freq);
                FMHZ = f__mhz;
            }

            if (!Tools.ValidateTIME(tb_time.Text, out double time))
                return Tools.ValidationError(tb_time);
            else
            {
                Tools.ValidationSuccess(tb_time);
                TIME = time;
            }

            return true;
        }
    }
}
