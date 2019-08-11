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

namespace p528_gui.Windows
{
    public partial class AddHeightWindow : Window
    {
        public Units Units { get; set; }

        public double H2 { get; set; }

        public AddHeightWindow()
        {
            InitializeComponent();
        }

        private void Btn_Accept_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(tb_h2.Text) ||
                !Double.TryParse(tb_h2.Text, out double h2) ||
                h2 < Tools.ConvertMetersToSpecifiedUnits(1.5, Units))
            {
                Tools.ValidationError(tb_h2);
                MessageBox.Show("Terminal 2 must be at least " + ((Units == Units.Meters) ? "1.5 meters" : "5 feet"));
            }
            else
            {
                Tools.ValidationSuccess(tb_h2);
                H2 = h2;

                this.DialogResult = true;
                this.Close();
            }
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
