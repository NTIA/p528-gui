using p528_gui.Windows;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace p528_gui.UserControls
{
    public partial class MultipleTimeInputsControl : UserControl
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

        public double H1 { get; set; }

        public double H2 { get; set; }

        public double FMHZ { get; set; }

        public List<double> TIMEs { get; set; } = new List<double>();

        public MultipleTimeInputsControl()
        {
            InitializeComponent();
        }

        public bool AreInputsValid()
        {
            if (!Tools.ValidateH1(tb_h1.Text, _units, out double h_1))
                return Tools.ValidationError(tb_h1);
            else
            {
                Tools.ValidationSuccess(tb_h1);
                H1 = h_1;
            }

            if (!Tools.ValidateH2(tb_h2.Text, _units, out double h_2))
                return Tools.ValidationError(tb_h2);
            else
            {
                Tools.ValidationSuccess(tb_h2);
                H2 = h_2;
            }

            if (H1 > H2)
            {
                Tools.ValidationError(tb_h1);
                Tools.ValidationError(tb_h2);
                MessageBox.Show(Messages.Terminal1LessThan2Error);
            }
            else
            {
                Tools.ValidationSuccess(tb_h1);
                Tools.ValidationSuccess(tb_h2);
            }

            if (!Tools.ValidateFMHZ(tb_freq.Text, out double f__mhz))
                return Tools.ValidationError(tb_freq);
            else
            {
                Tools.ValidationSuccess(tb_freq);
                FMHZ = f__mhz;
            }

            TIMEs.Clear();
            foreach (ListBoxItem item in lb_times.Items)
            {
                string TIME = item.Content.ToString();

                if (!Tools.ValidateTIME(TIME, out double time))
                    return false;
                else
                    TIMEs.Add(time / 100);
            }

            return true;
        }

        private void Lb_times_SelectionChanged(object sender, SelectionChangedEventArgs e) => btn_Remove.IsEnabled = (lb_times.SelectedItems.Count > 0);

        private void Btn_Add_Click(object sender, RoutedEventArgs e)
        {
            var wndw = new AddTimeWindow();

            if (!wndw.ShowDialog().Value)
                return;

            lb_times.Items.Add(new ListViewItem() { Content = wndw.TIME });
        }

        private void Btn_Remove_Click(object sender, RoutedEventArgs e)
        {
            var itemsToRemove = new List<ListViewItem>();

            foreach (ListViewItem item in lb_times.SelectedItems)
                itemsToRemove.Add(item);

            foreach (var item in itemsToRemove)
            {
                lb_times.Items.Remove(item);
            }
        }
    }
}
