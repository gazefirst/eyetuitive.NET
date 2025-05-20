using System;

namespace GazeFirst
{
    public class CalibrationFinishedArgs : EventArgs
    {
        /// <summary>
        /// Boolean indicating whether the calibration was successful
        /// </summary>
        public bool success = false;

        /// <summary>
        /// Percentage rating of the calibration overall (0-100)
        /// </summary>
        public int percentageRatingOverall = 0;

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

        /// <summary>
        /// Guid of this calibration and its result
        /// </summary>
        public Guid calibrationId = Guid.Empty;

        /// <summary>
        /// Timestamp (unix time) of the calibration
        /// </summary>
        public long timestamp = 0;
    }
}
