using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.LocalState
{
    internal class Uncontrolled : State
    {
        public Uncontrolled(Rectangle activeDisplay, Rectangle baseDisplay)
        {
            ActiveDisplay = activeDisplay;
            BaseDisplay = baseDisplay;
        }

        public Rectangle ActiveDisplay { get; }

        public Rectangle BaseDisplay { get; }
    }
}
