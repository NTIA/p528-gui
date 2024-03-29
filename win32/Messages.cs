﻿namespace P528GUI
{
    static class Messages
    {
        // warning text
        public const string ModelConsistencyWarning = "Note: The P.528 model has returned a warning that the transition between diffraction and troposcatter might not be physically consistent.  Care should be taken when using the results.";

        public const string Terminal1HeightWarining = "Terminal 1 height is above upper limit specified in Recommendation text.";

        public const string Terminal2HeightWarining = "Terminal 2 height is above upper limit specified in Recommendation text.";

        public const string Terminal1LessThan2Error = "Terminal 1 must be less than the height of Terminal 2";

        public const string FrequencyRangeError = "The frequency must be between 100 MHz and 30 000 MHz, inclusive";

        public const string TimeRangeError = "The time percentage must be between 1 and 99, inclusive.";
    }
}
