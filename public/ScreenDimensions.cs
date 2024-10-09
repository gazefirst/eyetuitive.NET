namespace GazeFirst
{
    /// <summary>
    /// Screen dimensions 
    /// </summary>
    public class ScreenDimensions
    {
        /// <summary>
        /// ScreenDimensions constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public ScreenDimensions(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// ScreenDimensions constructor (empty)
        /// </summary>
        public ScreenDimensions()
        {
            Width = 0;
            Height = 0;
        }

        /// <summary>
        /// Width of the screen in millimeters
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Height of the screen in millimeters
        /// </summary>
        public double Height { get; set; }
    }
}
