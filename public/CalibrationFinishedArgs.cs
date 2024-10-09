using System;

namespace GazeFirst
{
    public class CalibrationFinishedArgs : EventArgs
    {
        /// <summary>
        /// Boolean indicating whether the calibration was successful
        /// </summary>
        public bool success;

        /// <summary>
        /// Percentage rating of the calibration overall (0-100)
        /// </summary>
        public int percentageRatingOverall;

        /// <summary>
        /// Percentage rating of the calibration per point (0-100)
        /// </summary>
        public int[] percentageRatingPerPoint = Array.Empty<int>();

        /// <summary>
        /// Percentage rating of the calibration per point on the left eye (0-100)
        /// </summary>
        public int[] percentageRatingPerPointLeft = Array.Empty<int>();

        /// <summary>
        /// Percentage rating of the calibration per point on the right eye (0-100)
        /// </summary>
        public int[] percentageRatingPerPointRight = Array.Empty<int>();

        /// <summary>
        /// Can the calibration be improved
        /// </summary>
        public bool canImprove = false;
    }
}
