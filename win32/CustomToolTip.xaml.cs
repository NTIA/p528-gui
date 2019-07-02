using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace p528_gui
{
    public partial class CustomToolTip : IChartTooltip
    {
        private TooltipData _data;

        public PointViewModel PointVM { get; set; }

        public Units Units { get; set; } = Units.Meters;

        public CustomToolTip()
        {
            InitializeComponent();

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TooltipData Data
        {
            get { return _data; }
            set
            {
                _data = value;

                PointVM = new PointViewModel(
                    _data.Points[0].Series.Title,
                    _data.Points[0].ChartPoint.X,
                    _data.Points[0].ChartPoint.Y,
                    _data.Points[0].Series.Stroke,
                    Units);

                OnPropertyChanged("PointVM");
            }
        }

        public TooltipSelectionMode? SelectionMode { get; set; }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
