using System;

namespace GazeFirst
{
    /// <summary>
    /// Calibration point update arguments
    /// </summary>
    public class CalibrationPointUpdateArgs : EventArgs
    {
        /// <summary>
        /// Sequence number of the calibration point (starts at 0)
        /// </summary>
        public int sequenceNumber;

        /// <summary>
        /// Target point in normalized coordinates
        /// </summary>
        public NormedPoint2d target = new NormedPoint2d();

        /// <summary>
        /// State of the calibration point
        /// </summary>
        public CalibrationPointState state;        
    }
}
