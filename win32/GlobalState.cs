using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace P528GUI
{
    public static class GlobalState
    {
        static public event EventHandler<EventArgs> UnitsChanged;

        private static Units _units = Units.Meters;

        public static Units Units
        {
            get { return _units; }
            set
            {
                _units = value;
                UnitsChanged?.Invoke(null, new EventArgs());
            }
        }
    }
}
