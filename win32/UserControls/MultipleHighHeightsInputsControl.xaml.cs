using ITS.Propagation;
using p528_gui.Interfaces;
using p528_gui.ValidationRules;
using p528_gui.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace p528_gui.UserControls
{
    public partial class MultipleHighHeightsInputsControl : UserControl, IUnitEnabled, INotifyPropertyChanged
    {
        #region Private Fields

        private int _errorCnt = 0;

        private Units _units;

        #endregion

        #region Public Properties

        public Units Units
        {
            get { return _units; }
            set
            {
                _units = value;
                tb_t1.Text = "Terminal 1 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
                tb_t2.Text = "Terminal 2 Heights " + ((_units == Units.Meters) ? "(m):" : "(ft):");
            }
        }

        /// <summary>
        /// Low terminal height, in user defined units
        /// </summary>
        public double h_1 { get; set; }

        /// <summary>
        /// High terminal heights, in user defined units
        /// </summary>
        public ObservableCollection<double> h_2s { get; set; } = new ObservableCollection<double>();

        /// <summary>
        /// Frequency, in MHz
        /// </summary>
        public double f__mhz { get; set; }

        /// <summary>
        /// Time percentage
        /// </summary>
        public double time { get; set; }

        /// <summary>
        /// Polarization
        /// </summary>
        public P528.Polarization Polarization { get; set; }

        /// <summary>
        /// Number of validation errors
        /// </summary>
        public int ErrorCnt
        {
            get { return _errorCnt; }
            set
            {
                _errorCnt = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public MultipleHighHeightsInputsControl()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void TextBox_Error(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
                ErrorCnt++;
            else
                ErrorCnt--;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        #region Event Handlers

        /// <summary>
        /// Prompt user for new high terminal to add
        /// </summary>
        private void Btn_AddHeight_Click(object sender, RoutedEventArgs e)
        {
            var wndw = new AddHighHeightWindow() { Units = _units };

            if (!wndw.ShowDialog().Value)
                return;

            if (h_2s.Count == 0)
            {
                ErrorCnt--;
                Validation.ClearInvalid(lb_h2s.GetBindingExpression(ListBox.ItemsSourceProperty));
            }

            h_2s.Add(wndw.H2);
        }

        /// <summary>
        /// Remove selected high height(s) from UI control
        /// </summary>
        private void Btn_Remove_Click(object sender, RoutedEventArgs e)
        {
            var itemsToRemove = new List<double>();

            foreach (double item in lb_h2s.SelectedItems)
                itemsToRemove.Add(item);

            foreach (var item in itemsToRemove)
                h_2s.Remove(item);

            if (h_2s.Count == 0)
            {
                ErrorCnt++;

                var binding = lb_h2s.GetBindingExpression(ListBox.ItemsSourceProperty);
                var error = new ValidationError(new DoubleValidation(), binding) { ErrorContent = "At least 1 high terminal height is required" };
                Validation.MarkInvalid(binding, error);
            }
        }

        /// <summary>
        /// Control if the 'Remove' button is enabled
        /// </summary>
        private void Lb_h2s_SelectionChanged(object sender, SelectionChangedEventArgs e) 
            => btn_Remove.IsEnabled = lb_h2s.SelectedItems.Count > 0;

        #endregion
    }
}
