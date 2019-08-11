using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace p528_gui
{
    static class Tools
    {
        static public void ValidationError(TextBox tb)
        {
            tb.Background = Brushes.LightPink;
        }

        static public void ValidationSuccess(TextBox tb)
        {
            tb.Background = Brushes.White;
        }
    }
}
