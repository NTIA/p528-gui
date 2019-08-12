using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Win32;
using p528_gui.Interfaces;
using p528_gui.UserControls;
using p528_gui.Windows;
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
    public enum Units
    {
        Meters,
        Feet
    }

    public partial class MainWindow : Window
    {
        [DllImport("p528.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "Main")]
        internal static extern int P528(double d__km, double h_1__meter, double h_2__meter, double f__mhz, double time_percentage, ref CResult result);

        [DllImport("p528.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "MainEx")]
        internal static extern int P528EX(double d__km, double h_1__meter, double h_2__meter, double f__mhz, double time_percentage, ref CResult result,
            ref Terminal terminal_1, ref Terminal terminal_2, ref TroposcatterParams tropo, ref Path path, ref LineOfSightParams los_params);

        private const int ERROR_HEIGHT_AND_DISTANCE = 10;
        private const int WARNING__DFRAC_TROPO_REGION = 0xFF1;
        private const int WARNING__LOW_FREQUENCY = 0xFF2;

        public SeriesCollection PlotData { get; set; } = new SeriesCollection();

        private const int LOS_SERIES = 0;
        private const int DFRAC_SERIES = 1;
        private const int SCAT_SERIES = 2;
        private const int FS_SERIES = 3;

        private IEnumerable<ObservablePoint> _pts_FS;
        private Units _units = Units.Meters;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            tb_ConsistencyWarning.Text = Messages.ModelConsistencyWarning;
            tb_FrequencyWarning.Text = Messages.LowFrequencyWarning;

            ConfigurePlotForSingleCurve();

            SetUnits();
        }

        private void ConfigurePlotForSingleCurve()
        {
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
        }

        private void ConfigurePlotForMultipleHeightCurves()
        {
            PlotData.Clear();
        }

        private void ConfigurePlotForMultipleTimeCurves()
        {
            PlotData.Clear();
        }

        private void Btn_Render_Click(object sender, RoutedEventArgs e)
        {
            if (mi_PlotMode_SingleCurve.IsChecked)
                RenderSingleCurve();
            else if (mi_PlotMode_MultipleHeights.IsChecked)
                RenderMultipleHeights();
            else if (mi_PlotMode_MultipleTimePercentages.IsChecked)
                RenderMultipleTimes();
        }

        private void RenderSingleCurve()
        {
            var inputConrol = grid_Controls.Children[0] as SingleCurveInputsControl;
            if (!inputConrol.AreInputsValid())
                return;

            mi_Export.IsEnabled = true;

            var rtn = GetPointsEx(out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints, out List<Point> fsPoints, true);

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

        private void RenderMultipleHeights()
        {
            var inputControl = grid_Controls.Children[0] as MultipleHeightsInputsControl;
            if (!inputControl.AreInputsValid())
                return;

            PlotData.Clear();

            // convert inputs into metric units
            double h_1__meter = (_units == Units.Meters) ? inputControl.H1 : (inputControl.H1 * Constants.METER_PER_FOOT);
            List<double> h_2s__meter = new List<double>();
            for (int i = 0; i < inputControl.H2s.Count; i++)
            {
                double h2 = inputControl.H2s[i];
                double h_2__meter = (_units == Units.Meters) ? h2 : (h2 * Constants.METER_PER_FOOT);

                int rtn = GetPoints(h_1__meter, h_2__meter, inputControl.FMHZ, inputControl.TIME, out List<Point> pts);

                // Convert to Observable Points for Plotting Library currently in use
                var obpts = pts.Select(x => new ObservablePoint(x.X, x.Y));

                PlotData.Add(new LineSeries
                {
                    Title = $"{h2} {_units.ToString()}",
                    PointGeometry = null,
                    Stroke = Tools.GetBrush(i),
                    StrokeThickness = 5,
                    Values = new ChartValues<ObservablePoint>(obpts),
                    Fill = new SolidColorBrush() { Opacity = 0 },
                });
            }
        }

        private void RenderMultipleTimes()
        {
            var inputControl = grid_Controls.Children[0] as MultipleTimeInputsControl;
            if (!inputControl.AreInputsValid())
                return;

            PlotData.Clear();

            // convert inputs into metric units
            double h_1__meter = (_units == Units.Meters) ? inputControl.H1 : (inputControl.H1 * Constants.METER_PER_FOOT);
            double h_2__meter = (_units == Units.Meters) ? inputControl.H2 : (inputControl.H2 * Constants.METER_PER_FOOT);
            List<double> times = new List<double>();
            for (int i = 0; i < inputControl.TIMEs.Count; i++)
            {
                double time = inputControl.TIMEs[i];

                int rtn = GetPoints(h_1__meter, h_2__meter, inputControl.FMHZ, time, out List<Point> pts);

                // Convert to Observable Points for Plotting Library currently in use
                var obpts = pts.Select(x => new ObservablePoint(x.X, x.Y));

                PlotData.Add(new LineSeries
                {
                    Title = $"{time * 100}%",
                    PointGeometry = null,
                    Stroke = Tools.GetBrush(i),
                    StrokeThickness = 5,
                    Values = new ChartValues<ObservablePoint>(obpts),
                    Fill = new SolidColorBrush() { Opacity = 0 },
                });
            }
        }

        private int GetPoints(double h_1__meter, double h_2__meter, double f__mhz, double time, out List<Point> lossPoints)
        {
            lossPoints = new List<Point>();

            var result = new CResult();
            int rtn = 0;

            // iterate on user-specified units (km or n miles)
            double d_step = (xAxis.MaxValue - xAxis.MinValue) / 1500;
            double d = xAxis.MinValue;
            double d__km, d_out;
            while (d <= xAxis.MaxValue)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                var r = P528(d__km, h_1__meter, h_2__meter, f__mhz, time, ref result);

                // convert output distance from P.528 back into user-specified units
                d_out = (_units == Units.Meters) ? result.d__km : (result.d__km / Constants.KM_PER_NAUTICAL_MILE);

                // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                if (r != ERROR_HEIGHT_AND_DISTANCE && r != 0)
                    rtn = r;

                lossPoints.Add(new Point(d_out, result.A__db));

                d += d_step;
            }

            return rtn;
        }

        private int GetPointsEx(out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints, out List<Point> fsPoints, bool blendLines)
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
            var h1__meter = (_units == Units.Meters) ? singleCurveInputsCtrl.H1 : (singleCurveInputsCtrl.H1 * Constants.METER_PER_FOOT);
            var h2__meter = (_units == Units.Meters) ? singleCurveInputsCtrl.H2 : (singleCurveInputsCtrl.H2 * Constants.METER_PER_FOOT);

            // iterate on user-specified units (km or n miles)
            double d_step = (xAxis.MaxValue - xAxis.MinValue) / 1500;
            double d = xAxis.MinValue;
            while (d <= xAxis.MaxValue)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                var r = P528(d__km, h1__meter, h2__meter, singleCurveInputsCtrl.FMHZ, singleCurveInputsCtrl.TIME, ref result);

                // convert output distance from P.528 back into user-specified units
                d_out = (_units == Units.Meters) ? result.d__km : (result.d__km / Constants.KM_PER_NAUTICAL_MILE);

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

                d += d_step;
            }

            return rtn;
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

            var exportOptionsWndw = new ExportOptionsWindow();
            if (!exportOptionsWndw.ShowDialog().Value)
                return;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<double>();
            var A_fs__db = new List<double>();
            var dists = new List<double>();
            var modes = new List<int>();
            int warnings = 0;
            double d__km, d_out;

            var h1__meter = (_units == Units.Meters) ? singleCurveInputsCtrl.H1 : (singleCurveInputsCtrl.H1 * Constants.METER_PER_FOOT);
            var h2__meter = (_units == Units.Meters) ? singleCurveInputsCtrl.H1 : (singleCurveInputsCtrl.H1 * Constants.METER_PER_FOOT);

            var result = new CResult();
            double d = xAxis.MinValue;
            while (d <= xAxis.MaxValue)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                var r = P528(d__km, h1__meter, h2__meter, singleCurveInputsCtrl.FMHZ, singleCurveInputsCtrl.TIME, ref result);

                // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                if (r != ERROR_HEIGHT_AND_DISTANCE && r != 0)
                    warnings = r;

                // convert output distance from P.528 back into user-specified units
                d_out = (_units == Units.Meters) ? result.d__km : (result.d__km / Constants.KM_PER_NAUTICAL_MILE);

                dists.Add(Math.Round(d_out, 3));
                A__db.Add(Math.Round(result.A__db, 3));
                A_fs__db.Add(Math.Round(result.A_fs__db, 3));
                modes.Add(result.propagation_mode);

                d++;
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
                        fs.WriteLine(Messages.ModelConsistencyWarning);
                    if ((warnings & WARNING__LOW_FREQUENCY) == WARNING__LOW_FREQUENCY)
                        fs.WriteLine(Messages.LowFrequencyWarning);
                }

                fs.WriteLine();
                fs.WriteLine($"h_1,{singleCurveInputsCtrl.H1}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"h_2,{singleCurveInputsCtrl.H2}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{singleCurveInputsCtrl.FMHZ}");
                fs.WriteLine($"time%,{singleCurveInputsCtrl.TIME * 100}");
                fs.WriteLine();

                if (exportOptionsWndw.IncludeModeOfPropagation)
                    fs.WriteLine("Mode of Propagation: 1 = Line-of-Sight; 2 = Diffraction; 3 = Troposcatter\n");

                if (exportOptionsWndw.IsRowAlignedData)
                {
                    fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                    fs.WriteLine($"{String.Join(",", dists)}");
                    fs.WriteLine($"A__db,{String.Join(",", A__db)}");
                    if (exportOptionsWndw.IncludeFreeSpaceLoss)
                        fs.WriteLine($"A_fs__db,{String.Join(",", A_fs__db)}");
                    if (exportOptionsWndw.IncludeModeOfPropagation)
                        fs.WriteLine($"Mode,{String.Join(",", modes)}");
                }
                else
                {
                    fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                    fs.Write("A__db");
                    if (exportOptionsWndw.IncludeFreeSpaceLoss)
                        fs.Write(",A_fs__db");
                    if (exportOptionsWndw.IncludeModeOfPropagation)
                        fs.Write(",PropMode");
                    fs.WriteLine();

                    for (int i = 0; i < A__db.Count; i++)
                    {
                        fs.Write($"{dists[i]},{A__db[i]}");
                        if (exportOptionsWndw.IncludeFreeSpaceLoss)
                            fs.Write($",{A_fs__db[i]}");
                        if (exportOptionsWndw.IncludeModeOfPropagation)
                            fs.Write($",{modes[i]}");
                        fs.WriteLine();
                    }
                }
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
            (grid_Controls.Children[0] as IUnitEnabled).Units = _units;
            xAxis.Title = "Distance " + ((_units == Units.Meters) ? "(km)" : "(n mile)");
            ResetPlot();
            customToolTip.Units = _units;

            // Clear plot data
            if (mi_PlotMode_SingleCurve.IsChecked)
            {
                PlotData[LOS_SERIES].Values.Clear();
                PlotData[DFRAC_SERIES].Values.Clear();
                PlotData[SCAT_SERIES].Values.Clear();
                PlotData[FS_SERIES].Values.Clear();
            }
            else
                PlotData.Clear();
        }

        private void Mi_SetAxisLimits_Click(object sender, RoutedEventArgs e)
        {
            var limitsWndw = new AxisLimitsWindow()
            {
                XAxisUnit = (_units == Units.Meters) ? "km" : "n mile",
                XAxisMaximum = xAxis.MaxValue,
                XAxisMinimum = xAxis.MinValue,
                XAxisStep = xSeparator.Step,
                YAxisMaximum = yAxis.MaxValue,
                YAxisMinimum = yAxis.MinValue,
                YAxisStep = ySeparator.Step
            };

            if (!limitsWndw.ShowDialog().Value)
                return;

            xAxis.MaxValue = limitsWndw.XAxisMaximum;
            xAxis.MinValue = limitsWndw.XAxisMinimum;
            xSeparator.Step = limitsWndw.XAxisStep;

            yAxis.MaxValue = limitsWndw.YAxisMaximum;
            yAxis.MinValue = limitsWndw.YAxisMinimum;
            ySeparator.Step = limitsWndw.YAxisStep;
        }

        private void Mi_ResetAxisLimits_Click(object sender, RoutedEventArgs e)
        {
            ResetPlot();

            Btn_Render_Click(null, null);
        }

        private void ResetPlot()
        {
            if (_units == Units.Meters)
                xAxis.MaxValue = 1800;
            else
                xAxis.MaxValue = 970;

            xAxis.MinValue = 0;
            xSeparator.Step = 200;
            yAxis.MaxValue = -100;
            yAxis.MinValue = -300;
            ySeparator.Step = 20;
        }

        private void Mi_PlotMode_SingleCurve_Click(object sender, RoutedEventArgs e)
        {
            mi_PlotMode_SingleCurve.IsChecked = true;
            mi_PlotMode_MultipleHeights.IsChecked = false;
            mi_PlotMode_MultipleTimePercentages.IsChecked = false;

            grid_Controls.Children.Clear();
            grid_Controls.Children.Add(new SingleCurveInputsControl() { Units = _units });
            ConfigurePlotForSingleCurve();
            mi_SingleCurve_View.Visibility = Visibility.Visible;
        }

        private void Mi_PlotMode_MultipleHeights_Click(object sender, RoutedEventArgs e)
        {
            mi_PlotMode_SingleCurve.IsChecked = false;
            mi_PlotMode_MultipleHeights.IsChecked = true;
            mi_PlotMode_MultipleTimePercentages.IsChecked = false;

            grid_Controls.Children.Clear();
            grid_Controls.Children.Add(new MultipleHeightsInputsControl() { Units = _units });
            ConfigurePlotForMultipleHeightCurves();
            mi_SingleCurve_View.Visibility = Visibility.Collapsed;
        }

        private void Mi_PlotMode_MultipleTimePercentages_Click(object sender, RoutedEventArgs e)
        {
            mi_PlotMode_SingleCurve.IsChecked = false;
            mi_PlotMode_MultipleHeights.IsChecked = false;
            mi_PlotMode_MultipleTimePercentages.IsChecked = true;

            grid_Controls.Children.Clear();
            grid_Controls.Children.Add(new MultipleTimeInputsControl() { Units = _units });
            ConfigurePlotForMultipleTimeCurves();
            mi_SingleCurve_View.Visibility = Visibility.Collapsed;
        }
    }
}
