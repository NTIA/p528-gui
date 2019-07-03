using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

namespace p528_gui
{
    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct CResult
    {
        public int propagation_mode;
        public int los_iterations;

        public double d__km;
        public double A__db;
        public double A_fs__db;
    }

    public enum Units
    {
        Meters,
        Feet
    }

    public partial class MainWindow : Window
    {
        [DllImport("p528.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "Main")]
        private static extern int P528(double d__km, double h_1__meter, double h_2__meter, double f__mhz, double time_percentage, ref CResult result);

        private const int ERROR_HEIGHT_AND_DISTANCE = 10;
        private const int WARNING__DFRAC_TROPO_REGION = 0xFF1;
        private const int WARNING__LOW_FREQUENCY = 0xFF2;

        public SeriesCollection PlotData { get; set; } = new SeriesCollection();

        private double _h1;         // meters or feet
        private double _h2;         // meters or feet
        private double _f__mhz;     // MHz
        private double _time;       // %

        private const double MAX_DISTANCE = 1800;   // km or n miles
        private const int PLOT_STEP = 1;            // km or n miles

        private const int LOS_SERIES = 0;
        private const int DFRAC_SERIES = 1;
        private const int SCAT_SERIES = 2;
        private const int FS_SERIES = 3;

        // unit conversion factors
        private const double METER_PER_FOOT = 0.3048;
        private const double KM_PER_NAUTICAL_MILE = 1.852;

        private const int TOP_OF_ATMOSPHERE__KM = 475;

        // warning text
        private readonly string TerminalHeightWarning = "Note: Although valid, the entered value is above the reference atmosphere which stops at 475 km above sea level";
        private readonly string ModelConsistencyWarning = "Caution: The P.528 model has returned a warning that the transition between diffraction and troposcatter might not be physically consistent.  Take caution when using these results.";
        private readonly string LowFrequencyWarning = "Caution: The entered frequency is less than the lower limit specified in P.528.  Take caution when using these results.";

        private IEnumerable<ObservablePoint> _pts_FS;
        private Units _units = Units.Meters;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            img_t1.ToolTip = TerminalHeightWarning;
            img_t2.ToolTip = TerminalHeightWarning;
            tb_ConsistencyWarning.Text = ModelConsistencyWarning;
            tb_FrequencyWarning.Text = LowFrequencyWarning;
            
            PlotData.Add(new LineSeries
            {
                Title = "Line of Sight",
                PointGeometry = null,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#cc79a7"),
                StrokeThickness = 5,
                Values = new ChartValues<ObservablePoint>(),
                Fill = new SolidColorBrush() { Opacity = 0 }
            });

            PlotData.Add(new LineSeries
            {
                Title = "Diffraction",
                PointGeometry = null,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#0072b2"),
                StrokeThickness = 5,
                Values = new ChartValues<ObservablePoint>(),
                Fill = new SolidColorBrush() { Opacity = 0 }
            });

            PlotData.Add(new LineSeries
            {
                Title = "Troposcatter",
                PointGeometry = null,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#e69f00"),
                StrokeThickness = 5,
                Values = new ChartValues<ObservablePoint>(),
                Fill = new SolidColorBrush() { Opacity = 0 }
            });

            PlotData.Add(new LineSeries
            {
                Title = "Free Space",
                PointGeometry = null,
                StrokeDashArray = new DoubleCollection(new double[] { 4 }),
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#000000"),
                StrokeThickness = 1,
                Values = new ChartValues<ObservablePoint>(),
                Fill = new SolidColorBrush() { Opacity = 0 }
            });

            SetUnits();
        }

        private void Btn_Render_Click(object sender, RoutedEventArgs e)
        {
            if (!AreInputsValid())
                return;

            mi_Export.IsEnabled = true;

            var rtn = GetPoints(out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints, out List<Point> fsPoints, true);

            // Set any warning messages
            tb_ConsistencyWarning.Visibility = ((rtn & WARNING__DFRAC_TROPO_REGION) == WARNING__DFRAC_TROPO_REGION) ? Visibility.Visible : Visibility.Collapsed;
            tb_FrequencyWarning.Visibility = ((rtn & WARNING__LOW_FREQUENCY) == WARNING__LOW_FREQUENCY) ? Visibility.Visible : Visibility.Collapsed;

            // Convert to Observable Points for Plotting Library currently in use
            var pts_LOS = losPoints.Select(x => new ObservablePoint(x.X, x.Y));
            var pts_DFRAC = dfracPoints.Select(x => new ObservablePoint(x.X, x.Y));
            var pts_SCAT = scatPoints.Select(x => new ObservablePoint(x.X, x.Y));
            _pts_FS = fsPoints.Select(x => new ObservablePoint(x.X, x.Y));

            // Plot the data
            PlotData[LOS_SERIES].Values.Clear();
            PlotData[LOS_SERIES].Values.AddRange(pts_LOS);
            PlotData[DFRAC_SERIES].Values.Clear();
            PlotData[DFRAC_SERIES].Values.AddRange(pts_DFRAC);
            PlotData[SCAT_SERIES].Values.Clear();
            PlotData[SCAT_SERIES].Values.AddRange(pts_SCAT);
            if (mi_FreeSpace.IsChecked)
            {
                PlotData[FS_SERIES].Values.Clear();
                PlotData[FS_SERIES].Values.AddRange(_pts_FS);
            }
        }

        private double ConvertMetersToSpecifiedUnits(double meters)
        {
            return (_units == Units.Meters) ? meters : (meters / METER_PER_FOOT);
        }

        private double ConvertSpecifiedUnitsToKm(double value)
        {
            var value__meters = (_units == Units.Meters) ? value : (value * METER_PER_FOOT);

            return value__meters / 1000.0;
        }

        /// <summary>
        /// Validate user specified inputs
        /// </summary>
        /// <returns>Did input validation succeed?</returns>
        private bool AreInputsValid()
        {
            if (String.IsNullOrEmpty(tb_h1.Text) ||
                !Double.TryParse(tb_h1.Text, out double h1) ||
                h1 < ConvertMetersToSpecifiedUnits(1.5))
            {
                ValidationError(tb_h1);
                MessageBox.Show("Terminal 1 must be at least " + ((_units == Units.Meters) ? "1.5 meters" : "5 feet"));
                return false;
            }
            else
            {
                ValidationSuccess(tb_h1);
                _h1 = h1;

                img_t1.Visibility = (ConvertSpecifiedUnitsToKm(_h1) <= TOP_OF_ATMOSPHERE__KM) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (String.IsNullOrEmpty(tb_h2.Text) ||
                !Double.TryParse(tb_h2.Text, out double h2) ||
                h2 < ConvertMetersToSpecifiedUnits(1.5))
            {
                ValidationError(tb_h2);
                MessageBox.Show("Terminal 2 must be at least " + ((_units == Units.Meters) ? "1.5 meters" : "5 feet"));
                return false;
            }
            else
            {
                ValidationSuccess(tb_h2);
                _h2 = h2;

                img_t2.Visibility = (ConvertSpecifiedUnitsToKm(_h2) <= TOP_OF_ATMOSPHERE__KM) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (h1 > h2)
            {
                ValidationError(tb_h1);
                ValidationError(tb_h2);
                MessageBox.Show("Terminal 1 must be less than the height of Terminal 2");
            }
            else
            {
                ValidationSuccess(tb_h1);
                ValidationSuccess(tb_h2);
            }

            if (String.IsNullOrEmpty(tb_freq.Text) ||
                !Double.TryParse(tb_freq.Text, out double f__mhz) ||
                f__mhz < 100 ||
                f__mhz > 15500)
            {
                ValidationError(tb_freq);
                MessageBox.Show("The frequency must be between 100 MHz and 15 500 MHz, inclusive");
                return false;
            }
            else
            {
                ValidationSuccess(tb_freq);
                _f__mhz = f__mhz;
            }

            if (String.IsNullOrEmpty(tb_time.Text) ||
                !Double.TryParse(tb_time.Text, out double time) ||
                time < 1 ||
                time > 99)
            {
                ValidationError(tb_time);
                MessageBox.Show("The time percentage must be between 1 and 99, inclusive.");
                return false;
            }
            else
            {
                ValidationSuccess(tb_time);
                _time = time / 100;
            }

            return true;
        }

        private int GetPoints(out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints, out List<Point> fsPoints, bool blendLines)
        {
            losPoints = new List<Point>();
            dfracPoints = new List<Point>();
            scatPoints = new List<Point>();
            fsPoints = new List<Point>();

            bool dfracSwitch = false;
            bool scatSwitch = false;

            var result = new CResult();
            int rtn = 0;
            double d__km, d_out;

            // convert inputs into metric units
            var h1__meter = (_units == Units.Meters) ? _h1 : (_h1 * METER_PER_FOOT);
            var h2__meter = (_units == Units.Meters) ? _h2 : (_h2 * METER_PER_FOOT);

            // iterate on user-specified units (km or n miles)
            for (int d = 0; d <= MAX_DISTANCE; d += PLOT_STEP)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * KM_PER_NAUTICAL_MILE);

                var r = P528(d__km, h1__meter, h2__meter, _f__mhz, _time, ref result);

                // convert output distance from P.528 back into user-specified units
                d_out = (_units == Units.Meters) ? result.d__km : (result.d__km / KM_PER_NAUTICAL_MILE);

                // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                if (r != ERROR_HEIGHT_AND_DISTANCE && r != 0)
                    rtn = r;

                switch (result.propagation_mode)
                {
                    case 1: // Line-of-Sight
                        losPoints.Add(new Point(d_out, result.A__db));
                        break;
                    case 2: // Diffraction
                        if (blendLines && !dfracSwitch)
                        {
                            losPoints.Add(new Point(d_out, result.A__db));   // Adding to ensure there is no gap in the curve
                            dfracSwitch = true;
                        }
                        dfracPoints.Add(new Point(d_out, result.A__db));
                        break;
                    case 3: // Troposcatter
                        if (blendLines && !scatSwitch)
                        {
                            dfracPoints.Add(new Point(d_out, result.A__db)); // Adding to ensure there is no gap in the curve
                            scatSwitch = true;
                        }
                        scatPoints.Add(new Point(d_out, result.A__db));
                        break;
                }

                fsPoints.Add(new Point(d_out, result.A_fs__db));
            }

            return rtn;
        }

        private void ValidationError(TextBox tb)
        {
            tb.Background = Brushes.LightPink;
        }

        private void ValidationSuccess(TextBox tb)
        {
            tb.Background = Brushes.White;
        }

        private void Mi_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Mi_Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV file (*.csv)|*.csv";

            if (sfd.ShowDialog() != true)
                return;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<double>();
            var A_fs__db = new List<double>();
            var dists = new List<double>();
            int warnings = 0;
            double d__km, d_out;

            var h1__meter = (_units == Units.Meters) ? _h1 : (_h1 * METER_PER_FOOT);
            var h2__meter = (_units == Units.Meters) ? _h2 : (_h2 * METER_PER_FOOT);

            var result = new CResult();
            for (int d = 0; d <= MAX_DISTANCE; d++)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * KM_PER_NAUTICAL_MILE);

                var r = P528(d__km, h1__meter, h2__meter, _f__mhz, _time, ref result);

                // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                if (r != ERROR_HEIGHT_AND_DISTANCE && r != 0)
                    warnings = r;

                // convert output distance from P.528 back into user-specified units
                d_out = (_units == Units.Meters) ? result.d__km : (result.d__km / KM_PER_NAUTICAL_MILE);

                dists.Add(Math.Round(d_out, 3));
                A__db.Add(Math.Round(result.A__db, 3));
                A_fs__db.Add(Math.Round(result.A_fs__db, 3));
            }

            using (var fs = new StreamWriter(sfd.FileName))
            {
                fs.WriteLine("Data Generated by the ITS ITU-R Rec P.528 GUI");
                fs.WriteLine($"Generated on {DateTime.Now.ToShortDateString()}");
                fs.WriteLine($"App Version,{version.Major}.{version.Minor}.{version.Build}");
                fs.WriteLine($"P.528 DLL Version,{dll.FileMajorPart}.{dll.FileMinorPart}.{dll.FileBuildPart}");

                // Check and print any warnings
                if (warnings != 0)
                {
                    fs.WriteLine();

                    if ((warnings & WARNING__DFRAC_TROPO_REGION) == WARNING__DFRAC_TROPO_REGION)
                        fs.WriteLine(ModelConsistencyWarning);
                    if ((warnings & WARNING__LOW_FREQUENCY) == WARNING__LOW_FREQUENCY)
                        fs.WriteLine(LowFrequencyWarning);
                }

                fs.WriteLine();
                fs.WriteLine($"h_1,{_h1}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"h_2,{_h2}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{_f__mhz}");
                fs.WriteLine($"time%,{_time * 100}");
                fs.WriteLine();

                fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                fs.WriteLine($"{String.Join(",", dists)}");
                fs.WriteLine($"A__db,{String.Join(",", A__db)}");
                fs.WriteLine($"A_fs__dB,{String.Join(",", A_fs__db)}");
            }

            MessageBox.Show("Export Completed");
        }

        private void Mi_About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void Mi_FreeSpace_Click(object sender, RoutedEventArgs e)
        {
            if (!mi_FreeSpace.IsChecked)
                PlotData.RemoveAt(FS_SERIES);
            else
                AddFreeSpaceLineToPlot();
        }

        private void AddFreeSpaceLineToPlot()
        {
            PlotData.Add(new LineSeries
            {
                Title = "Free Space",
                PointGeometry = null,
                StrokeDashArray = new DoubleCollection(new double[] { 4 }),
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#000000"),
                StrokeThickness = 1,
                Values = new ChartValues<ObservablePoint>(),
                Fill = new SolidColorBrush() { Opacity = 0 }
            });

            if (_pts_FS != null)
                PlotData[FS_SERIES].Values.AddRange(_pts_FS);
        }

        private void Mi_Units_Meters_Click(object sender, RoutedEventArgs e)
        {
            mi_Units_Meters.IsChecked = true;
            mi_Units_Feet.IsChecked = false;
            _units = Units.Meters;

            SetUnits();
        }

        private void Mi_Units_Feet_Click(object sender, RoutedEventArgs e)
        {
            mi_Units_Feet.IsChecked = true;
            mi_Units_Meters.IsChecked = false;
            _units = Units.Feet;

            SetUnits();
        }

        private void SetUnits()
        {
            // Update text
            tb_t1.Text = "Terminal 1 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
            tb_t2.Text = "Terminal 2 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
            xAxis.Title = "Distance " + ((_units == Units.Meters) ? "(km)" : "(n mile)");
            xAxis.MaxValue = (_units == Units.Meters) ? 1800 : 970;
            xSeparator.Step = (_units == Units.Meters) ? 200 : 100;
            customToolTip.Units = _units;

            // Clear plot data
            PlotData[LOS_SERIES].Values.Clear();
            PlotData[DFRAC_SERIES].Values.Clear();
            PlotData[SCAT_SERIES].Values.Clear();
            PlotData[FS_SERIES].Values.Clear();
        }
    }
}
