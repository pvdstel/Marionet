using Marionet.Core.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Net
{
    public class MouseButtonActionReceivedEventArgs : EventArgs
    {
        public MouseButtonActionReceivedEventArgs(string from,  MouseButton button)
        {
            From = from;
            Button = button;
        }

        public string From { get; }

        public MouseButton Button { get; }
    }
}
