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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace p528_gui
{
    #region Common Enums

    enum PlotMode
    {
        Single,
        MultipleLowTerminals,
        MultipleHighTerminals,
        MultipleTimes
    }

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

    enum Polarization : int
    {
        Horizontal = 0,
        Vertical = 1
    }

    #endregion

    class ModelArgs
    {
        public double h_1__meter { get; set; }

        public double h_2__meter { get; set; }

        public double time { get; set; }

        public P528.Polarization Polarization { get; set; }

        public double f__mhz { get; set; }
    }

    class CurveData
    {
        public int Rtn { get; set; } = 0;

        public List<double> Distances { get; set; } = new List<double>();

        public List<double> L_fs__db { get; set; } = new List<double>();

        public List<double> L_btl__db { get; set; } = new List<double>();

        public List<P528.ModeOfPropagation> PropModes { get; set; } = new List<P528.ModeOfPropagation>();
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int ERROR_HEIGHT_AND_DISTANCE = 10;
        private const int WARNING__DFRAC_TROPO_REGION = 0xFF1;

        public PlotModel PlotModel { get; set; }

        private Units _units = Units.Meters;

        public bool IsModeOfPropVisible { get; set; } = true;

        private delegate void RenderPlot();
        private RenderPlot Render;

        readonly LinearAxis _xAxis;
        readonly LinearAxis _yAxis;

        BackgroundWorker _worker;

        public bool IsFreeSpaceLineVisible { get; set; } = true;

        private int _progressPercentage { get; set; } = 0;
        public int ProgressPercentage
        {
            get { return _progressPercentage; }
            set
            {
                _progressPercentage = value;
                OnPropertyChanged();
            }
        }

        private bool _isWorking { get; set; } = false;
        public bool IsWorking
        {
            get { return _isWorking; }
            set
            {
                _isWorking = value;
                OnPropertyChanged();
            }
        }

        Binding _isWorkingBinding;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        LineSeries _fsSeries;
        LineSeries _btlSeries;
        LineSeries _losSeries;
        LineSeries _dfracSeries;
        LineSeries _scatSeries;

        public MainWindow()
        {
            InitializeComponent();

            PlotModel = new PlotModel() { Title = "" };

            _xAxis = new LinearAxis();
            _xAxis.Title = "Distance (km)";
            _xAxis.Minimum = 0;
            _xAxis.Maximum = 1800;
            _xAxis.MajorGridlineStyle = LineStyle.Dot;
            _xAxis.Position = AxisPosition.Bottom;

            _yAxis = new LinearAxis();
            _yAxis.Title = "Basic Transmission Loss (dB)";
            _yAxis.MajorGridlineStyle = LineStyle.Dot;
            _yAxis.Position = AxisPosition.Left;
            _yAxis.StartPosition = 1;
            _yAxis.EndPosition = 0;

            PlotModel.Axes.Add(_xAxis);
            PlotModel.Axes.Add(_yAxis);

            DataContext = this;

            tb_ConsistencyWarning.Text = Messages.ModelConsistencyWarning;

            SetUnits();

            Render = RenderSingleCurve;

            InitializeLineSeries();

            _isWorkingBinding = new Binding("IsWorking");
            _isWorkingBinding.Source = this;
            _isWorkingBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            _isWorkingBinding.Converter = new BooleanInverterConverter();


            _worker = new BackgroundWorker();
            _worker.DoWork += Worker_RunP528;
            _worker.WorkerReportsProgress = true;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.WorkerSupportsCancellation = true;
        }

        private void InitializeLineSeries()
        {
            // set up free space loss line and bind it to boolean
            _fsSeries = new LineSeries()
            {
                StrokeThickness = 1,
                MarkerSize = 0,
                LineStyle = LineStyle.Dot,
                Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#000000")),
                Title = "Free Space",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = IsFreeSpaceLineVisible
            };

            _btlSeries = new LineSeries()
            {
                StrokeThickness = 5,
                MarkerSize = 0,
                LineStyle = LineStyle.Solid,
                Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#cc79a7")),
                Title = "Basic Transmission Loss",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = !IsModeOfPropVisible
            };

            _losSeries = new LineSeries()
            {
                StrokeThickness = 5,
                MarkerSize = 0,
                LineStyle = LineStyle.Solid,
                Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#cc79a7")),
                Title = "Line of Sight",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = IsModeOfPropVisible
            };

            _dfracSeries = new LineSeries()
            {
                StrokeThickness = 5,
                MarkerSize = 0,
                LineStyle = LineStyle.Solid,
                Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#0072b2")),
                Title = "Diffraction",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = IsModeOfPropVisible
            };

            _scatSeries = new LineSeries()
            {
                StrokeThickness = 5,
                MarkerSize = 0,
                LineStyle = LineStyle.Solid,
                Color = ConvertBrushToOxyColor((SolidColorBrush)new BrushConverter().ConvertFrom("#e69f00")),
                Title = "Troposcatter",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = IsModeOfPropVisible
            };
        }

        private void Btn_Render_Click(object sender, RoutedEventArgs e) => Render();

        OxyColor ConvertBrushToOxyColor(Brush brush) => OxyColor.Parse(((SolidColorBrush)brush).Color.ToString());

        #region Render Methods (Prep Background Worker)

        private void RenderSingleCurve()
        {
            var inputControl = grid_InputControls.Children[0] as SingleCurveInputsControl;

            mi_Export.IsEnabled = true;

            // convert inputs into metric units
            var h_1__meter = (_units == Units.Meters) ? inputControl.h_1 : (inputControl.h_1 * Constants.METER_PER_FOOT);
            var h_2__meter = (_units == Units.Meters) ? inputControl.h_2 : (inputControl.h_2 * Constants.METER_PER_FOOT);

            // generate list of jobs - only single curve so just 1 job
            var jobs = new List<ModelArgs>()
            {
                new ModelArgs()
                {
                    h_1__meter = h_1__meter,
                    h_2__meter = h_2__meter,
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                }
            };

            // start the background worker
            _worker.RunWorkerCompleted += Worker_SingleCurveWorkerCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void RenderMultipleLowHeights()
        {
            var inputControl = grid_InputControls.Children[0] as MultipleLowHeightsInputsControl;

            mi_Export.IsEnabled = true;

            // convert inputs into metric units
            double h_2__meter = (_units == Units.Meters) ? inputControl.h_2 : (inputControl.h_2 * Constants.METER_PER_FOOT);

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            for (int i = 0; i < inputControl.h_1s.Count; i++)
            {
                double h_1 = inputControl.h_1s[i];
                double h_1__meter = (_units == Units.Meters) ? h_1 : (h_1 * Constants.METER_PER_FOOT);

                jobs.Add(new ModelArgs()
                {
                    h_1__meter = h_1__meter,
                    h_2__meter = h_2__meter,
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                });
            }

            // start the background worker
            _worker.RunWorkerCompleted += Worker_MultipleLowHeightsWorkerCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void RenderMultipleHighHeights()
        {
            var inputControl = grid_InputControls.Children[0] as MultipleHighHeightsInputsControl;

            mi_Export.IsEnabled = true;

            // convert inputs into metric units
            double h_1__meter = (_units == Units.Meters) ? inputControl.h_1 : (inputControl.h_1 * Constants.METER_PER_FOOT);

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            for (int i = 0; i < inputControl.h_2s.Count; i++)
            {
                double h_2 = inputControl.h_2s[i];
                double h_2__meter = (_units == Units.Meters) ? h_2 : (h_2 * Constants.METER_PER_FOOT);

                jobs.Add(new ModelArgs()
                {
                    h_1__meter = h_1__meter,
                    h_2__meter = h_2__meter,
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                });
            }

            // start the background worker
            _worker.RunWorkerCompleted += Worker_MultipleHighHeightsWorkerCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void RenderMultipleTimes()
        {
            var inputControl = grid_InputControls.Children[0] as MultipleTimeInputsControl;

            mi_Export.IsEnabled = true;

            double h_1__meter = (_units == Units.Meters) ? inputControl.h_1 : (inputControl.h_1 * Constants.METER_PER_FOOT);
            double h_2__meter = (_units == Units.Meters) ? inputControl.h_2 : (inputControl.h_2 * Constants.METER_PER_FOOT);

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            foreach (var time in inputControl.times)
            {
                jobs.Add(new ModelArgs()
                {
                    h_1__meter = h_1__meter,
                    h_2__meter = h_2__meter,
                    f__mhz = inputControl.f__mhz,
                    time = time,
                    Polarization = inputControl.Polarization
                });
            }

            // start the background worker
            _worker.RunWorkerCompleted += Worker_MultipleTimesWorkerCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        #endregion

        #region Background Worker Methods

        /// <summary>
        /// Main execution code of background worker
        /// </summary>
        private void Worker_RunP528(object sender, DoWorkEventArgs e)
        {
            // set the UI to reflect work is being done
            IsWorking = true;
            _worker.ReportProgress(0);

            // grab the pending jobs
            var jobs = (List<ModelArgs>)e.Argument;

            // set up List to store job results
            var jobResults = new List<CurveData>();

            // execute jobs
            for (int i = 0; i < jobs.Count; i++)
            {
                var curveData = new CurveData();

                double d__km, d_out__user_units;

                // iterate on user-specified units (km or n miles)
                int steps = 500;
                double d_step__user_units = (_xAxis.Maximum - _xAxis.Minimum) / steps;
                double d__user_units = _xAxis.Minimum;

                while (d__user_units <= _xAxis.Maximum)
                {
                    // convert distance to specified units for input to P.528
                    d__km = (_units == Units.Meters) ? d__user_units : (d__user_units * Constants.KM_PER_NAUTICAL_MILE);

                    var rtn = P528.Invoke(d__km, jobs[i].h_1__meter, jobs[i].h_2__meter, jobs[i].f__mhz, 
                        jobs[i].Polarization, jobs[i].time, out P528.Result result);

                    // convert output distance from P.528 back into user-specified units
                    d_out__user_units = (_units == Units.Meters) ? result.d__km : (result.d__km / Constants.KM_PER_NAUTICAL_MILE);

                    // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                    if (rtn != ERROR_HEIGHT_AND_DISTANCE && rtn != 0)
                        curveData.Rtn = rtn;

                    // record the result
                    curveData.Distances.Add(d_out__user_units);
                    curveData.L_btl__db.Add(result.A__db);
                    curveData.L_fs__db.Add(result.A_fs__db);
                    curveData.PropModes.Add(result.ModeOfPropagation);

                    // interate
                    d__user_units += d_step__user_units;

                    _worker.ReportProgress(Convert.ToInt32(100 * (i + d__user_units / _xAxis.Maximum) / jobs.Count));
                    if (_worker.CancellationPending)
                    {
                        e.Cancel = true;
                        IsWorking = false;
                        return;
                    }
                }

                jobResults.Add(curveData);
            }

            e.Cancel = false;
            e.Result = jobResults;

            IsWorking = false;
        }

        /// <summary>
        /// Report on updated progress of background worker
        /// </summary>
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
            => ProgressPercentage = e.ProgressPercentage;

        /// <summary>
        /// Single curve mode job is completed
        /// </summary>
        private void Worker_SingleCurveWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // unsubscribe to future events
            _worker.RunWorkerCompleted -= Worker_SingleCurveWorkerCompleted;

            // check if worker finished its jobs
            if (e.Cancelled)
                return;

            // this is a single curve worker, so we can safely call First()
            var curveData = ((List<CurveData>)e.Result).First();

            // Set any warning messages
            tb_ConsistencyWarning.Visibility = ((curveData.Rtn & WARNING__DFRAC_TROPO_REGION) == WARNING__DFRAC_TROPO_REGION) ? Visibility.Visible : Visibility.Collapsed;

            ResetPlotData();

            // build lines
            for (int i = 0; i < curveData.Distances.Count; i++)
            {
                switch (curveData.PropModes[i])
                {
                    case P528.ModeOfPropagation.LineOfSight:
                        _losSeries.Points.Add(new DataPoint(curveData.Distances[i], curveData.L_btl__db[i]));
                        break;
                    case P528.ModeOfPropagation.Diffraction:
                        _dfracSeries.Points.Add(new DataPoint(curveData.Distances[i], curveData.L_btl__db[i]));
                        break;
                    case P528.ModeOfPropagation.Troposcatter:
                        _scatSeries.Points.Add(new DataPoint(curveData.Distances[i], curveData.L_btl__db[i]));
                        break;
                }

                _btlSeries.Points.Add(new DataPoint(curveData.Distances[i], curveData.L_btl__db[i]));
                _fsSeries.Points.Add(new DataPoint(curveData.Distances[i], curveData.L_fs__db[i]));
            }

            // fill in the gaps between line segments to make continuous
            _losSeries.Points.Add(_dfracSeries.Points.First());
            _dfracSeries.Points.Add(_scatSeries.Points.First());

            // add all line series to plot
            PlotModel.Series.Add(_losSeries);
            PlotModel.Series.Add(_dfracSeries);
            PlotModel.Series.Add(_scatSeries);
            PlotModel.Series.Add(_btlSeries);
            PlotModel.Series.Add(_fsSeries);

            // redraw the plot
            plot.InvalidatePlot();
        }

        /// <summary>
        /// Multiple low terminal curves job is completed
        /// </summary>
        private void Worker_MultipleLowHeightsWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // unsubscribe to future events
            _worker.RunWorkerCompleted -= Worker_MultipleLowHeightsWorkerCompleted;

            // check if worker finished its jobs
            if (e.Cancelled)
                return;

            var jobResults = (List<CurveData>)e.Result;

            var inputControl = grid_InputControls.Children[0] as MultipleLowHeightsInputsControl;

            ResetPlotData();

            int j = 0;
            foreach (var curveData in jobResults)
            {
                // Plot the data
                var series = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor(Tools.GetBrush(j)),
                    Title = $"{inputControl.h_1s[j]} {_units.ToString()}",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };

                // build line
                for (int i = 0; i < curveData.Distances.Count; i++)
                    series.Points.Add(new DataPoint(curveData.Distances[i], curveData.L_btl__db[i]));

                // add to plot
                PlotModel.Series.Add(series);

                j++;
            }

            // redraw the plot
            plot.InvalidatePlot();
        }

        /// <summary>
        /// Multiple high terminal curves job is completed
        /// </summary>
        private void Worker_MultipleHighHeightsWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // unsubscribe to future events
            _worker.RunWorkerCompleted -= Worker_MultipleHighHeightsWorkerCompleted;

            // check if worker finished its jobs
            if (e.Cancelled)
                return;

            var jobResults = (List<CurveData>)e.Result;

            var inputControl = grid_InputControls.Children[0] as MultipleHighHeightsInputsControl;

            ResetPlotData();

            int j = 0;
            foreach (var curveData in jobResults)
            {
                // Plot the data
                var series = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = LineStyle.Solid,
                    Color = ConvertBrushToOxyColor(Tools.GetBrush(j)),
                    Title = $"{inputControl.h_2s[j]} {_units.ToString()}",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };

                // build line
                for (int i = 0; i < curveData.Distances.Count; i++)
                    series.Points.Add(new DataPoint(curveData.Distances[i], curveData.L_btl__db[i]));

                // add to plot
                PlotModel.Series.Add(series);

                j++;
            }

            // redraw the plot
            plot.InvalidatePlot();
        }

        private void Worker_MultipleTimesWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // unsubscribe to future events
            _worker.RunWorkerCompleted -= Worker_MultipleHighHeightsWorkerCompleted;

            // check if worker finished its jobs
            if (e.Cancelled)
                return;

            var jobResults = (List<CurveData>)e.Result;

            var inputControl = grid_InputControls.Children[0] as MultipleTimeInputsControl;

            ResetPlotData();

            int j = 0;
            foreach (var curveData in jobResults)
            {
                // Plot the data
                var series = new LineSeries()
                {
                    StrokeThickness = 5,
                    MarkerSize = 0,
                    LineStyle = LineStyle.Solid,
                    Color = ConvertBrushToOxyColor(Tools.GetBrush(j)),
                    Title = $"{inputControl.times[j]}%",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };

                // build line
                for (int i = 0; i < curveData.Distances.Count; i++)
                    series.Points.Add(new DataPoint(curveData.Distances[i], curveData.L_btl__db[i]));

                // add to plot
                PlotModel.Series.Add(series);

                j++;
            }

            // update free space line
            _fsSeries.Points.Clear();
            for (int i = 0; i < jobResults[0].Distances.Count; i++)
                _fsSeries.Points.Add(new DataPoint(jobResults[0].Distances[i], jobResults[0].L_fs__db[i]));
            PlotModel.Series.Add(_fsSeries);

            // redraw the plot
            plot.InvalidatePlot();
        }

        #endregion

        #region CSV Export Methods

        private void Mi_Export_Click(object sender, RoutedEventArgs e)
        {
            //SaveFileDialog sfd = new SaveFileDialog();
            //sfd.Filter = "CSV file (*.csv)|*.csv";

            //if (sfd.ShowDialog() != true)
            //    return;

            //bool result = false;
            //if (mi_PlotMode_SingleCurve.IsChecked)
            //    result = CsvExport_SingleCurve(sfd.FileName);
            //if (mi_PlotMode_MultipleLowHeights.IsChecked)
            //    result =CsvExport_MultipleLowTerminals(sfd.FileName);
            //if (mi_PlotMode_MultipleHighHeights.IsChecked)
            //    result = CsvExport_MultipleHighTerminals(sfd.FileName);
            //if (mi_PlotMode_MultipleTimePercentages.IsChecked)
            //    result = CsvExport_MultipleTimePercentages(sfd.FileName);

            //if (result)
            //    MessageBox.Show("Export Completed");
        }

        private bool CsvExport_SingleCurve(string filepath)
        {
            var inputControl = grid_InputControls.Children[0] as SingleCurveInputsControl;

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
                modes.Add((int)result.ModeOfPropagation);

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
            var inputControl = grid_InputControls.Children[0] as MultipleLowHeightsInputsControl;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return false;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<List<double>>();
            for (int i = 0; i < inputControl.h_1s.Count; i++)
                A__db.Add(new List<double>());
            var dists = new List<double>();
            int warnings = 0;
            double d__km;

            double h_2__meter = (_units == Units.Meters) ? inputControl.h_2 : (inputControl.h_2 * Constants.METER_PER_FOOT);
            double f__mhz = inputControl.f__mhz;
            double time = inputControl.time;

            double d = _xAxis.Minimum;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                for (int i = 0; i < inputControl.h_1s.Count; i++)
                {
                    double h_1__meter = (_units == Units.Meters) ? inputControl.h_1s[i] : (inputControl.h_1s[i] * Constants.METER_PER_FOOT);

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
                }

                fs.WriteLine();
                fs.WriteLine($"h_2,{inputControl.h_2}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{f__mhz}");
                fs.WriteLine($"time%,{time * 100}");
                fs.WriteLine();

                if (exportOptionsWndw.IsRowAlignedData)
                {
                    fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                    fs.WriteLine($"{String.Join(",", dists)}");
                    for (int i = 0; i < inputControl.h_1s.Count; i++)
                    {
                        var units = (_units == Units.Meters) ? "meters" : "feet";
                        fs.WriteLine($"h_1 = {inputControl.h_1s[i]} {units},{String.Join(",", A__db[i])}");
                    }
                }
                else
                {
                    fs.Write((_units == Units.Meters) ? "d__km" : "d__n_mile");
                    for (int i = 0; i < inputControl.h_1s.Count; i++)
                    {
                        var units = (_units == Units.Meters) ? "meters" : "feet";
                        fs.Write($",h_1 = {inputControl.h_1s[i]} {units}");
                    }
                    fs.WriteLine();

                    for (int i = 0; i < dists.Count; i++)
                    {
                        fs.Write($"{dists[i]}");
                        for (int j = 0; j < inputControl.h_1s.Count; j++)
                            fs.Write($",{A__db[j][i]}");

                        fs.WriteLine();
                    }
                }
            }

            return true;
        }

        private bool CsvExport_MultipleHighTerminals(string filepath)
        {
            var inputControl = grid_InputControls.Children[0] as MultipleHighHeightsInputsControl;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return false;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<List<double>>();
            for (int i = 0; i < inputControl.h_2s.Count; i++)
                A__db.Add(new List<double>());
            var dists = new List<double>();
            int warnings = 0;
            double d__km;

            double h_1__meter = (_units == Units.Meters) ? inputControl.h_1 : (inputControl.h_1 * Constants.METER_PER_FOOT);
            double f__mhz = inputControl.f__mhz;
            double time = inputControl.time;

            double d = _xAxis.Minimum;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                for (int i = 0; i < inputControl.h_2s.Count; i++)
                {
                    double h_2__meter = (_units == Units.Meters) ? inputControl.h_2s[i] : (inputControl.h_2s[i] * Constants.METER_PER_FOOT);

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
                }

                fs.WriteLine();
                fs.WriteLine($"h_1,{inputControl.h_1}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{f__mhz}");
                fs.WriteLine($"time%,{time * 100}");
                fs.WriteLine();

                if (exportOptionsWndw.IsRowAlignedData)
                {
                    fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                    fs.WriteLine($"{String.Join(",", dists)}");
                    for (int i = 0; i < inputControl.h_2s.Count; i++)
                    {
                        var units = (_units == Units.Meters) ? "meters" : "feet";
                        fs.WriteLine($"h_2 = {inputControl.h_2s[i]} {units},{String.Join(",", A__db[i])}");
                    }
                }
                else
                {
                    fs.Write((_units == Units.Meters) ? "d__km" : "d__n_mile");
                    for (int i = 0; i < inputControl.h_2s.Count; i++)
                    {
                        var units = (_units == Units.Meters) ? "meters" : "feet";
                        fs.Write($",h_2 = {inputControl.h_2s[i]} {units}");
                    }
                    fs.WriteLine();

                    for (int i = 0; i < dists.Count; i++)
                    {
                        fs.Write($"{dists[i]}");
                        for (int j = 0; j < inputControl.h_2s.Count; j++)
                            fs.Write($",{A__db[j][i]}");

                        fs.WriteLine();
                    }
                }
            }

            return true;
        }

        private bool CsvExport_MultipleTimePercentages(string filepath)
        {
            var inputControl = grid_InputControls.Children[0] as MultipleTimeInputsControl;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return false;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dll = FileVersionInfo.GetVersionInfo("p528.dll");

            // Regenerate data at 1 km steps for export
            var A__db = new List<List<double>>();
            for (int i = 0; i < inputControl.times.Count; i++)
                A__db.Add(new List<double>());
            var dists = new List<double>();
            int warnings = 0;
            double d__km;

            double h_1__meter = (_units == Units.Meters) ? inputControl.h_1 : (inputControl.h_1 * Constants.METER_PER_FOOT);
            double h_2__meter = (_units == Units.Meters) ? inputControl.h_2 : (inputControl.h_2 * Constants.METER_PER_FOOT);
            double f__mhz = inputControl.f__mhz;

            double d = _xAxis.Minimum;
            while (d <= _xAxis.Maximum)
            {
                // convert distance to specified units for input to P.528
                d__km = (_units == Units.Meters) ? d : (d * Constants.KM_PER_NAUTICAL_MILE);

                for (int i = 0; i < inputControl.times.Count; i++)
                {
                    var r = P528.Invoke(d__km, h_1__meter, h_2__meter, f__mhz, P528.Polarization.Horizontal, inputControl.times[i], out P528.Result result);

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
                }

                fs.WriteLine();
                fs.WriteLine($"h_1,{inputControl.h_1}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"h_2,{inputControl.h_2}," + ((_units == Units.Meters) ? "meters" : "feet"));
                fs.WriteLine($"f__mhz,{f__mhz}");
                fs.WriteLine();

                if (exportOptionsWndw.IsRowAlignedData)
                {
                    fs.Write(((_units == Units.Meters) ? "d__km" : "d__n_mile") + ",");
                    fs.WriteLine($"{String.Join(",", dists)}");
                    for (int i = 0; i < inputControl.times.Count; i++)
                        fs.WriteLine($"time = {inputControl.times[i]} %,{String.Join(",", A__db[i])}");
                }
                else
                {
                    fs.Write((_units == Units.Meters) ? "d__km" : "d__n_mile");
                    for (int i = 0; i < inputControl.times.Count; i++)
                        fs.Write($",time = {inputControl.times[i]} %");
                    fs.WriteLine();

                    for (int i = 0; i < dists.Count; i++)
                    {
                        fs.Write($"{dists[i]}");
                        for (int j = 0; j < inputControl.times.Count; j++)
                            fs.Write($",{A__db[j][i]}");

                        fs.WriteLine();
                    }
                }
            }

            return true;
        }

        #endregion

        #region Menu Event Handlers

        /// <summary>
        /// Exit the application
        /// </summary>
        private void Mi_Exit_Click(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>
        /// Update the visibility of the free space transmission loss line
        /// </summary>
        private void Mi_FreeSpace_Click(object sender, RoutedEventArgs e)
        {
            _fsSeries.IsVisible = IsFreeSpaceLineVisible;
            plot.InvalidatePlot();
        }

        /// <summary>
        /// Update the visibility of the model of propagation data
        /// </summary>
        private void Mi_ModeOfProp_Click(object sender, RoutedEventArgs e)
        {
            _losSeries.IsVisible = IsModeOfPropVisible;
            _dfracSeries.IsVisible = IsModeOfPropVisible;
            _scatSeries.IsVisible = IsModeOfPropVisible;
            _btlSeries.IsVisible = !IsModeOfPropVisible;

            plot.InvalidatePlot();
        }

        /// <summary>
        /// Show the About window
        /// </summary>
        private void Mi_About_Click(object sender, RoutedEventArgs e)
            => new AboutWindow().ShowDialog();

        #endregion

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
            if (grid_InputControls.Children.Count > 0)
            {
                // Update text
                (grid_InputControls.Children[0] as IUnitEnabled).Units = _units;
                _xAxis.Title = "Distance " + ((_units == Units.Meters) ? "(km)" : "(n mile)");
                ResetPlot();
                //customToolTip.Units = _units;
            }

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

            ResetPlotData();
        }

        private void ResetPlotData()
        {
            // clear the plot of any data series
            PlotModel.Series.Clear();

            // clear lines series of data points
            _losSeries.Points.Clear();
            _dfracSeries.Points.Clear();
            _scatSeries.Points.Clear();
            _btlSeries.Points.Clear();
            _fsSeries.Points.Clear();
        }

        

        /// <summary>
        /// Controls the application mode with respect to the type of plot generated
        /// </summary>
        void Command_PlotMode(object sender, ExecutedRoutedEventArgs e)
        {
            // grab the command parameter
            var plotMode = (PlotMode)e.Parameter;

            // set the appropriate menu check
            foreach (MenuItem mi in mi_Mode.Items)
                mi.IsChecked = (PlotMode)mi.CommandParameter == plotMode;

            // reset the UI
            grid_InputControls.Children.Clear();
            PlotModel.Series.Clear();
            plot.InvalidatePlot();

            UserControl userControl = null;            

            // set the application with the correct UI elements and configuration
            switch (plotMode)
            {
                case PlotMode.Single:
                    Render = RenderSingleCurve;

                    userControl = new SingleCurveInputsControl() { Units = _units };
                    
                    mi_View.Visibility = Visibility.Visible;
                    IsModeOfPropVisible = true;
                    break;

                case PlotMode.MultipleLowTerminals:
                    Render = RenderMultipleLowHeights;

                    userControl = new MultipleLowHeightsInputsControl() { Units = _units };

                    mi_View.Visibility = Visibility.Collapsed;
                    IsModeOfPropVisible = true;
                    break;

                case PlotMode.MultipleHighTerminals:
                    Render = RenderMultipleHighHeights;

                    userControl = new MultipleHighHeightsInputsControl() { Units = _units };

                    mi_View.Visibility = Visibility.Collapsed;
                    IsModeOfPropVisible = true;
                    break;

                case PlotMode.MultipleTimes:
                    Render = RenderMultipleTimes;

                    userControl = new MultipleTimeInputsControl() { Units = _units };

                    mi_View.Visibility = Visibility.Visible;
                    IsModeOfPropVisible = false;
                    break;
            }

            grid_InputControls.Children.Add(userControl);

            // define binding for input validation errors
            Binding inputErrorBinding = new Binding("ErrorCnt");
            inputErrorBinding.Source = userControl;
            inputErrorBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            inputErrorBinding.Converter = new IntegerToBooleanConverter();

            // define multibinding for Render button to both input validation and background worker status
            MultiBinding multiBinding = new MultiBinding();
            multiBinding.Bindings.Add(inputErrorBinding);
            multiBinding.Bindings.Add(_isWorkingBinding);
            multiBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            multiBinding.Converter = new MultipleBooleanAndConverter();

            // set bindings
            BindingOperations.SetBinding(userControl, IsEnabledProperty, _isWorkingBinding);
            BindingOperations.SetBinding(btn_Render, IsEnabledProperty, multiBinding);

            // force update the view
            PlotModel.Series.Clear();
            plot.InvalidatePlot();
            //ActivePlot = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // initialized application for Single Curve Mode
            var command = PlotModeCommand.Command;
            command.Execute(PlotMode.Single);
        }

        private void Btn_CancelWork_Click(object sender, RoutedEventArgs e) => _worker.CancelAsync();
    }
}
