using ITS.Propagation;
using p528_gui.ValidationRules;
using p528_gui.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace p528_gui.UserControls
{
    public partial class MultipleLowHeightsInputsControl : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private int _errorCnt = 0;

        #endregion

        #region Public Properties

        /// <summary>
        /// Low terminal heights, in user defined units
        /// </summary>
        public ObservableCollection<double> h_1s { get; set; } = new ObservableCollection<double>() { 5 };

        /// <summary>
        /// High terminal height, in user defined units
        /// </summary>
        public double h_2 { get; set; }

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

        public MultipleLowHeightsInputsControl()
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
            tb_h2.GetBindingExpression(TextBox.TextProperty).UpdateSource();

            // Manually validation terminal heights in ListBox, removing invalid ones
            var terminalValidationRule = new TerminalHeightValidation();
            var removeIndexes = new List<int>();
            for (int i = 0; i < h_1s.Count; i++)
            {
                var result = terminalValidationRule.Validate(h_1s[i], null);

                if (!result.IsValid)
                    removeIndexes.Add(i);
            }
            if (removeIndexes.Count > 0)
            {
                MessageBox.Show("Some low terminal heights are invalid for the current units.  These will be removed.");

                for (int i = removeIndexes.Count - 1; i >= 0; i--)
                    h_1s.RemoveAt(removeIndexes[i]);
            }
            Btn_Remove_Click(null, null);   // kicking a validation check of count
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
        /// Prompt user for new low terminal to add
        /// </summary>
        private void Btn_AddHeight_Click(object sender, RoutedEventArgs e)
        {
            var wndw = new AddLowHeightWindow();

            if (!wndw.ShowDialog().Value)
                return;

            if (h_1s.Count == 0)
            {
                ErrorCnt--;
                Validation.ClearInvalid(lb_h1s.GetBindingExpression(ListBox.ItemsSourceProperty));
            }

            h_1s.Add(wndw.h_1);
        }

        /// <summary>
        /// Remove selected low height(s) from UI control
        /// </summary>
        private void Btn_Remove_Click(object sender, RoutedEventArgs e)
        {
            var itemsToRemove = new List<double>();

            foreach (double item in lb_h1s.SelectedItems)
                itemsToRemove.Add(item);

            foreach (var item in itemsToRemove)
                h_1s.Remove(item);

            if (h_1s.Count == 0)
            {
                ErrorCnt++;

                var binding = lb_h1s.GetBindingExpression(ListBox.ItemsSourceProperty);
                var error = new ValidationError(new DoubleValidation(), binding) { ErrorContent = "At least 1 low terminal height is required" };
                Validation.MarkInvalid(binding, error);
            }
        }

        /// <summary>
        /// Control if the 'Remove' button is enabled
        /// </summary>
        private void Lb_h1s_SelectionChanged(object sender, SelectionChangedEventArgs e) 
            => btn_Remove.IsEnabled = lb_h1s.SelectedItems.Count > 0;

        #endregion
    }
}
