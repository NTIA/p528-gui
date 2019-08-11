using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace p528_gui
{
    public partial class ExportOptionsWindow : Window
    {
        public bool IsRowAlignedData { get; private set; }

        public bool IncludeFreeSpaceLoss { get; private set; }

        public bool IncludeModeOfPropagation { get; private set; }

        public ExportOptionsWindow()
        {
            InitializeComponent();
        }

        private void Btn_Export_Click(object sender, RoutedEventArgs e)
        {
            IsRowAlignedData = rb_RowAlignedData.IsChecked.Value;
            IncludeFreeSpaceLoss = cb_FreeSpaceLoss.IsChecked.Value;
            IncludeModeOfPropagation = cb_ModeOfPropagation.IsChecked.Value;

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
