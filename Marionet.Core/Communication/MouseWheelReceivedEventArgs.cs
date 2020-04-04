using Marionet.Core.Input;
using System;

namespace Marionet.Core.Communication
{
    public class MouseWheelReceivedEventArgs : EventArgs
    {
        public MouseWheelReceivedEventArgs(string from, int delta, MouseWheelDirection direction)
        {
            From = from;
            Delta = delta;
            Direction = direction;
        }

        public string From { get; }

        public int Delta { get; }

        public MouseWheelDirection Direction { get; }
    }
}
