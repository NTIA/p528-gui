using P528GUI.ValidationRules;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace P528GUI.Windows
{
    public partial class AddLowHeightWindow : Window, INotifyPropertyChanged
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

        /// <summary>
        /// High terminal height, in user defined units
        /// </summary>
        public double? h_2 { get; set; }

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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!h_2.HasValue)
                return;

            var tb = sender as TextBox;

            if (!Double.TryParse(tb.Text, out double value))
                return;

            if (value > h_2)
            {
                if (!_invalidTerminalRelationship)
                {
                    ErrorCnt++;
                    _invalidTerminalRelationship = true;
                }

                var binding = tb.GetBindingExpression(TextBox.TextProperty);
                var error = new ValidationError(new TerminalRelationshipValidation(), binding) { ErrorContent = "Terminal 1 must be less than or equal to Terminal 2" };
                Validation.MarkInvalid(binding, error);
            }
            else if (value <= h_2 && _invalidTerminalRelationship)
            {
                ErrorCnt--;
                _invalidTerminalRelationship = false;
                Validation.ClearInvalid(tb.GetBindingExpression(TextBox.TextProperty));
            }
        }
    }
}
