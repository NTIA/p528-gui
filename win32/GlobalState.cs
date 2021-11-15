using System;

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
