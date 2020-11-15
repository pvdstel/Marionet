using System;
using System.Collections.ObjectModel;

namespace Marionet.Core.Communication
{
    public class ClientDisplaysChangedEventArgs : EventArgs
    {
        public ClientDisplaysChangedEventArgs(string desktopName, ReadOnlyCollection<Rectangle> displays)
        {
            DesktopName = desktopName;
            Displays = displays;
        }

        public string DesktopName { get; }

        public ReadOnlyCollection<Rectangle> Displays { get; }
    }
}
