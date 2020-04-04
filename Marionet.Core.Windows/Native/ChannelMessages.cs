using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Windows.Native
{
    internal class MouseChannelMessage
    {
        public LowLevelMouseProc_wParam wParam;
        public MSLLHOOKSTRUCT lParam; 
    }

    internal class KeyboardChannelMessage
    {
        public LowLevelKeyboardProc_wParam wParam;
        public KBDLLHOOKSTRUCT lParam;
    }

    internal class DisplayChannelMessage
    {
        public uint wParam;
        public uint lParam;
    }
}
