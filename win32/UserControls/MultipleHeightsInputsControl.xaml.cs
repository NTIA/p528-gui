using p528_gui.Interfaces;
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
    public partial class MultipleHeightsInputsControl : UserControl, IUnitEnabled, IInputValidation
    {
        private Units _units;
        public Units Units
        {
            get { return _units; }
            set
            {
                _units = value;
                tb_t1.Text = "Terminal 1 Height " + ((_units == Units.Meters) ? "(m):" : "(ft):");
                tb_t2.Text = "Terminal 2 Heights " + ((_units == Units.Meters) ? "(m):" : "(ft):");
            }
        }

        public double H1 { get; set; }

        public List<double> H2s { get; set; } = new List<double>();

        public double FMHZ { get; set; }

        public double TIME { get; set; }

        public MultipleHeightsInputsControl()
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

            H2s.Clear();
            foreach (ListBoxItem item in lb_h2s.Items)
            {
                string h2 = item.Content.ToString();

                if (!Tools.ValidateH2(h2, _units, out double h_2))
                    return false;
                else
                {
                    if (h_1 > h_2)
                    {
                        Tools.ValidationError(tb_h1);
                        MessageBox.Show(Messages.Terminal1LessThan2Error);
                    }
                    else
                    {
                        Tools.ValidationSuccess(tb_h1);

                        H2s.Add(h_2);
                    }
                }
            }

            if (!Tools.ValidateFMHZ(tb_freq.Text, out double f__mhz))
                return Tools.ValidationError(tb_freq);
            else
            {
                Tools.ValidationSuccess(tb_freq);
                FMHZ = f__mhz;
            }

            if (!Tools.ValidateTIME(tb_time.Text, out double time))
                return Tools.ValidationError(tb_time);
            else
            {
                Tools.ValidationSuccess(tb_time);
                TIME = time / 100;
            }

            return true;
        }

        private void Btn_AddHeight_Click(object sender, RoutedEventArgs e)
        {
            var wndw = new AddHeightWindow() { Units = _units };

            if (!wndw.ShowDialog().Value)
                return;

            lb_h2s.Items.Add(new ListViewItem() { Content = wndw.H2 });
        }

        private void Lb_h2s_SelectionChanged(object sender, SelectionChangedEventArgs e) => btn_Remove.IsEnabled = (lb_h2s.SelectedItems.Count > 0);

        private void Btn_Remove_Click(object sender, RoutedEventArgs e)
        {
            var itemsToRemove = new List<ListViewItem>();

            foreach (ListViewItem item in lb_h2s.SelectedItems)
                itemsToRemove.Add(item);

            foreach (var item in itemsToRemove)
            {
                lb_h2s.Items.Remove(item);
            }
        }
    }
}
