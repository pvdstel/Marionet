using System.Collections.Generic;

namespace Marionet.Core.Communication
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
