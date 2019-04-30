using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace p528_gui
{
    public class PointViewModel
    {
        public string PropMode { get; set; }
        public string DistanceKM { get; set; }
        public string LossDB { get; set; }
        public Brush Color { get; set; }

        public PointViewModel(string propMode, double d__km, double A__db, Brush color)
        {
            this.PropMode = propMode;
            this.DistanceKM = $"{Math.Round(d__km, 0).ToString("F0")} km";
            this.LossDB = $"{ Math.Round(A__db, 3).ToString("F3")} dB";
            this.Color = color;
        }
    }
}
