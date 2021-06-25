using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace p528_gui.Windows
{
    public partial class AddLowHeightWindow : Window, INotifyPropertyChanged
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

        public AddLowHeightWindow()
        {
            InitializeComponent();

            tb_t1.Text = "Terminal 1 Height " + ((GlobalState.Units == Units.Meters) ? "(m):" : "(ft):");

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

        private void Btn_Accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
