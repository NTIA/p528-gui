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
    public partial class MultipleHighHeightsInputsControl : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private int _errorCnt = 0;

        private bool _invalidTerminalRelationship = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Low terminal height, in user defined units
        /// </summary>
        public double h_1 { get; set; }

        /// <summary>
        /// High terminal heights, in user defined units
        /// </summary>
        public ObservableCollection<double> h_2s { get; set; } = new ObservableCollection<double>() { 1000 };

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

            GlobalState.UnitsChanged += GlobalState_UnitsChanged;

            DataContext = this;
        }

        private void GlobalState_UnitsChanged(object sender, EventArgs e)
        {
            tb_t1.Text = "Terminal 1 Height " + ((GlobalState.Units == Units.Meters) ? "(m):" : "(ft):");
            tb_t2.Text = "Terminal 2 Height " + ((GlobalState.Units == Units.Meters) ? "(m):" : "(ft):");

            // Need to manually force validation since it only triggers during text updates
            tb_h1.GetBindingExpression(TextBox.TextProperty).UpdateSource();

            // Manually validation terminal heights in ListBox, removing invalid ones
            var terminalValidationRule = new TerminalHeightValidation();
            var removeIndexes = new List<int>();
            for (int i = 0; i < h_2s.Count; i++)
            {
                var result = terminalValidationRule.Validate(h_2s[i], null);

                if (!result.IsValid)
                    removeIndexes.Add(i);
            }
            if (removeIndexes.Count > 0)
            {
                MessageBox.Show("Some high terminal heights are invalid for the current units.  These will be removed.");

                for (int i = removeIndexes.Count - 1; i >= 0; i--)
                    h_2s.RemoveAt(removeIndexes[i]);
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
        /// Prompt user for new high terminal to add
        /// </summary>
        private void Btn_AddHeight_Click(object sender, RoutedEventArgs e)
        {
            var wndw = new AddHighHeightWindow() { h_1 = h_1 };

            if (!wndw.ShowDialog().Value)
                return;

            if (h_2s.Count == 0)
            {
                ErrorCnt--;
                Validation.ClearInvalid(lb_h2s.GetBindingExpression(ListBox.ItemsSourceProperty));
            }

            h_2s.Add(wndw.h_2);
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

        private void tb_h1_TextChanged(object sender, TextChangedEventArgs e)
        {
            // check if terminal height is less than any in listbox
            if (!Double.TryParse(tb_h1.Text, out double h1))
                return;

            int cnt = 0;
            foreach (double h2 in h_2s)
            {
                if (h2 < h1)
                {
                    if (!_invalidTerminalRelationship)
                    {
                        ErrorCnt++;
                        _invalidTerminalRelationship = true;
                    }

                    var binding = tb_h1.GetBindingExpression(TextBox.TextProperty);
                    var error = new ValidationError(new TerminalRelationshipValidation(), binding) { ErrorContent = "Terminal 1 must be less than or equal to Terminal 2" };
                    Validation.MarkInvalid(binding, error);
                }
                else
                    cnt++;
            }
            if (cnt == h_2s.Count && _invalidTerminalRelationship)
            {
                ErrorCnt--;
                _invalidTerminalRelationship = false;
            }
        }
    }
}
