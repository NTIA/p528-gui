using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace P528GUI.Windows
{
    public partial class AxisLimitsWindow : Window, INotifyPropertyChanged
    {
        private double _xAxisMinimum = 0;
        public double XAxisMinimum
        {
            get { return _xAxisMinimum; }
            set
            {
                _xAxisMinimum = value;
                OnPropertyChanged();
            }
        }

        private double _xAxixMaximum = 1800;
        public double XAxisMaximum
        {
            get { return _xAxixMaximum; }
            set
            {
                _xAxixMaximum = value;
                OnPropertyChanged();
            }
        }

        private double _yAxisMinimum = 0;
        public double YAxisMinimum
        {
            get { return _yAxisMinimum; }
            set
            {
                _yAxisMinimum = value;
                OnPropertyChanged();
            }
        }

        private double _yAxisMaximum = 300;
        public double YAxisMaximum
        {
            get { return _yAxisMaximum; }
            set
            {
                _yAxisMaximum = value;
                OnPropertyChanged();
            }
        }

        public string XAxisUnit
        {
            set
            {
                tb_xAxisMinimumUnits.Text = value;
                tb_xAxisMaximumUnits.Text = value;
            }
        }

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

        public AxisLimitsWindow()
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

        private void btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void XAxisTextBox_TextChanged(object sender, TextChangedEventArgs e) => grid_XAxis?.BindingGroup.CommitEdit();

        private void YAxisTextBox_TextChanged(object sender, TextChangedEventArgs e) => grid_YAxis?.BindingGroup.CommitEdit();
    }
}
