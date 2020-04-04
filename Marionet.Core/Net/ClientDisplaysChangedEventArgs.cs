using System;
using System.Collections.Generic;

namespace Marionet.Core.Net
{
    public class ClientDisplaysChangedEventArgs : EventArgs
    {
        public ClientDisplaysChangedEventArgs(string desktopName, List<Rectangle> displays)
        {
            DesktopName = desktopName;
            Displays = displays;
        }

        public string DesktopName { get; }

        public List<Rectangle> Displays { get; }
    }
}
