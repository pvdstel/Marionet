using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Input
{
    public interface IKeyboardListener
    {
        event EventHandler<KeyboardButtonActionEventArgs> KeyboardButtonPressed;

        event EventHandler<KeyboardButtonActionEventArgs> KeyboardButtonReleased;
    }
}
