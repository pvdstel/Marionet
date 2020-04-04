using System;
using System.Collections.Generic;

namespace Marionet.Core.Input
{
    public class DisplaysChangedEventArgs : EventArgs
    {
        public DisplaysChangedEventArgs(List<Rectangle> displays, Rectangle primaryDisplay)
        {
            Displays = displays;
            PrimaryDisplay = primaryDisplay;
        }

        public List<Rectangle> Displays { get; }

        public Rectangle PrimaryDisplay { get; }
    }
}
