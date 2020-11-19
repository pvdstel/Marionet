using System;
using System.Threading.Tasks;

namespace Marionet.Core.Input
{
    public interface IMouseListener : IInputListener
    {
        ValueTask<Point> GetCursorPosition();

        event EventHandler<MouseButtonActionEventArgs> MouseButtonPressed;

        event EventHandler<MouseButtonActionEventArgs> MouseButtonReleased;

        event EventHandler<MouseMoveEventArgs> MouseMoved;

        event EventHandler<MouseWheelEventArgs> MouseWheel;
    }
}
