using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p528_gui
{
    static class Messages
    {
        // warning text
        public const string ModelConsistencyWarning = "Note: The P.528 model has returned a warning that the transition between diffraction and troposcatter might not be physically consistent.  Care should be taken when using the results.";
        public const string LowFrequencyWarning = "Note: The entered frequency is less than the lower limit specified in P.528-4.  Care should be taken when using the results.";

        public const string TerminalHeightWarning = "Note: Although valid, the entered value is above the reference atmosphere which stops at 475 km above sea level";

        public const string Terminal1LessThan2Error = "Terminal 1 must be less than the height of Terminal 2";

        public const string FrequencyRangeError = "The frequency must be between 100 MHz and 15 500 MHz, inclusive";

        public const string TimeRangeError = "The time percentage must be between 1 and 99, inclusive.";
    }
}
