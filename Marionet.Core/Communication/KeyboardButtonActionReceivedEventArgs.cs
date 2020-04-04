using System;

namespace Marionet.Core.Communication
{
    public class KeyboardButtonActionReceivedEventArgs : EventArgs
    {
        public KeyboardButtonActionReceivedEventArgs(string from, int keyCode)
        {
            From = from;
            KeyCode = keyCode;
        }

        public string From { get; }

        public int KeyCode { get; }
    }
}
