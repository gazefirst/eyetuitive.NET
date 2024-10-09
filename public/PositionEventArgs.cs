using System;

namespace GazeFirst
{
    /// <summary>
    /// Position event arguments
    /// </summary>
    public class PositionEventArgs : EventArgs
    {
        /// <summary>
        /// Depth in millimeters (Distance from the camera)
        /// </summary>
        public double depthInMM;

        /// <summary>
        /// Left eye position
        /// </summary>
        public NormedPoint2d leftEyePos = new NormedPoint2d();

        /// <summary>
        /// Right eye position
        /// </summary>
        public NormedPoint2d rightEyePos = new NormedPoint2d();

        /// <summary>
        /// Is the left eye open
        /// </summary>
        public bool isLeftEyeOpen = true;

        /// <summary>
        /// Is the right eye open
        /// </summary>
        public bool isRightEyeOpen = true;

        /// <summary>
        /// True if gaze is paused (user needs to look into the eyetracker camera to unpause)
        /// </summary>
        public bool gazeIsPaused = false;
    }
}
