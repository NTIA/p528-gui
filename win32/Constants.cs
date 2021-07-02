using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p528_gui
{
    static class Constants
    {
        // unit conversion factors
        public const double METER_PER_FOOT = 0.3048;
        public const double KM_PER_NAUTICAL_MILE = 1.852;

        public const int ERROR_HEIGHT_AND_DISTANCE = 10;

        public const int WARNING__DFRAC_TROPO_REGION = 0xFF1;

        public const string RENDER__MSG = "Generating Plot Data...";

        public const string EXPORT_MSG = "Regenerating Data at Higher Resolution for Export...";
    }
}
