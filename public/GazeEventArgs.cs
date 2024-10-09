using System;

namespace GazeFirst
{
    /// <summary>
    /// Gaze event arguments
    /// </summary>
    public class GazeEventArgs : EventArgs
    {
        /// <summary>
        /// Timestamp of the gaze data in microseconds since the start of the tracking
        /// </summary>
        public long timestamp;

        /// <summary>
        /// Normed gaze point combined from both eyes or from the only eye if the other is not available / disabled
        /// </summary>
        public NormedPoint2d gazePoint = new NormedPoint2d();

        /// <summary>
        /// Normed gaze point of the left eye
        /// </summary>
        public NormedPoint2d leftEye = new NormedPoint2d();

        /// <summary>
        /// Normed gaze point of the right eye
        /// </summary>
        public NormedPoint2d rightEye = new NormedPoint2d();

        /// <summary>
        /// Fixation at the gaze point (true) or not (false)
        /// </summary>
        public bool fixation;

        /// <summary>
        /// User presence: True if user is present (normally yes). 
        /// Device will send false with empty data once till a user gets redetected.
        /// </summary>
        public bool userPresent;

        /// <summary>
        /// Left eye open: True if left eye is open / false if closed or user is not present
        /// </summary>
        public bool leftEyeOpen;

        /// <summary>
        /// Right eye open: True if right eye is open / false if closed or user is not present
        /// </summary>
        public bool rightEyeOpen;
    }
}
