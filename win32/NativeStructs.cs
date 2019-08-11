using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace p528_gui
{
    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct CResult
    {
        public int propagation_mode;

        public double d__km;
        public double A__db;
        public double A_fs__db;
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct Terminal
    {
        // Heights
        public double h_r__km;             // Real terminal height
        public double h_e__km;             // Effective terminal height
        public double h__km;               // Terminal height used in model
        public double delta_h__km;         //

        // Distances
        public double d_r__km;             // Ray traced horizon distance
        public double d__km;               // Horizon distance used in model

        // Angles
        public double phi__rad;            // Central angle between the terminal and its smooth earth horizon
        public double theta__rad;          // Incident angle of the grazing ray at the terminal
    };

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct LineOfSightParams
    {
        // Heights
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public double[] z__km;

        // Distances
        public double d__km;               // Path distance between terminals
        public double r_0__km;             // Direct ray length
        public double r_12__km;            // Indirect ray length
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public double[] D__km;

        // Angles
        public double theta_h1;
        public double theta_h2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public double[] theta;

        // Misc
        public double a_a__km;             // Adjusted earth radius
        public double delta_r;             // Ray length path difference
        public double A_LOS__db;           // Loss due to LOS path
    };

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct Path
    {
        // Distances
        public double d_ML__km;            // Maximum line of sight distance
        public double d_0__km;             //
        public double d_d__km;             // Distance where smooth earth diffraction is 0 dB

        // Earth
        public double a_e__km;             // Effective earth radius
    };

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct TroposcatterParams
    {
        // Distances
        public double d_s__km;             // Scattering distance 
        public double d_z__km;             // Half the scattering distance

        // Heights
        public double h_v__km;             // Height of the common volume cross-over point

        // Angles
        public double theta_s;             // Scattering angle
        public double theta_A;             // cross-over angle

        // Losses
        public double A_s__db;             // Troposcattter Loss
        public double A_s_prev__db;        // Troposcatter Loss of Previous Test Point

        // Misc
        public double M_s;                 // Troposcatter Line Slope
    };
}
