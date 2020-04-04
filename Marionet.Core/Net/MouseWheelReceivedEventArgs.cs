using Marionet.Core.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Net
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
