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
    public partial class AddHighHeightWindow : Window
    {
        private Units _units;
        public Units Units
        {
            get { return _units; }
            set
            {
                _units = value;
                tb_t2.Text = "Terminal 2 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
            }
        }

        public double H2 { get; set; }

        public AddHighHeightWindow()
        {
            InitializeComponent();
        }

        private void Btn_Accept_Click(object sender, RoutedEventArgs e)
        {
            if (!Tools.ValidateH2(tb_h2.Text, Units, out double h2))
                Tools.ValidationError(tb_h2);
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
