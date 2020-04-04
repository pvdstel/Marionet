using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Net
{
    public class DesktopsChangedEventArgs
    {
        public DesktopsChangedEventArgs(List<string> desktops)
        {
            Desktops = desktops;
        }

        public List<string> Desktops { get; }
    }
}
