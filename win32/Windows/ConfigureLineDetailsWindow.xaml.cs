using P528GUI.UserControls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace P528GUI.Windows
{
    public class LineDetails
    {
        public string Label { get; set; }
        public SolidColorBrush LineColor { get; set; }
        public LineStyle LineStyle { get; set; }
        public double Thickness { get; set; }
    }

    public partial class ConfigureLineDetailsWindow : Window, INotifyPropertyChanged
    {
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

        private List<LineDetails> _lineDetails = new List<LineDetails>();
        public List<LineDetails> LineDetails
        {
            get { return _lineDetails; }
            set
            {
                _lineDetails.Clear();
                _lineDetails.AddRange(value);
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigureLineDetailsWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

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

        private void TextBox_Error(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
                ErrorCnt++;
            else
                ErrorCnt--;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
