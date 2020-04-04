using System.Threading.Tasks;

namespace Marionet.Core.Input
{
    public interface IMouseController
    {
        Task PressMouseButton(MouseButton button);

        Task ReleaseMouseButton(MouseButton button);

        Task MoveMouse(Point position);

        Task Wheel(int delta, MouseWheelDirection direction);
    }
}
