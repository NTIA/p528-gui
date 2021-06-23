using p528_gui.Interfaces;
using System;
using System.Collections.Generic;
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
    public partial class SingleCurveInputsControl : UserControl, IUnitEnabled, INotifyPropertyChanged
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
        /// Time percentage
        /// </summary>
        public double time { get; set; }

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

        public SingleCurveInputsControl()
        {
            InitializeComponent();

            img_t1.ToolTip = Messages.TerminalHeightWarning;
            img_t2.ToolTip = Messages.TerminalHeightWarning;

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
