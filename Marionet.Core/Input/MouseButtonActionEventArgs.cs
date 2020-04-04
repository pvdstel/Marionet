using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Input
{
    public class MouseButtonActionEventArgs : EventArgs
    {
        public MouseButtonActionEventArgs(MouseButton button)
        {
            Button = button;
        }

        public MouseButton Button { get; }
    }
}
