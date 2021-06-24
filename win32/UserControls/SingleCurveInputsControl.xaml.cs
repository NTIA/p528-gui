using p528_gui.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace p528_gui.UserControls
{
    public partial class SingleCurveInputsControl : UserControl, IUnitEnabled, INotifyPropertyChanged
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
        /// Time percentage
        /// </summary>
        public double time { get; set; }

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

        public SingleCurveInputsControl()
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
    }
}
