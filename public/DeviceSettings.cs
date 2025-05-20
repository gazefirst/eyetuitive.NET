using System;

namespace GazeFirst
{
    public class DeviceSettingsArgs
    {
        /// <summary>
        /// Enable or disable eyetracker native gaze data (e.g. on HID / USB)
        /// </summary>
        public bool pauseNative = false;

        /// <summary>
        /// Enable or disable eyetracker API gaze data
        /// </summary>
        public bool pauseAPIGaze = false;

        /// <summary>
        /// Enable or disable eyetracker pause by looking into the eyetracker camera (center of eyetuitive screen)
        /// </summary>
        public bool enablePauseByGaze = false;

        /// <summary>
        /// Frame rate of device
        /// </summary>
        public FrameRate Framerate = FrameRate.FPS30;
    }
}
