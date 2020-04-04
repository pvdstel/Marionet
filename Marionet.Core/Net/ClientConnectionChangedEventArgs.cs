using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Net
{
    public class ClientConnectionChangedEventArgs : EventArgs
    {
        public ClientConnectionChangedEventArgs(string desktopName)
        {
            DesktopName = desktopName;
        }

        public string DesktopName { get; }
    }
}
