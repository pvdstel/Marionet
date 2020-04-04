using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.LocalState
{
    internal class Controlling : State
    {
        public Controlling(Desktop activeDesktop, Rectangle activeDisplay, Point cursorPosition)
        {
            ActiveDesktop = activeDesktop;
            ActiveDisplay = activeDisplay;
            CursorPosition = cursorPosition;
        }

        public Desktop ActiveDesktop { get; }

        public Rectangle ActiveDisplay { get; }

        public Point CursorPosition { get; set; }
    }
}
