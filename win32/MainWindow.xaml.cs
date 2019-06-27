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

    public partial class MainWindow : Window
    {
        [DllImport("p528.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "Main")]
        private static extern int P528(double d__km, double h_1__meter, double h_2__meter, double f__mhz, double time_percentage, ref CResult result);

        public SeriesCollection PlotData { get; set; } = new SeriesCollection();

        private double _h1__meter;
        private double _h2__meter;
        private double _f__mhz;
        private double _time;

        private const double MAX_DISTANCE = 1800;   // km
        private const int PLOT_STEP_KM = 1;

        private const int LOS_SERIES = 0;
        private const int DFRAC_SERIES = 1;
        private const int SCAT_SERIES = 2;

        private const int TOP_OF_ATMOSPHERE__KM = 475;
        private readonly string TerminalHeightWarning = "Note: Although valid, the entered value is above the reference atmosphere which stops at 475 km above sea level";
        private readonly string ModelConsistencyWarning = "Caution: The P.528 model has returned a warning that the transition between diffraction and troposcatter might not be physically consistent.  Take caution when using these results.";

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            img_t1.ToolTip = TerminalHeightWarning;
            img_t2.ToolTip = TerminalHeightWarning;
            tb_Warning.Text = ModelConsistencyWarning;
            
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
        }

        private void Btn_Render_Click(object sender, RoutedEventArgs e)
        {
            if (!AreInputsValid())
                return;

            mi_Export.IsEnabled = true;

            var rtn = GetPoints(out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints);

            tb_Warning.Visibility = (rtn == 0) ? Visibility.Collapsed : Visibility.Visible;

            // Convert to Observable Points for Plotting Library currently in use
            var pts_LOS = losPoints.Select(x => new ObservablePoint(x.X, x.Y));
            var pts_DFRAC = dfracPoints.Select(x => new ObservablePoint(x.X, x.Y));
            var pts_SCAT = scatPoints.Select(x => new ObservablePoint(x.X, x.Y));

            // Plot the data
            PlotData[LOS_SERIES].Values.Clear();
            PlotData[LOS_SERIES].Values.AddRange(pts_LOS);
            PlotData[DFRAC_SERIES].Values.Clear();
            PlotData[DFRAC_SERIES].Values.AddRange(pts_DFRAC);
            PlotData[SCAT_SERIES].Values.Clear();
            PlotData[SCAT_SERIES].Values.AddRange(pts_SCAT);
        }

        /// <summary>
        /// Validate user specified inputs
        /// </summary>
        /// <returns>Did input validation succeed?</returns>
        private bool AreInputsValid()
        {
            if (String.IsNullOrEmpty(tb_h1.Text) ||
                !Double.TryParse(tb_h1.Text, out double h1__meter) ||
                h1__meter < 1.5)
            {
                ValidationError(tb_h1);
                MessageBox.Show("Terminal 1 must be at least 1.5 meters");
                return false;
            }
            else
            {
                ValidationSuccess(tb_h1);
                _h1__meter = h1__meter;

                img_t1.Visibility = (_h1__meter / 1000.0 <= TOP_OF_ATMOSPHERE__KM) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (String.IsNullOrEmpty(tb_h2.Text) ||
                !Double.TryParse(tb_h2.Text, out double h2__meter) ||
                h2__meter < 1.5)
            {
                ValidationError(tb_h2);
                MessageBox.Show("Terminal 2 must be at least 1.5 meters");
                return false;
            }
            else
            {
                ValidationSuccess(tb_h2);
                _h2__meter = h2__meter;

                img_t2.Visibility = (_h2__meter / 1000.0 <= TOP_OF_ATMOSPHERE__KM) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (h1__meter > h2__meter)
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
                f__mhz < 125 ||
                f__mhz > 15500)
            {
                ValidationError(tb_freq);
                MessageBox.Show("The frequency must be between 125 MHz and 15 500 MHz, inclusive");
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

        private int GetPoints(out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints)
        {
            losPoints = new List<Point>();
            dfracPoints = new List<Point>();
            scatPoints = new List<Point>();

            var result = new CResult();
            bool dfracSwitch = false;
            bool scatSwitch = false;
            int rtn = 0;
            for (int d__km = 0; d__km <= MAX_DISTANCE; d__km += PLOT_STEP_KM)
            {
                var r = P528(d__km, _h1__meter, _h2__meter, _f__mhz, _time, ref result);
                if (r != 0)
                    rtn = r;

                switch (result.propagation_mode)
                {
                    case 1: // Line-of-Sight
                        losPoints.Add(new Point(result.d__km, result.A__db));
                        break;
                    case 2: // Diffraction
                        if (!dfracSwitch)
                        {
                            losPoints.Add(new Point(result.d__km, result.A__db));   // Adding to ensure there is no gap in the curve
                            dfracSwitch = true;
                        }
                        dfracPoints.Add(new Point(result.d__km, result.A__db));
                        break;
                    case 3: // Troposcatter
                        if (!scatSwitch)
                        {
                            dfracPoints.Add(new Point(result.d__km, result.A__db)); // Adding to ensure there is no gap in the curve
                            scatSwitch = true;
                        }
                        scatPoints.Add(new Point(result.d__km, result.A__db));
                        break;
                }
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

            using (var fs = new StreamWriter(sfd.FileName))
            {
                fs.WriteLine("!!!DATA GENERATED FROM PRE-APPROVED VERSION OF REC P528!!!");
                fs.WriteLine();
                fs.WriteLine("Data Generated by the ITS ITU-R Rec P.528 GUI");
                fs.WriteLine($"Generated on {DateTime.Now.ToShortDateString()}");
                fs.WriteLine($"App Version,{version.Major}.{version.Minor}.{version.Build}");
                fs.WriteLine($"P.528 DLL Version,{dll.FileMajorPart}.{dll.FileMinorPart}.{dll.FileBuildPart}");
                fs.WriteLine();
                fs.WriteLine($"h_1__meter,{_h1__meter}");
                fs.WriteLine($"h_2__meter,{_h2__meter}");
                fs.WriteLine($"f__mhz,{_f__mhz}");
                fs.WriteLine($"time%,{_time * 100}");
                fs.WriteLine();

                // Regenerate data at 1 km steps for export
                var A__db = new List<double>();
                var d__km = new List<double>();
                var result = new CResult();
                for (int i = 0; i <= MAX_DISTANCE; i++)
                {
                    P528(i, _h1__meter, _h2__meter, _f__mhz, _time, ref result);

                    d__km.Add(Math.Round(result.d__km, 3));
                    A__db.Add(Math.Round(result.A__db, 3));
                }

                fs.WriteLine($"d__km,{String.Join(",", d__km)}");
                fs.WriteLine($"A__db,{String.Join(",", A__db)}");
            }

            MessageBox.Show("Export Completed");
        }

        private void Mi_About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }
    }
}
