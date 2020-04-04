using Marionet.Core.Input;
using System;

namespace Marionet.Core.Communication
{
    public class MouseButtonActionReceivedEventArgs : EventArgs
    {
        public MouseButtonActionReceivedEventArgs(string from, MouseButton button)
        {
            From = from;
            Button = button;
        }

        public string From { get; }

        public MouseButton Button { get; }
    }
}
