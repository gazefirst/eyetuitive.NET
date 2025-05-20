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
        /// Confidence of the point
        /// </summary>
        public double Confidence { get; set; } = 1d;

        /// <summary>
        /// True if the point has a confidence value
        /// </summary>
        public bool HasConfidence { get; set; }

        /// <summary>
        /// NormedPoint2d constructor
        /// </summary>
        public NormedPoint2d()
        {
            X = 0;
            Y = 0;
            Confidence = 1d;
            HasConfidence = false;
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
            Confidence = 1d;
            HasConfidence = false;
        }

        /// <summary>
        /// NormedPoint2d constructor with confidence
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="confidence"></param>
        public NormedPoint2d(double x, double y, double confidence)
        {
            X = x;
            Y = y;
            Confidence = confidence;
            HasConfidence = true;
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
