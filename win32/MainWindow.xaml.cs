using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Microsoft.Win32;
using P528GUI.UserControls;
using P528GUI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ITS.Propagation;
using P528GUI.Converters;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace P528GUI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Private Fields 

        /// <summary>
        /// X-axis for the plot
        /// </summary>
        private readonly LinearAxis _xAxis;

        /// <summary>
        /// Y-axis for the plot
        /// </summary>
        private readonly LinearAxis _yAxis;

        /// <summary>
        /// BackgroundWorker that handles data generation on a different thread
        /// </summary>
        private BackgroundWorker _worker;

        /// <summary>
        /// Backing field for ProgressPercentage
        /// </summary>
        private int _progressPercentage { get; set; } = 0;

        /// <summary>
        /// Backing field for ProgressMsg
        /// </summary>
        private string _progressMsg = String.Empty;

        /// <summary>
        /// Status of the P528 backgroundworker
        /// </summary>
        private bool _isWorking { get; set; } = false;

        /// <summary>
        /// Is analysis able to be exported to CSV
        /// </summary>
        private bool _isExportable = false;

        /// <summary>
        /// Is figure able to be saved as image
        /// </summary>
        private bool _isSaveable = false;

        /// <summary>
        /// Is the mode of propagation menu item enabled
        /// </summary>
        private bool _isModeOfPropEnabled = false;

        /// <summary>
        /// Is the mode of propagation menu item checked
        /// </summary>
        private bool _isModeOfPropChecked = true;

        /// <summary>
        /// Shared binding object
        /// </summary>
        private Binding _isWorkingBinding;

        /// <summary>
        /// Default number of steps in plot
        /// </summary>
        private int _steps = 500;

        /// <summary>
        /// Flag if P528 backgroundworker to used _steps or full resolution when generating data
        /// </summary>
        private bool _fullResolution = false;

        #endregion

        #region Properties

        /// <summary>
        /// Data backing the plot UI control
        /// </summary>
        public PlotModel PlotModel { get; set; }

        /// <summary>
        /// Is the propagation mode visible
        /// </summary>
        public bool IsModeOfPropChecked
        {
            get { return _isModeOfPropChecked; }
            set
            {
                _isModeOfPropChecked = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Is the propagation mode menu item enabled
        /// </summary>
        public bool IsModeOfPropEnabled
        {
            get { return _isModeOfPropEnabled; }
            set
            {
                _isModeOfPropEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Is the free space basic transmission loss curve visible?
        /// </summary>
        public bool IsFreeSpaceLineVisible { get; set; } = true;

        /// <summary>
        /// Progress precentage of the P528 background worker
        /// </summary>
        public int ProgressPercentage
        {
            get { return _progressPercentage; }
            set
            {
                _progressPercentage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Message to user on task of P528 backgroudn worker
        /// </summary>
        public string ProgressMsg
        {
            get { return _progressMsg; }
            set
            {
                _progressMsg = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Status of P528 background worker
        /// </summary>
        public bool IsWorking
        {
            get { return _isWorking; }
            set
            {
                _isWorking = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Is the plot in an exportable state
        /// </summary>
        public bool IsExportable
        {
            get { return _isExportable; }
            set
            {
                _isExportable = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Is the plot in a saveable state
        /// </summary>
        public bool IsSaveable
        {
            get { return _isSaveable; }
            set
            {
                _isSaveable = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private delegate void RenderPlot();
        private RenderPlot Render;

        private delegate void ExportData();
        private ExportData Export;

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

            PlotModel = new PlotModel() { Title = "P.528 Prediction Results" };

            // configure x-axis
            _xAxis = new LinearAxis();
            _xAxis.Title = "Distance (km)";
            _xAxis.Minimum = Constants.XAXIS_MIN_DEFAULT; ;
            _xAxis.Maximum = Constants.XAXIS_MAX_DEFAULT__KM;
            _xAxis.MajorGridlineStyle = OxyPlot.LineStyle.Dot;
            _xAxis.Position = AxisPosition.Bottom;
            _xAxis.AxisChanged += XAxis_Changed;

            // configure y-axis
            _yAxis = new LinearAxis();
            _yAxis.Title = "Basic Transmission Loss (dB)";
            _yAxis.MajorGridlineStyle = OxyPlot.LineStyle.Dot;
            _yAxis.Position = AxisPosition.Left;
            _yAxis.StartPosition = 1;
            _yAxis.EndPosition = 0;
            _yAxis.Minimum = Constants.YAXIS_MIN_DEFAULT;
            _yAxis.Maximum = Constants.YAXIS_MAX_DEFAULT;

            // add axis' to plot
            PlotModel.Axes.Add(_xAxis);
            PlotModel.Axes.Add(_yAxis);

            // enable data binding
            DataContext = this;

            tb_ConsistencyWarning.Text = Messages.ModelConsistencyWarning;

            SetUnits();

            Render = RenderSingleCurve;

            InitializeLineSeries();

            _isWorkingBinding = new Binding("IsWorking");
            _isWorkingBinding.Source = this;
            _isWorkingBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            _isWorkingBinding.Converter = new BooleanInverterConverter();

            // set up the background worker for P528 data generation
            _worker = new BackgroundWorker();
            _worker.DoWork += Worker_RunP528;
            _worker.WorkerReportsProgress = true;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.WorkerSupportsCancellation = true;
        }
        
        #region Render Methods (Prep Background Worker)

        private void RenderSingleCurve()
        {
            IsExportable = false;

            var inputControl = grid_InputControls.Children[0] as SingleCurveInputsControl;

            // generate list of jobs - only single curve so just 1 job
            var jobs = new List<ModelArgs>()
            {
                new ModelArgs()
                {
                    h_1__user_units = inputControl.h_1,
                    h_2__user_units = inputControl.h_2,
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                }
            };

            ProgressMsg = Constants.RENDER_MSG;

            // start the background worker
            _worker.RunWorkerCompleted += Worker_SingleCurveWorkerCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void RenderMultipleLowHeights()
        {
            IsExportable = false;

            var inputControl = grid_InputControls.Children[0] as MultipleLowHeightsInputsControl;

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            foreach (double h_1 in inputControl.h_1s)
                jobs.Add(new ModelArgs()
                {
                    h_1__user_units = h_1,
                    h_2__user_units = inputControl.h_2,
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                });

            ProgressMsg = Constants.RENDER_MSG;

            // start the background worker
            _worker.RunWorkerCompleted += Worker_MultipleLowHeightsWorkerCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void RenderMultipleHighHeights()
        {
            IsExportable = false;

            var inputControl = grid_InputControls.Children[0] as MultipleHighHeightsInputsControl;

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            foreach (double h_2 in inputControl.h_2s)
                jobs.Add(new ModelArgs()
                {
                    h_1__user_units = inputControl.h_1,
                    h_2__user_units = h_2,
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                });

            ProgressMsg = Constants.RENDER_MSG;

            // start the background worker
            _worker.RunWorkerCompleted += Worker_MultipleHighHeightsWorkerCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void RenderMultipleTimes()
        {
            IsExportable = false;

            var inputControl = grid_InputControls.Children[0] as MultipleTimeInputsControl;

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            foreach (var time in inputControl.times)
                jobs.Add(new ModelArgs()
                {
                    h_1__user_units = inputControl.h_1,
                    h_2__user_units = inputControl.h_2,
                    f__mhz = inputControl.f__mhz,
                    time = time,
                    Polarization = inputControl.Polarization
                });

            ProgressMsg = Constants.RENDER_MSG;

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

            double min = Math.Max(0, _xAxis.Minimum);
            double max = _xAxis.ActualMaximum;

            // execute jobs
            for (int i = 0; i < jobs.Count; i++)
            {
                var curveData = new CurveData();
                curveData.ModelArgs = jobs[i];

                // iterate on user-specified units (km or n miles)
                double d_step__user_units = _fullResolution ? 1 : (max - min) / _steps;
                double d__user_units = min;

                while (d__user_units <= max)
                {
                    var rtn = P528.Invoke(
                        Tools.ConvertToKm(d__user_units),
                        Tools.ConvertToMeters(jobs[i].h_1__user_units),
                        Tools.ConvertToMeters(jobs[i].h_2__user_units), 
                        jobs[i].f__mhz, 
                        jobs[i].Polarization, 
                        jobs[i].time, 
                        out P528.Result result);

                    // Ignore 'ERROR_HEIGHT_AND_DISTANCE' for visualization.  Just relates to the d__km = 0 point and will return 0 dB result
                    if (rtn != Constants.ERROR_HEIGHT_AND_DISTANCE && rtn != 0)
                        curveData.Rtn = rtn;

                    // record the result
                    curveData.d__user_units.Add(Tools.ConvertFromKm(result.d__km));
                    curveData.L_btl__db.Add(result.A__db);
                    curveData.L_fs__db.Add(result.A_fs__db);
                    curveData.PropModes.Add(result.ModeOfPropagation);

                    // iterate
                    d__user_units += d_step__user_units;

                    _worker.ReportProgress(Convert.ToInt32(100 * (i + d__user_units / max) / jobs.Count));
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
            IsExportable = true;
            IsSaveable = true;
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
            tb_ConsistencyWarning.Visibility = ((curveData.Rtn & Constants.WARNING__DFRAC_TROPO_REGION) == Constants.WARNING__DFRAC_TROPO_REGION) ? Visibility.Visible : Visibility.Collapsed;

            ResetPlotData();

            // build lines
            for (int i = 0; i < curveData.d__user_units.Count; i++)
            {
                switch (curveData.PropModes[i])
                {
                    case P528.ModeOfPropagation.LineOfSight:
                        _losSeries.Points.Add(new DataPoint(curveData.d__user_units[i], curveData.L_btl__db[i]));
                        break;
                    case P528.ModeOfPropagation.Diffraction:
                        _dfracSeries.Points.Add(new DataPoint(curveData.d__user_units[i], curveData.L_btl__db[i]));
                        break;
                    case P528.ModeOfPropagation.Troposcatter:
                        _scatSeries.Points.Add(new DataPoint(curveData.d__user_units[i], curveData.L_btl__db[i]));
                        break;
                }

                _btlSeries.Points.Add(new DataPoint(curveData.d__user_units[i], curveData.L_btl__db[i]));
                _fsSeries.Points.Add(new DataPoint(curveData.d__user_units[i], curveData.L_fs__db[i]));
            }

            // fill in the gaps between line segments to make continuous
            // checking if line series have points based on bounds of generated data
            if (_dfracSeries.Points.Any())
                _losSeries.Points.Add(_dfracSeries.Points.First());
            if (_scatSeries.Points.Any())
                _dfracSeries.Points.Add(_scatSeries.Points.First());

            // add all line series to plot, even if they don't have any data
            // for the current scenerio
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
                    Title = $"{inputControl.h_1s[j]} {GlobalState.Units}",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };

                // build line
                for (int i = 0; i < curveData.d__user_units.Count; i++)
                    series.Points.Add(new DataPoint(curveData.d__user_units[i], curveData.L_btl__db[i]));

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
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor(Tools.GetBrush(j)),
                    Title = $"{inputControl.h_2s[j]} {GlobalState.Units}",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };

                // build line
                for (int i = 0; i < curveData.d__user_units.Count; i++)
                    series.Points.Add(new DataPoint(curveData.d__user_units[i], curveData.L_btl__db[i]));

                // add to plot
                PlotModel.Series.Add(series);

                j++;
            }

            // redraw the plot
            plot.InvalidatePlot();
        }

        /// <summary>
        /// Multiple times curves job is completed
        /// </summary>
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
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = ConvertBrushToOxyColor(Tools.GetBrush(j)),
                    Title = $"{inputControl.times[j]}%",
                    MarkerType = MarkerType.None,
                    CanTrackerInterpolatePoints = false
                };

                // build line
                for (int i = 0; i < curveData.d__user_units.Count; i++)
                    series.Points.Add(new DataPoint(curveData.d__user_units[i], curveData.L_btl__db[i]));

                // add to plot
                PlotModel.Series.Add(series);

                j++;
            }

            // update free space line
            _fsSeries.Points.Clear();
            for (int i = 0; i < jobResults[0].d__user_units.Count; i++)
                _fsSeries.Points.Add(new DataPoint(jobResults[0].d__user_units[i], jobResults[0].L_fs__db[i]));
            PlotModel.Series.Add(_fsSeries);

            // redraw the plot
            plot.InvalidatePlot();
        }

        #endregion

        #region CSV Export Methods

        private void Mi_Export_Click(object sender, RoutedEventArgs e) => Export();

        private void CsvExport_SingleCurveInit()
        {
            var inputControl = grid_InputControls.Children[0] as SingleCurveInputsControl;

            // generate list of jobs - only single curve so just 1 job
            var jobs = new List<ModelArgs>()
            {
                new ModelArgs()
                {
                    h_1__user_units = Tools.ConvertToMeters(inputControl.h_1),
                    h_2__user_units = Tools.ConvertToMeters(inputControl.h_2),
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                }
            };

            // set full resolution data
            _fullResolution = true;

            ProgressMsg = Constants.EXPORT_MSG;

            // start the background worker
            _worker.RunWorkerCompleted += Worker_SingleCurveDataExportCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void CsvExport_MultipleLowTerminals()
        {
            var inputControl = grid_InputControls.Children[0] as MultipleLowHeightsInputsControl;

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            foreach (double h_1 in inputControl.h_1s)
                jobs.Add(new ModelArgs()
                {
                    h_1__user_units = h_1,
                    h_2__user_units = inputControl.h_2,
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                });

            // set full resolution data
            _fullResolution = true;

            ProgressMsg = Constants.EXPORT_MSG;

            // start the background worker
            _worker.RunWorkerCompleted += Worker_MultipleLowHeightsDataExportCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void CsvExport_MultipleHighTerminals()
        {
            var inputControl = grid_InputControls.Children[0] as MultipleHighHeightsInputsControl;

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            foreach (double h_2 in inputControl.h_2s)
                jobs.Add(new ModelArgs()
                {
                    h_1__user_units = inputControl.h_1,
                    h_2__user_units = h_2,
                    f__mhz = inputControl.f__mhz,
                    time = inputControl.time,
                    Polarization = inputControl.Polarization
                });

            // set full resolution data
            _fullResolution = true;

            ProgressMsg = Constants.EXPORT_MSG;

            // start the background worker
            _worker.RunWorkerCompleted += Worker_MultipleHighHeightsDataExportCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void CsvExport_MultipleTimePercentages()
        {
            var inputControl = grid_InputControls.Children[0] as MultipleTimeInputsControl;

            // create a list of jobs
            var jobs = new List<ModelArgs>();

            foreach (var time in inputControl.times)
                jobs.Add(new ModelArgs()
                {
                    h_1__user_units = inputControl.h_1,
                    h_2__user_units = inputControl.h_2,
                    f__mhz = inputControl.f__mhz,
                    time = time,
                    Polarization = inputControl.Polarization
                });

            // set full resolution data
            _fullResolution = true;

            ProgressMsg = Constants.EXPORT_MSG;

            // start the background worker
            _worker.RunWorkerCompleted += Worker_MultipleTimesDataExportCompleted;
            _worker.RunWorkerAsync(jobs);
        }

        private void Worker_SingleCurveDataExportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // unsubscribe to future events and reset resolution
            _worker.RunWorkerCompleted -= Worker_SingleCurveDataExportCompleted;
            _fullResolution = false;

            // check if worker finished its jobs
            if (e.Cancelled)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV file (*.csv)|*.csv";

            if (sfd.ShowDialog() != true)
                return;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = false };
            if (!exportOptionsWndw.ShowDialog().Value)
                return;

            // this is a single curve worker, so we can safely call First()
            var curveData = ((List<CurveData>)e.Result).First();

            Exporter.ExportSingleCurveData(sfd.FileName, curveData, exportOptionsWndw.IncludeModeOfPropagation,
                exportOptionsWndw.IncludeFreeSpaceLoss, exportOptionsWndw.IsRowAlignedData);

            MessageBox.Show("Export Completed");
        }

        private void Worker_MultipleLowHeightsDataExportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // unsubscribe to future events and reset resolution
            _worker.RunWorkerCompleted -= Worker_MultipleLowHeightsDataExportCompleted;
            _fullResolution = false;

            // check if worker finished its jobs
            if (e.Cancelled)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV file (*.csv)|*.csv";

            if (sfd.ShowDialog() != true)
                return;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return;

            Exporter.ExportMultipleLowHeightsData(sfd.FileName, (List<CurveData>)e.Result, exportOptionsWndw.IsRowAlignedData);

            MessageBox.Show("Export Completed");
        }

        private void Worker_MultipleHighHeightsDataExportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // unsubscribe to future events and reset resolution
            _worker.RunWorkerCompleted -= Worker_MultipleHighHeightsDataExportCompleted;
            _fullResolution = false;

            // check if worker finished its jobs
            if (e.Cancelled)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV file (*.csv)|*.csv";

            if (sfd.ShowDialog() != true)
                return;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return;

            Exporter.ExportMultipleHighHeightsData(sfd.FileName, (List<CurveData>)e.Result, exportOptionsWndw.IsRowAlignedData);

            MessageBox.Show("Export Completed");
        }

        private void Worker_MultipleTimesDataExportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // unsubscribe to future events and reset resolution
            _worker.RunWorkerCompleted -= Worker_MultipleTimesDataExportCompleted;
            _fullResolution = false;

            // check if worker finished its jobs
            if (e.Cancelled)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV file (*.csv)|*.csv";

            if (sfd.ShowDialog() != true)
                return;

            var exportOptionsWndw = new ExportOptionsWindow() { ShowMinimum = true };
            if (!exportOptionsWndw.ShowDialog().Value)
                return;

            Exporter.ExportMultipleTimesData(sfd.FileName, (List<CurveData>)e.Result, exportOptionsWndw.IsRowAlignedData);

            MessageBox.Show("Export Completed");
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
            _losSeries.IsVisible = IsModeOfPropChecked;
            _dfracSeries.IsVisible = IsModeOfPropChecked;
            _scatSeries.IsVisible = IsModeOfPropChecked;
            _btlSeries.IsVisible = !IsModeOfPropChecked;

            plot.InvalidatePlot();
        }

        /// <summary>
        /// Show the About window
        /// </summary>
        private void Mi_About_Click(object sender, RoutedEventArgs e)
            => new AboutWindow().ShowDialog();

        /// <summary>
        /// Set units to Meters
        /// </summary>
        private void Mi_Units_Meters_Click(object sender, RoutedEventArgs e)
        {
            mi_Units_Meters.IsChecked = true;
            mi_Units_Feet.IsChecked = false;

            GlobalState.Units = Units.Meters;

            SetUnits();
        }

        /// <summary>
        /// Set units to Feet
        /// </summary>
        private void Mi_Units_Feet_Click(object sender, RoutedEventArgs e)
        {
            mi_Units_Feet.IsChecked = true;
            mi_Units_Meters.IsChecked = false;

            GlobalState.Units = Units.Feet;

            SetUnits();
        }

        private void Mi_SetAxisLimits_Click(object sender, RoutedEventArgs e)
        {
            var limitsWndw = new AxisLimitsWindow()
            {
                XAxisUnit = (GlobalState.Units == Units.Meters) ? "km" : "n mile",
                XAxisMaximum = _xAxis.Maximum,
                XAxisMinimum = _xAxis.Minimum,
                YAxisMaximum = _yAxis.Maximum,
                YAxisMinimum = _yAxis.Minimum,
            };

            if (!limitsWndw.ShowDialog().Value)
                return;

            _xAxis.Maximum = limitsWndw.XAxisMaximum;
            _xAxis.Minimum = limitsWndw.XAxisMinimum;

            _yAxis.Maximum = limitsWndw.YAxisMaximum;
            _yAxis.Minimum = limitsWndw.YAxisMinimum;

            plot.InvalidatePlot();
        }

        private void Mi_ResetAxisLimits_Click(object sender, RoutedEventArgs e) => ResetPlotAxis();

        private void Mi_SaveAsImage_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new SaveFileDialog
            {
                DefaultExt = ".png",
                Filter = "Portable Network Graphics (.png)|*.png"
            };

            if (fileDialog.ShowDialog().Value)
            {
                var pngExporter = new OxyPlot.Wpf.PngExporter
                {
                    Width = 600,
                    Height = 400,
                    Background = OxyColors.White,
                };
                OxyPlot.Wpf.ExporterExtensions.ExportToFile(pngExporter, PlotModel, fileDialog.FileName);
            }
        }

        private void Mi_View_SetPlotTitle(object sender, RoutedEventArgs e)
        {
            var wndw = new PlotTitleWindow();
            if (!wndw.ShowDialog().Value)
                return;

            PlotModel.Title = wndw.PlotTitle;
            plot.InvalidatePlot();
        }

        private void Mi_View_SetLineDetails(object sender, RoutedEventArgs e)
        {
            // gather current line details
            var lineDetails = new List<LineDetails>();
            foreach (LineSeries series in PlotModel.Series)
            {
                lineDetails.Add(new LineDetails()
                {
                    Label = series.Title,
                    Thickness = series.StrokeThickness,
                    LineStyle = (UserControls.LineStyle)(int)series.LineStyle,
                    LineColor = Tools.LineColors.Keys.Single(c => c.Color.R == series.Color.R && c.Color.G == series.Color.G && c.Color.B == series.Color.B)
                });
            }

            var wndw = new ConfigureLineDetailsWindow() { LineDetails = lineDetails };
            if (!wndw.ShowDialog().Value)
                return;

            // update plot with user defined info
            for (int i = 0; i < PlotModel.Series.Count; i++)
            {
                var details = wndw.LineDetails[i];
                var series = (LineSeries)PlotModel.Series[i];

                series.Title = details.Label;
                series.LineStyle = (OxyPlot.LineStyle)(int)details.LineStyle;
                series.Color = ConvertBrushToOxyColor(details.LineColor);
                series.StrokeThickness = details.Thickness;
            }

            plot.InvalidatePlot();
        }

        #endregion

        #region Non-Menu Event Handlers

        /// <summary>
        /// Program initialization method - fired at startup
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // initialized application for Single Curve Mode
            var command = PlotModeCommand.Command;
            command.Execute(PlotMode.Single);
        }

        /// <summary>
        /// Cancel the background worker
        /// </summary>
        private void Btn_CancelWork_Click(object sender, RoutedEventArgs e)
            => _worker.CancelAsync();

        private void Btn_Render_Click(object sender, RoutedEventArgs e) => Render();

        private void XAxis_Changed(object sender, AxisChangedEventArgs e) => IsExportable = false;

        #endregion

        private void SetUnits()
        {
            if (grid_InputControls.Children.Count == 0)
                return;

            // Update text
            _xAxis.Title = "Distance " + ((GlobalState.Units == Units.Meters) ? "(km)" : "(n mile)");
            ResetPlotAxis();
        }

        private void ResetPlotAxis()
        {
            if (GlobalState.Units == Units.Meters)
                _xAxis.Maximum = Constants.XAXIS_MAX_DEFAULT__KM;
            else
                _xAxis.Maximum = Constants.XAXIS_MAX_DEFAULT__MI;

            _xAxis.Minimum = Constants.XAXIS_MIN_DEFAULT;
            _yAxis.Maximum = Constants.YAXIS_MAX_DEFAULT;
            _yAxis.Minimum = Constants.YAXIS_MIN_DEFAULT;

            plot.ResetAllAxes();
        }

        /// <summary>
        /// Clear the plot and its line series of all data
        /// </summary>
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

            plot.InvalidatePlot();
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

            // if the background worker is busy, cancel its job
            if (_worker.IsBusy)
                _worker.CancelAsync();

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
                    Export = CsvExport_SingleCurveInit;

                    userControl = new SingleCurveInputsControl();

                    IsModeOfPropEnabled = true;
                    IsModeOfPropChecked = true;
                    break;

                case PlotMode.MultipleLowTerminals:
                    Render = RenderMultipleLowHeights;
                    Export = CsvExport_MultipleLowTerminals;

                    userControl = new MultipleLowHeightsInputsControl();

                    IsModeOfPropEnabled = false;
                    IsModeOfPropChecked = false;
                    break;

                case PlotMode.MultipleHighTerminals:
                    Render = RenderMultipleHighHeights;
                    Export = CsvExport_MultipleHighTerminals;

                    userControl = new MultipleHighHeightsInputsControl();

                    IsModeOfPropEnabled = false;
                    IsModeOfPropChecked = false;
                    break;

                case PlotMode.MultipleTimes:
                    Render = RenderMultipleTimes;
                    Export = CsvExport_MultipleTimePercentages;

                    userControl = new MultipleTimeInputsControl();

                    IsModeOfPropEnabled = false;
                    IsModeOfPropChecked = false;
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
            GlobalState.Units = GlobalState.Units;
        }

        private void InitializeLineSeries()
        {
            // set up free space loss line and bind it to boolean
            _fsSeries = new LineSeries()
            {
                StrokeThickness = 1,
                MarkerSize = 0,
                LineStyle = OxyPlot.LineStyle.Dot,
                Color = ConvertBrushToOxyColor(Brushes.Black),
                Title = "Free Space",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = IsFreeSpaceLineVisible
            };

            _btlSeries = new LineSeries()
            {
                StrokeThickness = 5,
                MarkerSize = 0,
                LineStyle = OxyPlot.LineStyle.Solid,
                Color = ConvertBrushToOxyColor(Brushes.Green),
                Title = "Basic Transmission Loss",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = !IsModeOfPropChecked
            };

            _losSeries = new LineSeries()
            {
                StrokeThickness = 5,
                MarkerSize = 0,
                LineStyle = OxyPlot.LineStyle.Solid,
                Color = ConvertBrushToOxyColor(Brushes.Red),
                Title = "Line of Sight",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = IsModeOfPropChecked
            };

            _dfracSeries = new LineSeries()
            {
                StrokeThickness = 5,
                MarkerSize = 0,
                LineStyle = OxyPlot.LineStyle.Solid,
                Color = ConvertBrushToOxyColor(Brushes.Blue),
                Title = "Diffraction",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = IsModeOfPropChecked
            };

            _scatSeries = new LineSeries()
            {
                StrokeThickness = 5,
                MarkerSize = 0,
                LineStyle = OxyPlot.LineStyle.Solid,
                Color = ConvertBrushToOxyColor(Brushes.Orange),
                Title = "Troposcatter",
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false,
                IsVisible = IsModeOfPropChecked
            };
        }

        OxyColor ConvertBrushToOxyColor(Brush brush) => OxyColor.Parse(((SolidColorBrush)brush).Color.ToString());
    }
}
