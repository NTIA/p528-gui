using ITS.Propagation;
using System.Collections.Generic;

namespace P528GUI
{
    /// <summary>
    /// Information on a P.528 curve
    /// </summary>
    class CurveData
    {
        /// <summary>
        /// Model input parameters
        /// </summary>
        public ModelArgs ModelArgs { get; set; }

        /// <summary>
        /// Model return code
        /// </summary>
        public int Rtn { get; set; } = 0;

        /// <summary>
        /// Model warning flags
        /// </summary>
        public int Warn { get; set; } = 0;

        /// <summary>
        /// Distances, in user defined units
        /// </summary>
        public List<double> d__user_units { get; set; } = new List<double>();

        /// <summary>
        /// Free space basic transmission losses, in dB
        /// </summary>
        public List<double> L_fs__db { get; set; } = new List<double>();

        /// <summary>
        /// Basic transmission losses, in dB
        /// </summary>
        public List<double> L_btl__db { get; set; } = new List<double>();

        /// <summary>
        /// Modes of propagation
        /// </summary>
        public List<P528.ModeOfPropagation> PropModes { get; set; } = new List<P528.ModeOfPropagation>();
    }
}
