using ITS.Propagation;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace p528_gui.UserControls
{
    public partial class SingleCurveInputsControl : UserControl, INotifyPropertyChanged
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

        public SingleCurveInputsControl()
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
            foreach (var child in grid_Terminals.Children)
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) => grid_Terminals?.BindingGroup.CommitEdit();
    }
}
