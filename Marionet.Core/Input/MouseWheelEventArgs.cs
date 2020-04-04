using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Input
{
    public class MouseWheelEventArgs : EventArgs
    {
        public MouseWheelEventArgs(int delta, MouseWheelDirection direction)
        {
            Delta = delta;
            Direction = direction;
        }

        public int Delta { get; }

        public MouseWheelDirection Direction { get; }
    }
}
