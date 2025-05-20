using System;

namespace GazeFirst
{
    public class FrameArgs : EventArgs
    {
        /// <summary>
        /// Frame width
        /// </summary>
        public int width;

        /// <summary>
        /// Frame height
        /// </summary>
        public int height;

        /// <summary>
        /// Number of channels (1, 3 or 4) depending on image format
        /// </summary>
        public int channels;

        /// <summary>
        /// Data as bytes (size = width * height * channels)
        /// </summary>
        public byte[] data = Array.Empty<byte>();

        /// <summary>
        /// Timestamp of frame capture
        /// </summary>
        public long timestamp;
    }
}
