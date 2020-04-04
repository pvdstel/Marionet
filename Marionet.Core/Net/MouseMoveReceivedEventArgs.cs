using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Net
{
    public class MouseMoveReceivedEventArgs : EventArgs
    {
        public MouseMoveReceivedEventArgs(string from, Point position)
        {
            From = from;
            Position = position;
        }

        public string From { get; }

        public Point Position{ get; }
    }
}
