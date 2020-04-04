using System;

namespace Marionet.Core.Input
{
    public class KeyboardButtonActionEventArgs : EventArgs
    {
        public KeyboardButtonActionEventArgs(int keyCode, bool isSystem)
        {
            KeyCode = keyCode;
            IsSystem = isSystem;
        }

        public int KeyCode { get; }

        public bool IsSystem { get; }
    }
}
