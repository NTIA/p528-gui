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
    public partial class ColorPicker : UserControl
    {
        public ColorPicker()
        {
            InitializeComponent();

            cb_Colors.ItemsSource = Tools.LineColors.Keys.ToList();
        }

        public SolidColorBrush LineColor
        {
            get { return (SolidColorBrush)GetValue(LineColorProperty); }
            set { SetValue(LineColorProperty, value); }
        }

        public static readonly DependencyProperty LineColorProperty =
            DependencyProperty.Register(
                "LineColor",
                typeof(SolidColorBrush),
                typeof(ColorPicker),
                new UIPropertyMetadata(null)
                );
    }
}
