using System;

namespace Marionet.Core.Communication
{
    public class MouseMoveReceivedEventArgs : EventArgs
    {
        public MouseMoveReceivedEventArgs(string from, Point position)
        {
            From = from;
            Position = position;
        }

        public string From { get; }

        public Point Position { get; }
    }
}
