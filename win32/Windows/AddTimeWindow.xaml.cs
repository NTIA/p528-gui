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
    public partial class AddTimeWindow : Window
    {
        public double TIME { get; set; }

        public AddTimeWindow()
        {
            InitializeComponent();
        }

        private void Btn_Accept_Click(object sender, RoutedEventArgs e)
        {
            if (!Tools.ValidateTIME(tb_time.Text, out double time))
                Tools.ValidationError(tb_time);
            else
            {
                Tools.ValidationSuccess(tb_time);
                TIME = time;

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
