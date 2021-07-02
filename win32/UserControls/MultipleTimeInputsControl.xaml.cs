using ITS.Propagation;
using P528GUI.ValidationRules;
using P528GUI.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace P528GUI.UserControls
{
    public partial class MultipleTimeInputsControl : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private int _errorCnt = 0;

        #endregion

        #region Public Properties

        /// <summary>
        /// Low terminal height, in user defined units
        /// </summary>
        public double h_1 { get; set; }

        /// <summary>
        /// High terminal height, in user defined units
        /// </summary>
        public double h_2 { get; set; }

        /// <summary>
        /// Frequency, in MHz
        /// </summary>
        public double f__mhz { get; set; }

        /// <summary>
        /// Time percentages
        /// </summary>
        public ObservableCollection<double> times { get; set; } = new ObservableCollection<double>() { 50 };

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

        public MultipleTimeInputsControl()
        {
            InitializeComponent();

            GlobalState.UnitsChanged += GlobalState_UnitsChanged;

            DataContext = this;
        }

        private void GlobalState_UnitsChanged(object sender, EventArgs e)
        {
            tb_t1.Text = "Terminal 1 Height " + ((GlobalState.Units == Units.Meters) ? "(m):" : "(ft):");
            tb_t2.Text = "Terminal 2 Height " + ((GlobalState.Units == Units.Meters) ? "(m):" : "(ft):");

            // Need to manually force validation since it only triggers during text updates
            foreach (var child in grid_Main.Children)
                if (child is TextBox)
                    (child as TextBox).GetBindingExpression(TextBox.TextProperty).UpdateSource();
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
        /// Prompt user for new time to add
        /// </summary>
        private void Btn_Add_Click(object sender, RoutedEventArgs e)
        {
            var wndw = new AddTimeWindow();

            if (!wndw.ShowDialog().Value)
                return;

            if (times.Count == 0)
            {
                ErrorCnt--;
                Validation.ClearInvalid(lb_times.GetBindingExpression(ListBox.ItemsSourceProperty));
            }

            times.Add(wndw.time);
        }

        /// <summary>
        /// Remove selected time(s) from UI control
        /// </summary>
        private void Btn_Remove_Click(object sender, RoutedEventArgs e)
        {
            var itemsToRemove = new List<double>();

            foreach (double item in lb_times.SelectedItems)
                itemsToRemove.Add(item);

            foreach (var item in itemsToRemove)
                times.Remove(item);

            if (times.Count == 0)
            {
                ErrorCnt++;

                var binding = lb_times.GetBindingExpression(ListBox.ItemsSourceProperty);
                var error = new ValidationError(new DoubleValidation(), binding) { ErrorContent = "At least 1 time percentage is required" };
                Validation.MarkInvalid(binding, error);
            }
        }

        /// <summary>
        /// Control if the 'Remove' button is enabled
        /// </summary>
        private void Lb_times_SelectionChanged(object sender, SelectionChangedEventArgs e) => 
            btn_Remove.IsEnabled = lb_times.SelectedItems.Count > 0;

        #endregion

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) => grid_Terminals?.BindingGroup.CommitEdit();
    }
}
