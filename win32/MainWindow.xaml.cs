using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
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
using ITS.Propagation;
using p528_gui.Converters;

namespace p528_gui
{
    public enum Units
    {
        Meters,
        Feet
    }

    public enum ApplicationMode
    {
        SingleCurve,
        MultipleHeights,
        MultipleTimes
    }

    public partial class MainWindow : Window
    {
        private const int ERROR_HEIGHT_AND_DISTANCE = 10;
        private const int WARNING__DFRAC_TROPO_REGION = 0xFF1;
        private const int WARNING__LOW_FREQUENCY = 0xFF2;

        public PlotModel PlotModel { get; set; }

        private const int LOS_SERIES = 0;
        private const int DFRAC_SERIES = 1;
        private const int SCAT_SERIES = 2;
        private const int FS_SERIES = 3;

        private Units _units = Units.Meters;
        private bool _showModeOfProp = true;
        private bool _showFreeSpaceLine = true;

        private delegate void RenderPlot();
        private RenderPlot Render;

        readonly LogarithmicAxis _xAxis;
        readonly LinearAxis _yAxis;

        public MainWindow()
        {
            InitializeComponent();

            PlotModel = new PlotModel() { Title = "" };

            _xAxis = new LogarithmicAxis();
            _xAxis.Title = "Distance (km)";
            _xAxis.Minimum = 0;
            _xAxis.Maximum = 1800;
            _xAxis.MajorGridlineStyle = OxyPlot.LineStyle.Dot;
            _xAxis.Position = AxisPosition.Bottom;

            _yAxis = new LinearAxis();
            _yAxis.Title = "Basic Transmission Loss (dB)";
            _yAxis.MajorGridlineStyle = OxyPlot.LineStyle.Dot;
            _yAxis.Position = AxisPosition.Left;
            _yAxis.StartPosition = 1;
            _yAxis.EndPosition = 0;

            PlotModel.Axes.Add(_xAxis);
            PlotModel.Axes.Add(_yAxis);

            DataContext = this;

            tb_ConsistencyWarning.Text = Messages.ModelConsistencyWarning;
            tb_FrequencyWarning.Text = Messages.LowFrequencyWarning;

            SetUnits();

            Render = RenderSingleCurve;
        }

        private void Btn_Render_Click(object sender, RoutedEventArgs e) => Render();

        OxyColor ConvertBrushToOxyColor(Brush brush) => OxyColor.Parse(((SolidColorBrush)brush).Color.ToString());

        private void RenderSingleCurve()
        {
            var inputConrol = grid_Controls.Children[0] as SingleCurveInputsControl;

            mi_Export.IsEnabled = true;

            // convert inputs into metric units
            var h1__meter = (_units == Units.Meters) ? inputConrol.h_1 : (inputConrol.h_1 * Constants.METER_PER_FOOT);
            var h2__meter = (_units == Units.Meters) ? inputConrol.h_2 : (inputConrol.h_2 * Constants.METER_PER_FOOT);
            double f__mhz = inputConrol.f__mhz;
            double time = inputConrol.time;

            int rtn = GetPointsEx(h1__meter, h2__meter, f__mhz, time, out List<Point>btgPoints, 
                out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints, out List<Point> fsPoints, true);

            // Set any warning messages
            tb_ConsistencyWarning.Visibility = ((rtn & WARNING__DFRAC_TROPO_REGION) == WARNING__DFRAC_TROPO_REGION) ? Visibility.Visible : Visibility.Collapsed;
            tb_FrequencyWarning.Visibility = ((rtn & WARNING__LOW_FREQUENCY) == WARNING__LOW_FREQUENCY) ? Visibility.Visible : Visibility.Collapsed;

            // Plot the data
            PlotModel.Series.Clear();

            if (_showModeOfProp)
            {
                var losSeries = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#cc79a7")),
                    Title = "Line of Sight",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                losSeries.Points.AddRange(losPoints.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(losSeries);

                var dfracSeries = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#0072b2")),
                    Title = "Diffraction",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                dfracSeries.Points.AddRange(dfracPoints.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(dfracSeries);

                var scatSeries = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#e69f00")),
                    Title = "Diffraction",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                scatSeries.Points.AddRange(scatPoints.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(scatSeries);
            }
            else
            {
                var series = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#cc79a7")),
                    Title = "Basic Transmission Gain",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                series.Points.AddRange(btgPoints.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(series);
            }

            if (_showFreeSpaceLine)
            {
                // TODO: make dotted line
                var fsSeries = new LineSeries()
                {
                    StrokeThickness = 1,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#000000")),
                    Title = "Free Space",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                fsSeries.Points.AddRange(fsPoints.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(fsSeries);
            }

            plot.InvalidatePlot();
        }

        private void RenderMultipleLowHeights()
        {
            var inputControl = grid_Controls.Children[0] as MultipleLowHeightsInputsControl;
            if (!inputControl.AreInputsValid())
                return;

            mi_Export.IsEnabled = true;

            tb_FrequencyWarning.Visibility = (inputControl.FMHZ < 125) ? Visibility.Visible : Visibility.Collapsed;

            PlotModel.Series.Clear();

            // convert inputs into metric units
            double h_2__meter = (_units == Units.Meters) ? inputControl.H2 : (inputControl.H2 * Constants.METER_PER_FOOT);
            List<double> h_1s__meter = new List<double>();
            for (int i = 0; i < inputControl.H1s.Count; i++)
            {
                double h1 = inputControl.H1s[i];
                double h_1__meter = (_units == Units.Meters) ? h1 : (h1 * Constants.METER_PER_FOOT);

                int rtn = GetPoints(h_1__meter, h_2__meter, inputControl.FMHZ, inputControl.TIME, out List<Point> pts);

                // Plot the data
                var series = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor(Tools.GetBrush(i)),
                    Title = $"{h1} {_units.ToString()}",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                series.Points.AddRange(pts.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(series);
            }

            plot.InvalidatePlot();
        }

        private void RenderMultipleHighHeights()
        {
            var inputControl = grid_Controls.Children[0] as MultipleHighHeightsInputsControl;
            if (!inputControl.AreInputsValid())
                return;

            mi_Export.IsEnabled = true;

            tb_FrequencyWarning.Visibility = (inputControl.FMHZ < 125) ? Visibility.Visible : Visibility.Collapsed;

            PlotModel.Series.Clear();

            // convert inputs into metric units
            double h_1__meter = (_units == Units.Meters) ? inputControl.H1 : (inputControl.H1 * Constants.METER_PER_FOOT);
            List<double> h_2s__meter = new List<double>();
            for (int i = 0; i < inputControl.H2s.Count; i++)
            {
                double h2 = inputControl.H2s[i];
                double h_2__meter = (_units == Units.Meters) ? h2 : (h2 * Constants.METER_PER_FOOT);

                int rtn = GetPoints(h_1__meter, h_2__meter, inputControl.FMHZ, inputControl.TIME, out List<Point> pts);

                // Plot the data
                var series = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor(Tools.GetBrush(i)),
                    Title = $"{h2} {_units.ToString()}",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                series.Points.AddRange(pts.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(series);
            }

            plot.InvalidatePlot();
        }

        private void RenderMultipleTimes()
        {
            var inputControl = grid_Controls.Children[0] as MultipleTimeInputsControl;
            if (!inputControl.AreInputsValid())
                return;

            mi_Export.IsEnabled = true;

            tb_FrequencyWarning.Visibility = (inputControl.FMHZ < 125) ? Visibility.Visible : Visibility.Collapsed;

            PlotModel.Series.Clear();

            double f__mhz = inputControl.FMHZ;
            double h_1__meter = (_units == Units.Meters) ? inputControl.H1 : (inputControl.H1 * Constants.METER_PER_FOOT);
            double h_2__meter = (_units == Units.Meters) ? inputControl.H2 : (inputControl.H2 * Constants.METER_PER_FOOT);

            List<double> times = new List<double>();
            for (int i = 0; i < inputControl.TIMEs.Count; i++)
            {
                double time = inputControl.TIMEs[i];

                int rtn = GetPoints(h_1__meter, h_2__meter, f__mhz, time, out List<Point> pts);

                // Plot the data
                var series = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor(Tools.GetBrush(i)),
                    Title = $"{time * 100}%",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                series.Points.AddRange(pts.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(series);
            }

            if (_showFreeSpaceLine)
            {
                GetPointsEx(h_1__meter, h_2__meter, f__mhz, 0.5, out List<Point> btgPoints, 
                    out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints, out List<Point> fsPoints, true);

                // TODO: make dotted line
                var fsSeries = new LineSeries()
                {
                    StrokeThickness = 1,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#000000")),
                    Title = "Free Space",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };
                fsSeries.Points.AddRange(fsPoints.Select(x => new DataPoint(x.X, x.Y)));
                PlotModel.Series.Add(fsSeries);
            }

            plot.InvalidatePlot();
        }

        private int GetPoints(double h_1__meter, double h_2__meter, double f__mhz, double time, out List<Point> lossPoints)
        {
            lossPoints = new List<Point>();

            int rtn = 0;

            // iterate on user-specified units (km or n miles)
            double d_step = (_xAxis.Maximum - _xAxis.Minimum) / 1500;
            double d = _xAxis.Minimum;
            double d__km, d_out;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                var r = P528.Invoke(d__km, h_1__meter, h_2__meter, f__mhz, P528.Polarization.Horizontal, time, out P528.Result result);

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

        private int GetPointsEx(double h_1__meter, double h_2__meter, double f__mhz, double time, out List<Point> btgPoints, 
            out List<Point> losPoints, out List<Point> dfracPoints, out List<Point> scatPoints, out List<Point> fsPoints, bool blendLines)
        {
            losPoints = new List<Point>();
            dfracPoints = new List<Point>();
            scatPoints = new List<Point>();
            fsPoints = new List<Point>();
            btgPoints = new List<Point>();

            bool dfracSwitch = false;
            bool scatSwitch = false;

            int rtn = 0;
            double d__km, d_out;

            // iterate on user-specified units (km or n miles)
            double d_step = (_xAxis.Maximum - _xAxis.Minimum) / 1500;
            double d = _xAxis.Minimum;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                var r = P528.Invoke(d__km, h_1__meter, h_2__meter, f__mhz, P528.Polarization.Horizontal, time, out P528.Result result);

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
                btgPoints.Add(new Point(d_out, result.A__db));

                d += d_step;
            }

            return rtn;
        }

        private void Mi_Exit_Click(object sender, RoutedEventArgs e) => this.Close();

        #region CSV Export Methods

        private void Mi_Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV file (*.csv)|*.csv";

            if (sfd.ShowDialog() != true)
                return;

            bool result = false;
            if (mi_PlotMode_SingleCurve.IsChecked)
                result = CsvExport_SingleCurve(sfd.FileName);
            if (mi_PlotMode_MultipleLowHeights.IsChecked)
                result =CsvExport_MultipleLowTerminals(sfd.FileName);
            if (mi_PlotMode_MultipleHighHeights.IsChecked)
                result = CsvExport_MultipleHighTerminals(sfd.FileName);
            if (mi_PlotMode_MultipleTimePercentages.IsChecked)
                result = CsvExport_MultipleTimePercentages(sfd.FileName);

            if (result)
                MessageBox.Show("Export Completed");
        }

        private bool CsvExport_SingleCurve(string filepath)
        {
            var inputControl = grid_Controls.Children[0] as SingleCurveInputsControl;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = false };
            if (!exportOptionsWndw.ShowDialog().Value)
                return false;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<double>();
            var A_fs__db = new List<double>();
            var dists = new List<double>();
            var modes = new List<int>();
            int warnings = 0;
            double d__km, d_out;

            double h_1__meter = (_units == Units.Meters) ? inputControl.h_1 : (inputControl.h_1 * Constants.METER_PER_FOOT);
            double h_2__meter = (_units == Units.Meters) ? inputControl.h_2 : (inputControl.h_2 * Constants.METER_PER_FOOT);
            double f__mhz = inputControl.f__mhz;
            double time = inputControl.time;

            double d = _xAxis.Minimum;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                var r = P528.Invoke(d__km, h_1__meter, h_2__meter, f__mhz, P528.Polarization.Horizontal, time, out P528.Result result);

                // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                if (r != ERROR_HEIGHT_AND_DISTANCE && r != 0)
                    warnings = r;

                // convert output distance from P.528 back into user-specified units
                d_out = (_units == Units.Meters) ? result.d__km : (result.d__km / Constants.KM_PER_NAUTICAL_MILE);

                dists.Add(Math.Round(d_out, 0));
                A__db.Add(Math.Round(result.A__db, 3));
                A_fs__db.Add(Math.Round(result.A_fs__db, 3));
                modes.Add(result.propagation_mode);

                d++;
            }

            using (var fs = new StreamWriter(filepath))
            {
                fs.WriteLine($"Data Generated by the ITS ITU-R Rec P.528-{dll.FileMajorPart} GUI");
                fs.WriteLine($"Generated on {DateTime.Now.ToShortDateString()}");
                fs.WriteLine($"App Version,{version.Major}.{version.Minor}.{version.Build}");
                fs.WriteLine($"P.528-{dll.FileMajorPart} DLL Version,{dll.FileMajorPart}.{dll.FileMinorPart}.{dll.FileBuildPart}");

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
                fs.WriteLine($"h_1,{inputControl.h_1}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"h_2,{inputControl.h_2}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{f__mhz}");
                fs.WriteLine($"time%,{time * 100}");
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

            return true;
        }

        private bool CsvExport_MultipleLowTerminals(string filepath)
        {
            var inputControl = grid_Controls.Children[0] as MultipleLowHeightsInputsControl;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return false;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<List<double>>();
            for (int i = 0; i < inputControl.H1s.Count; i++)
                A__db.Add(new List<double>());
            var dists = new List<double>();
            int warnings = 0;
            double d__km;

            double h_2__meter = (_units == Units.Meters) ? inputControl.H2 : (inputControl.H2 * Constants.METER_PER_FOOT);
            double f__mhz = inputControl.FMHZ;
            double time = inputControl.TIME;

            double d = _xAxis.Minimum;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                for (int i = 0; i < inputControl.H1s.Count; i++)
                {
                    double h_1__meter = (_units == Units.Meters) ? inputControl.H1s[i] : (inputControl.H1s[i] * Constants.METER_PER_FOOT);

                    var r = P528.Invoke(d__km, h_1__meter, h_2__meter, f__mhz, P528.Polarization.Horizontal, time, out P528.Result result);

                    // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                    if (r != ERROR_HEIGHT_AND_DISTANCE && r != 0)
                        warnings = r;

                    A__db[i].Add(Math.Round(result.A__db, 3));
                }

                dists.Add(Math.Round(d, 0));
                d++;
            }

            using (var fs = new StreamWriter(filepath))
            {
                fs.WriteLine($"Data Generated by the ITS ITU-R Rec P.528-{dll.FileMajorPart} GUI");
                fs.WriteLine($"Generated on {DateTime.Now.ToShortDateString()}");
                fs.WriteLine($"App Version,{version.Major}.{version.Minor}.{version.Build}");
                fs.WriteLine($"P.528-{dll.FileMajorPart} DLL Version,{dll.FileMajorPart}.{dll.FileMinorPart}.{dll.FileBuildPart}");

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
                fs.WriteLine($"h_2,{inputControl.H2}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{f__mhz}");
                fs.WriteLine($"time%,{time * 100}");
                fs.WriteLine();

                if (exportOptionsWndw.IsRowAlignedData)
                {
                    fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                    fs.WriteLine($"{String.Join(",", dists)}");
                    for (int i = 0; i < inputControl.H1s.Count; i++)
                    {
                        var units = (_units == Units.Meters) ? "meters" : "feet";
                        fs.WriteLine($"h_1 = {inputControl.H1s[i]} {units},{String.Join(",", A__db[i])}");
                    }
                }
                else
                {
                    fs.Write((_units == Units.Meters) ? "d__km" : "d__n_mile");
                    for (int i = 0; i < inputControl.H1s.Count; i++)
                    {
                        var units = (_units == Units.Meters) ? "meters" : "feet";
                        fs.Write($",h_1 = {inputControl.H1s[i]} {units}");
                    }
                    fs.WriteLine();

                    for (int i = 0; i < dists.Count; i++)
                    {
                        fs.Write($"{dists[i]}");
                        for (int j = 0; j < inputControl.H1s.Count; j++)
                            fs.Write($",{A__db[j][i]}");

                        fs.WriteLine();
                    }
                }
            }

            return true;
        }

        private bool CsvExport_MultipleHighTerminals(string filepath)
        {
            var inputControl = grid_Controls.Children[0] as MultipleHighHeightsInputsControl;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return false;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<List<double>>();
            for (int i = 0; i < inputControl.H2s.Count; i++)
                A__db.Add(new List<double>());
            var dists = new List<double>();
            int warnings = 0;
            double d__km;

            double h_1__meter = (_units == Units.Meters) ? inputControl.H1 : (inputControl.H1 * Constants.METER_PER_FOOT);
            double f__mhz = inputControl.FMHZ;
            double time = inputControl.TIME;

            double d = _xAxis.Minimum;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                for (int i = 0; i < inputControl.H2s.Count; i++)
                {
                    double h_2__meter = (_units == Units.Meters) ? inputControl.H2s[i] : (inputControl.H2s[i] * Constants.METER_PER_FOOT);

                    var r = P528.Invoke(d__km, h_1__meter, h_2__meter, f__mhz, P528.Polarization.Horizontal, time, out P528.Result result);

                    // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                    if (r != ERROR_HEIGHT_AND_DISTANCE && r != 0)
                        warnings = r;

                    A__db[i].Add(Math.Round(result.A__db, 3));
                }

                dists.Add(Math.Round(d, 0));
                d++;
            }

            using (var fs = new StreamWriter(filepath))
            {
                fs.WriteLine($"Data Generated by the ITS ITU-R Rec P.528-{dll.FileMajorPart} GUI");
                fs.WriteLine($"Generated on {DateTime.Now.ToShortDateString()}");
                fs.WriteLine($"App Version,{version.Major}.{version.Minor}.{version.Build}");
                fs.WriteLine($"P.528-{dll.FileMajorPart} DLL Version,{dll.FileMajorPart}.{dll.FileMinorPart}.{dll.FileBuildPart}");

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
                fs.WriteLine($"h_1,{inputControl.H1}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{f__mhz}");
                fs.WriteLine($"time%,{time * 100}");
                fs.WriteLine();

                if (exportOptionsWndw.IsRowAlignedData)
                {
                    fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                    fs.WriteLine($"{String.Join(",", dists)}");
                    for (int i = 0; i < inputControl.H2s.Count; i++)
                    {
                        var units = (_units == Units.Meters) ? "meters" : "feet";
                        fs.WriteLine($"h_2 = {inputControl.H2s[i]} {units},{String.Join(",", A__db[i])}");
                    }
                }
                else
                {
                    fs.Write((_units == Units.Meters) ? "d__km" : "d__n_mile");
                    for (int i = 0; i < inputControl.H2s.Count; i++)
                    {
                        var units = (_units == Units.Meters) ? "meters" : "feet";
                        fs.Write($",h_2 = {inputControl.H2s[i]} {units}");
                    }
                    fs.WriteLine();

                    for (int i = 0; i < dists.Count; i++)
                    {
                        fs.Write($"{dists[i]}");
                        for (int j = 0; j < inputControl.H2s.Count; j++)
                            fs.Write($",{A__db[j][i]}");

                        fs.WriteLine();
                    }
                }
            }

            return true;
        }

        private bool CsvExport_MultipleTimePercentages(string filepath)
        {
            var inputControl = grid_Controls.Children[0] as MultipleTimeInputsControl;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return false;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<List<double>>();
            for (int i = 0; i < inputControl.TIMEs.Count; i++)
                A__db.Add(new List<double>());
            var dists = new List<double>();
            int warnings = 0;
            double d__km;

            double h_1__meter = (_units == Units.Meters) ? inputControl.H1 : (inputControl.H1 * Constants.METER_PER_FOOT);
            double h_2__meter = (_units == Units.Meters) ? inputControl.H2 : (inputControl.H2 * Constants.METER_PER_FOOT);
            double f__mhz = inputControl.FMHZ;

            double d = _xAxis.Minimum;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                for (int i = 0; i < inputControl.TIMEs.Count; i++)
                {
                    var r = P528.Invoke(d__km, h_1__meter, h_2__meter, f__mhz, P528.Polarization.Horizontal, inputControl.TIMEs[i], out P528.Result result);

                    // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                    if (r != ERROR_HEIGHT_AND_DISTANCE && r != 0)
                        warnings = r;

                    A__db[i].Add(Math.Round(result.A__db, 3));
                }

                dists.Add(Math.Round(d, 0));
                d++;
            }

            using (var fs = new StreamWriter(filepath))
            {
                fs.WriteLine($"Data Generated by the ITS ITU-R Rec P.528-{dll.FileMajorPart} GUI");
                fs.WriteLine($"Generated on {DateTime.Now.ToShortDateString()}");
                fs.WriteLine($"App Version,{version.Major}.{version.Minor}.{version.Build}");
                fs.WriteLine($"P.528-{dll.FileMajorPart} DLL Version,{dll.FileMajorPart}.{dll.FileMinorPart}.{dll.FileBuildPart}");

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
                fs.WriteLine($"h_1,{inputControl.H1}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"h_2,{inputControl.H2}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{f__mhz}");
                fs.WriteLine();

                if (exportOptionsWndw.IsRowAlignedData)
                {
                    fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                    fs.WriteLine($"{String.Join(",", dists)}");
                    for (int i = 0; i < inputControl.TIMEs.Count; i++)
                        fs.WriteLine($"time = {inputControl.TIMEs[i]} %,{String.Join(",", A__db[i])}");
                }
                else
                {
                    fs.Write((_units == Units.Meters) ? "d__km" : "d__n_mile");
                    for (int i = 0; i < inputControl.TIMEs.Count; i++)
                        fs.Write($",time = {inputControl.TIMEs[i]} %");
                    fs.WriteLine();

                    for (int i = 0; i < dists.Count; i++)
                    {
                        fs.Write($"{dists[i]}");
                        for (int j = 0; j < inputControl.TIMEs.Count; j++)
                            fs.Write($",{A__db[j][i]}");

                        fs.WriteLine();
                    }
                }
            }

            return true;
        }

        #endregion

        private void Mi_About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void Mi_FreeSpace_Click(object sender, RoutedEventArgs e)
        {
            _showFreeSpaceLine = mi_FreeSpace.IsChecked;

            Render();
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
            _xAxis.Title = "Distance " + ((_units == Units.Meters) ? "(km)" : "(n mile)");
            ResetPlot();
            //customToolTip.Units = _units;

            // Clear plot data
            PlotModel.Series.Clear();
            plot.InvalidatePlot();
        }

        private void Mi_SetAxisLimits_Click(object sender, RoutedEventArgs e)
        {
            var limitsWndw = new AxisLimitsWindow()
            {
                XAxisUnit = (_units == Units.Meters) ? "km" : "n mile",
                XAxisMaximum = _xAxis.Maximum,
                XAxisMinimum = _xAxis.Minimum,
                //XAxisStep = xSeparator.Step,
                YAxisMaximum = _yAxis.Maximum,
                YAxisMinimum = _yAxis.Minimum,
                //YAxisStep = ySeparator.Step
            };

            if (!limitsWndw.ShowDialog().Value)
                return;

            _xAxis.Maximum = limitsWndw.XAxisMaximum;
            _xAxis.Minimum = limitsWndw.XAxisMinimum;
            //xSeparator.Step = limitsWndw.XAxisStep;

            _yAxis.Maximum = limitsWndw.YAxisMaximum;
            _yAxis.Minimum = limitsWndw.YAxisMinimum;
            //ySeparator.Step = limitsWndw.YAxisStep;
        }

        private void Mi_ResetAxisLimits_Click(object sender, RoutedEventArgs e) => ResetPlot();
        
        private void ResetPlot()
        {
            if (_units == Units.Meters)
                _xAxis.Maximum = 1800;
            else
                _xAxis.Maximum = 970;

            _xAxis.Minimum = 0;
            //xSeparator.Step = 200;
            _yAxis.Maximum = 300;
            _yAxis.Minimum = 100;
            //ySeparator.Step = 20;

            Render?.Invoke();
        }

        private void Mi_PlotMode_SingleCurve_Click(object sender, RoutedEventArgs e)
        {
            Render = RenderSingleCurve;

            mi_PlotMode_SingleCurve.IsChecked = true;
            mi_PlotMode_MultipleLowHeights.IsChecked = false;
            mi_PlotMode_MultipleHighHeights.IsChecked = false;
            mi_PlotMode_MultipleTimePercentages.IsChecked = false;

            grid_Controls.Children.Clear();
            var singleCurveCtrl = new SingleCurveInputsControl() { Units = _units };
            grid_Controls.Children.Add(singleCurveCtrl);
            PlotModel.Series.Clear();
            plot.InvalidatePlot();
            mi_View.Visibility = Visibility.Visible;
            mi_ModeOfProp.Visibility = Visibility.Visible;

            Binding binding = new Binding("ErrorCnt");
            binding.Source = singleCurveCtrl;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Converter = new IntegerToBooleanConverter();

            BindingOperations.SetBinding(btn_Render, Button.IsEnabledProperty, binding);
        }

        private void Mi_PlotMode_MultipleHighHeights_Click(object sender, RoutedEventArgs e)
        {
            Render = RenderMultipleHighHeights;

            mi_PlotMode_SingleCurve.IsChecked = false;
            mi_PlotMode_MultipleLowHeights.IsChecked = false;
            mi_PlotMode_MultipleHighHeights.IsChecked = true;
            mi_PlotMode_MultipleTimePercentages.IsChecked = false;

            grid_Controls.Children.Clear();
            grid_Controls.Children.Add(new MultipleHighHeightsInputsControl() { Units = _units });
            PlotModel.Series.Clear();
            plot.InvalidatePlot();
            mi_View.Visibility = Visibility.Collapsed;
            mi_ModeOfProp.Visibility = Visibility.Visible;
        }

        private void Mi_PlotMode_MultipleTimePercentages_Click(object sender, RoutedEventArgs e)
        {
            Render = RenderMultipleTimes;

            mi_PlotMode_SingleCurve.IsChecked = false;
            mi_PlotMode_MultipleLowHeights.IsChecked = false;
            mi_PlotMode_MultipleHighHeights.IsChecked = false;
            mi_PlotMode_MultipleTimePercentages.IsChecked = true;

            grid_Controls.Children.Clear();
            grid_Controls.Children.Add(new MultipleTimeInputsControl() { Units = _units });
            PlotModel.Series.Clear();
            plot.InvalidatePlot();
            mi_View.Visibility = Visibility.Visible;
            mi_ModeOfProp.Visibility = Visibility.Collapsed;
        }

        private void Mi_ModeOfProp_Click(object sender, RoutedEventArgs e)
        {
            _showModeOfProp = mi_ModeOfProp.IsChecked;

            Render();
        }

        private void Mi_PlotMode_MultipleLowHeights_Click(object sender, RoutedEventArgs e)
        {
            Render = RenderMultipleLowHeights;

            mi_PlotMode_SingleCurve.IsChecked = false;
            mi_PlotMode_MultipleLowHeights.IsChecked = true;
            mi_PlotMode_MultipleHighHeights.IsChecked = false;
            mi_PlotMode_MultipleTimePercentages.IsChecked = false;

            grid_Controls.Children.Clear();
            grid_Controls.Children.Add(new MultipleLowHeightsInputsControl() { Units = _units });
            PlotModel.Series.Clear();
            plot.InvalidatePlot();
            mi_View.Visibility = Visibility.Collapsed;
            mi_ModeOfProp.Visibility = Visibility.Visible;
        }
    }
}
