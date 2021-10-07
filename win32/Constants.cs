﻿namespace P528GUI
{
    static class Constants
    {
        // unit conversion factors
        public const double METER_PER_FOOT = 0.3048;
        public const double KM_PER_NAUTICAL_MILE = 1.852;

        public const int ERROR_HEIGHT_AND_DISTANCE = 10;

        public const int WARNING__DFRAC_TROPO_REGION = 0xFF1;

        public const string RENDER_MSG = "Generating Plot Data...";

        public const string EXPORT_MSG = "Regenerating Data at Higher Resolution for Export...";

        // plot default values
        public const double XAXIS_MIN_DEFAULT = 0;
        public const double XAXIS_MAX_DEFAULT__KM = 1800;
        public const double XAXIS_MAX_DEFAULT__MI = 970;
        public const double YAXIS_MAX_DEFAULT = 300;
        public const double YAXIS_MIN_DEFAULT = 0;
    }
}
