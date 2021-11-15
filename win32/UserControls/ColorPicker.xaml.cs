using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace P528GUI.UserControls
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
