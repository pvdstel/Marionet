using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Windows.Native
{
    internal static class Constants
    {
        public const int WHEEL_DELTA = 120;
        public const int XBUTTON1 = 0x0001;
        public const int XBUTTON2 = 0x0002;
        public const int WM_DISPLAYCHANGE = 0x007E;

        public const int INPUT_MOUSE = 0;
        public const int INPUT_KEYBOARD = 1;
        public const int INPUT_HARDWARE = 2;

        public const int EVENT_SYSTEM_DESKTOPSWITCH = 0x0020;
    }
}
