namespace GazeFirst
{
    /// <summary>
    /// Normalized point in 2D space
    /// </summary>
    public class NormedPoint2d
    {
        /// <summary>
        /// X coordinate in 2D space
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y coordinate in 2D space
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// NormedPoint2d constructor
        /// </summary>
        public NormedPoint2d()
        {
            X = 0;
            Y = 0;
        }

        /// <summary>
        /// NormedPoint2d constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public NormedPoint2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// ToString override
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
