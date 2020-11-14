using System;

namespace Marionet.Core.Input
{
    public interface IKeyboardListener : IInputListener
    {
        event EventHandler<KeyboardButtonActionEventArgs> KeyboardButtonPressed;

        event EventHandler<KeyboardButtonActionEventArgs> KeyboardButtonReleased;
    }
}
