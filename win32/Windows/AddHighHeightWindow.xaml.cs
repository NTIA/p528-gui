using p528_gui.ValidationRules;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace p528_gui.Windows
{
    public partial class AddHighHeightWindow : Window, INotifyPropertyChanged
    {
        #region Private Fields

        private int _errorCnt = 0;

        private bool _invalidTerminalRelationship = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Low terminal height, in user define units
        /// </summary>
        public double? h_1 { get; set; }

        /// <summary>
        /// High terminal height, in user define units
        /// </summary>
        public double h_2 { get; set; }

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

        public AddHighHeightWindow()
        {
            InitializeComponent();

            tb_t2.Text = "Terminal 2 Height " + ((GlobalState.Units == Units.Meters) ? "(m):" : "(ft):");

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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!h_1.HasValue)
                return;

            var tb = sender as TextBox;

            if (!Double.TryParse(tb.Text, out double value))
                return;

            if (value < h_1)
            {
                if (!_invalidTerminalRelationship)
                {
                    ErrorCnt++;
                    _invalidTerminalRelationship = true;
                }

                var binding = tb.GetBindingExpression(TextBox.TextProperty);
                var error = new ValidationError(new TerminalRelationshipValidation(), binding) { ErrorContent = "Terminal 2 must be greater than or equal to Terminal 1" };
                Validation.MarkInvalid(binding, error);
            }
            else if (value >= h_1 && _invalidTerminalRelationship)
            {
                ErrorCnt--;
                _invalidTerminalRelationship = false;
                Validation.ClearInvalid(tb.GetBindingExpression(TextBox.TextProperty));
            }
        }

        #endregion
    }
}
