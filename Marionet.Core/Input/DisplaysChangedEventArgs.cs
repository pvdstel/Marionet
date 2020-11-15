using System;
using System.Collections.ObjectModel;

namespace Marionet.Core.Input
{
    public class DisplaysChangedEventArgs : EventArgs
    {
        public DisplaysChangedEventArgs(ReadOnlyCollection<Rectangle> displays, Rectangle primaryDisplay)
        {
            Displays = displays;
            PrimaryDisplay = primaryDisplay;
        }

        public ReadOnlyCollection<Rectangle> Displays { get; }

        public Rectangle PrimaryDisplay { get; }
    }
}
