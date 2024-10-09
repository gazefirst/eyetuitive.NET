namespace GazeFirst
{
    /// <summary>
    /// Calibration point state
    /// </summary>
    public enum CalibrationPointState
    {
        Show,
        Collecting,
        Hide
    }

    /// <summary>
    /// Calibration points
    /// </summary>
    public enum CalibrationPoints
    {
        Nine = 0,// Nine point is default and mostly best
        One = 1,
        Five = 2,
        Thirteen = 3,
        Zero = 4 //Zero basically resets to a default calibration (only use this if user can not focus calibration points)
    }
}
