using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Communication
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
