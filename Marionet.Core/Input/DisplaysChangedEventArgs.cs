using System;
using System.Collections.Immutable;

namespace Marionet.Core.Input
{
    public class DisplaysChangedEventArgs : EventArgs
    {
        public DisplaysChangedEventArgs(ImmutableList<Rectangle> displays, Rectangle primaryDisplay)
        {
            Displays = displays;
            PrimaryDisplay = primaryDisplay;
        }

        public ImmutableList<Rectangle> Displays { get; }

        public Rectangle PrimaryDisplay { get; }
    }
}
