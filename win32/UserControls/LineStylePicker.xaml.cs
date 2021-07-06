using System.Windows;
using System.Windows.Controls;

namespace P528GUI.UserControls
{
    // Only support a select few of the OxyPlot.LineStyle enum - but using the same
    // int values so they can be cast directly
    public enum LineStyle : int
    {
        Solid = 0,
        Dash = 1,
        Dot = 2,
        DashDot = 3,
        DashDashDot = 4
    }

    public partial class LineStylePicker : UserControl
    {
        public LineStyle LineStyle
        {
            get { return (LineStyle)GetValue(LineStyleProperty); }
            set { SetValue(LineStyleProperty, value); }
        }

        public LineStylePicker()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register(
                "LineStyle",
                typeof(LineStyle),
                typeof(LineStylePicker),
                new UIPropertyMetadata(null)
                );
    }
}
