using System;

namespace GazeFirst
{
    public class UserSettingsArgs
    {
        /// <summary>
        /// Smoothing value for the gaze data (1 to 10, 1 is less smoothing, 10 is max smoothing)
        /// </summary>
        public int Smoothing = 1;

        /// <summary>
        /// Left eye only: If true, only left eye data is used
        /// </summary>
        public bool LeftEyeOnly = false;

        /// <summary>
        /// Right eye only: If true, only right eye data is used
        /// </summary>
        public bool RightEyeOnly = false;
    }
}
