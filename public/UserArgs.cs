using System;

namespace GazeFirst
{
    /// <summary>
    /// User event arguments
    /// </summary>
    public class UserArgs : EventArgs
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int UserID = -1;

        /// <summary>
        /// User name
        /// </summary>
        public string UserName = string.Empty;

        /// <summary>
        /// User active status
        /// </summary>
        public bool Active = false;

        /// <summary>
        /// Guid of the user
        /// </summary>
        public Guid UserGuid = Guid.Empty;
    }
}
