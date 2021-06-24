using p528_gui.Interfaces;
using p528_gui.ValidationRules;
using p528_gui.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace p528_gui.UserControls
{
    public partial class MultipleLowHeightsInputsControl : UserControl, IUnitEnabled, INotifyPropertyChanged
    {
        private Units _units;
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

        private int _errorCnt = 0;

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

        public event PropertyChangedEventHandler PropertyChanged;

        public MultipleLowHeightsInputsControl()
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
        /// Prompt user for new low terminal to add
        /// </summary>
        private void Btn_AddHeight_Click(object sender, RoutedEventArgs e)
        {
            var wndw = new AddLowHeightWindow() { Units = _units };

            if (!wndw.ShowDialog().Value)
                return;

            if (h_1s.Count == 0)
            {
                ErrorCnt--;
                Validation.ClearInvalid(lb_h1s.GetBindingExpression(ListBox.ItemsSourceProperty));
            }

            h_1s.Add(wndw.H1);
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
        private void Lb_h1s_SelectionChanged(object sender, SelectionChangedEventArgs e) => btn_Remove.IsEnabled = (lb_h1s.SelectedItems.Count > 0);

        #endregion
    }
}
