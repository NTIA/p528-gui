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
    public partial class MultipleTimeInputsControl : UserControl, INotifyPropertyChanged
    {
        private Units _units;
        public Units Units
        {
            get { return _units; }
            set
            {
                _units = value;
                tb_t1.Text = "Terminal 1 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
                tb_t2.Text = "Terminal 2 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
            }
        }

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

        private int _errorCnt = 0;
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

        public MultipleTimeInputsControl()
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

            times.Add(wndw.TIME);
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
        private void Lb_times_SelectionChanged(object sender, SelectionChangedEventArgs e) => btn_Remove.IsEnabled = (lb_times.SelectedItems.Count > 0);

        #endregion
    }
}
