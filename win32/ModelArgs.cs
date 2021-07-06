using ITS.Propagation;

namespace P528GUI
{
    /// <summary>
    /// Arguments for P.528 curve generation
    /// </summary>
    class ModelArgs
    {
        /// <summary>
        /// Height of the low terminal, in user defined units
        /// </summary>
        public double h_1__user_units { get; set; }

        /// <summary>
        /// Height of the high terminal, in user define units
        /// </summary>
        public double h_2__user_units { get; set; }

        /// <summary>
        /// Time percentage
        /// </summary>
        public double time { get; set; }

        /// <summary>
        /// Polarization
        /// </summary>
        public P528.Polarization Polarization { get; set; }

        /// <summary>
        /// Frequency, in MHz
        /// </summary>
        public double f__mhz { get; set; }
    }
}
