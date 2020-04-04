using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Net
{
    public class KeyboardButtonActionReceivedEventArgs : EventArgs
    {
        public KeyboardButtonActionReceivedEventArgs(string from, int keyCode)
        {
            From = from;
            KeyCode = keyCode;
        }

        public string From{ get; }

        public int KeyCode { get; }
    }
}
