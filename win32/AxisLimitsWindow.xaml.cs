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
using System.Windows.Shapes;

namespace p528_gui
{
    public partial class AxisLimitsWindow : Window
    {
        private double _xAxisMin;
        public double XAxisMinimum
        {
            get { return _xAxisMin; }
            set
            {
                _xAxisMin = value;
                tb_xAxisMinimum.Text = _xAxisMin.ToString();
            }
        }

        private double _xAxisMax;
        public double XAxisMaximum
        {
            get { return _xAxisMax; }
            set
            {
                _xAxisMax = value;
                tb_xAxisMaximum.Text = _xAxisMax.ToString();
            }
        }

        private double _xAxisStep;
        public double XAxisStep
        {
            get { return _xAxisStep; }
            set
            {
                _xAxisStep = value;
                tb_xAxisStep.Text = _xAxisStep.ToString();
            }
        }

        private double _yAxisMin;
        public double YAxisMinimum
        {
            get { return _yAxisMin; }
            set
            {
                _yAxisMin = value;
                tb_yAxisMinimum.Text = _yAxisMin.ToString();
            }
        }

        private double _yAxisMax;
        public double YAxisMaximum
        {
            get { return _yAxisMax; }
            set
            {
                _yAxisMax = value;
                tb_yAxisMaximum.Text = _yAxisMax.ToString();
            }
        }

        private double _yAxisStep;
        public double YAxisStep
        {
            get { return _yAxisStep; }
            set
            {
                _yAxisStep = value;
                tb_yAxisStep.Text = _yAxisStep.ToString();
            }
        }

        public string XAxisUnit
        {
            set
            {
                tb_xAxisMinimumUnits.Text = value;
                tb_xAxisMaximumUnits.Text = value;
                tb_xAxisStepUnits.Text = value;
            }
        }

        public AxisLimitsWindow()
        {
            InitializeComponent();
        }

        private void btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate())
                return;

            this.DialogResult = true;
            this.Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private bool Validate()
        {
            if (String.IsNullOrEmpty(tb_xAxisMinimum.Text) ||
                !Double.TryParse(tb_xAxisMinimum.Text, out double xMin) ||
                xMin < 0)
            {
                ValidationError(tb_xAxisMinimum);
                MessageBox.Show("Minimum value for X-Axis must be greater than zero");
                return false;
            }
            else
            {
                ValidationSuccess(tb_xAxisMinimum);
                _xAxisMin = xMin;
            }

            if (String.IsNullOrEmpty(tb_xAxisMaximum.Text) ||
                !Double.TryParse(tb_xAxisMaximum.Text, out double xMax) ||
                xMax <= _xAxisMin)
            {
                ValidationError(tb_xAxisMaximum);
                MessageBox.Show("Maximum value for X-Axis must be greater then minimum for X-Axis");
                return false;
            }
            else
            {
                ValidationSuccess(tb_xAxisMaximum);
                _xAxisMax = xMax;
            }

            if (String.IsNullOrEmpty(tb_xAxisStep.Text) ||
                !Double.TryParse(tb_xAxisStep.Text, out double xStep) ||
                xStep <= 0)
            {
                ValidationError(tb_xAxisStep);
                MessageBox.Show("Step value for X-Axis must be greater than zero");
                return false;
            }
            else
            {
                ValidationSuccess(tb_xAxisStep);
                _xAxisStep = xStep;
            }

            if (String.IsNullOrEmpty(tb_yAxisMinimum.Text) ||
                !Double.TryParse(tb_yAxisMinimum.Text, out double yMin))
            {
                ValidationError(tb_yAxisMinimum);
                MessageBox.Show("Unable to parse minimum value for Y-Axis");
                return false;
            }
            else
            {
                ValidationSuccess(tb_yAxisMinimum);
                _yAxisMin = yMin;
            }

            if (String.IsNullOrEmpty(tb_yAxisMaximum.Text) ||
                !Double.TryParse(tb_yAxisMaximum.Text, out double yMax) ||
                yMax <= _yAxisMin)
            {
                ValidationError(tb_yAxisMaximum);
                MessageBox.Show("Maximum value for Y-Axis must be greater then minimum for Y-Axis");
                return false;
            }
            else
            {
                ValidationSuccess(tb_yAxisMaximum);
                _yAxisMax = yMax;
            }

            if (String.IsNullOrEmpty(tb_yAxisStep.Text) ||
                !Double.TryParse(tb_yAxisStep.Text, out double yStep) ||
                yStep <= 0)
            {
                ValidationError(tb_yAxisStep);
                MessageBox.Show("Step value for Y-Axis must be greater than zero");
                return false;
            }
            else
            {
                ValidationSuccess(tb_yAxisStep);
                _yAxisStep = yStep;
            }

            return true;
        }

        private void ValidationError(TextBox tb)
        {
            tb.Background = Brushes.LightPink;
        }

        private void ValidationSuccess(TextBox tb)
        {
            tb.Background = Brushes.White;
        }
    }
}
