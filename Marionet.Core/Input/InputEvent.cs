using System;

namespace Marionet.Core.Input
{
    public class InputEventArgs : EventArgs
    {
        public InputEventArgs(bool blocked)
        {
            Blocked = blocked;
        }

        /// <summary>
        /// Gets whether this event was generated while input was blocked.
        /// </summary>
        public bool Blocked { get; }
    }
}
