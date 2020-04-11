using Marionet.Core.Input;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    internal class WindowsMouseController : IMouseController, IDisposable
    {
        private readonly IDisplayAdapter displayAdapter;
        private Rectangle virtualDesktop = new Rectangle(0, 0, 1, 1);
        private double scaleFactorX = 1;
        private double scaleFactorY = 1;
        private const int VirtualDesktopDimensions = 65535;

        public WindowsMouseController(IDisplayAdapter displayAdapter)
        {
            this.displayAdapter = displayAdapter;
            UpdateVirtualDesktopSize();
            displayAdapter.DisplaysChanged += OnDisplaysChanged;
        }

        public Task MoveMouse(Point position)
        {
            var movement = new Native.INPUT()
            {
                Type = Native.Constants.INPUT_MOUSE,
            };
            var virtualDesktopPosition = ToVirtualDesktopCoordinates(position);
            movement.Data.Mouse.dwFlags = Native.MOUSEEVENTF.ABSOLUTE | Native.MOUSEEVENTF.MOVE | Native.MOUSEEVENTF.VIRTUALDESK;
            movement.Data.Mouse.dx = virtualDesktopPosition.X;
            movement.Data.Mouse.dy = virtualDesktopPosition.Y;
            movement.Data.Mouse.dwExtraInfo = InputUtils.InstancePointer;
            movement.SendSingleInput();
            return Task.CompletedTask;
        }

        public Task PressMouseButton(MouseButton button)
        {
            var mouseDown = new Native.INPUT()
            {
                Type = Native.Constants.INPUT_MOUSE,
            };

            switch (button)
            {
                case MouseButton.Left:
                    mouseDown.Data.Mouse.dwFlags = Native.MOUSEEVENTF.LEFTDOWN;
                    break;
                case MouseButton.Middle:
                    mouseDown.Data.Mouse.dwFlags = Native.MOUSEEVENTF.MIDDLEDOWN;
                    break;
                case MouseButton.Right:
                    mouseDown.Data.Mouse.dwFlags = Native.MOUSEEVENTF.RIGHTDOWN;
                    break;
                case MouseButton.XButton1:
                    mouseDown.Data.Mouse.dwFlags = Native.MOUSEEVENTF.XDOWN;
                    mouseDown.Data.Mouse.mouseData = Native.Constants.XBUTTON1;
                    break;
                case MouseButton.XButton2:
                    mouseDown.Data.Mouse.dwFlags = Native.MOUSEEVENTF.XDOWN;
                    mouseDown.Data.Mouse.mouseData = Native.Constants.XBUTTON2;
                    break;
            }

            mouseDown.Data.Mouse.dwExtraInfo = InputUtils.InstancePointer;
            mouseDown.SendSingleInput();
            return Task.CompletedTask;
        }

        public Task ReleaseMouseButton(MouseButton button)
        {
            var mouseUp = new Native.INPUT()
            {
                Type = Native.Constants.INPUT_MOUSE,
            };

            switch (button)
            {
                case MouseButton.Left:
                    mouseUp.Data.Mouse.dwFlags = Native.MOUSEEVENTF.LEFTUP;
                    break;
                case MouseButton.Middle:
                    mouseUp.Data.Mouse.dwFlags = Native.MOUSEEVENTF.MIDDLEUP;
                    break;
                case MouseButton.Right:
                    mouseUp.Data.Mouse.dwFlags = Native.MOUSEEVENTF.RIGHTUP;
                    break;
                case MouseButton.XButton1:
                    mouseUp.Data.Mouse.dwFlags = Native.MOUSEEVENTF.XUP;
                    mouseUp.Data.Mouse.mouseData = Native.Constants.XBUTTON1;
                    break;
                case MouseButton.XButton2:
                    mouseUp.Data.Mouse.dwFlags = Native.MOUSEEVENTF.XUP;
                    mouseUp.Data.Mouse.mouseData = Native.Constants.XBUTTON2;
                    break;
            }

            mouseUp.Data.Mouse.dwExtraInfo = InputUtils.InstancePointer;
            mouseUp.SendSingleInput();
            return Task.CompletedTask;
        }

        public Task Wheel(int delta, MouseWheelDirection direction)
        {
            var wheel = new Native.INPUT()
            {
                Type = Native.Constants.INPUT_MOUSE,
            };

            wheel.Data.Mouse.dwFlags = direction == MouseWheelDirection.Horizontal ? Native.MOUSEEVENTF.HWHEEL : Native.MOUSEEVENTF.WHEEL;
            wheel.Data.Mouse.mouseData = delta;
            wheel.SendSingleInput();
            return Task.CompletedTask;
        }

        private void UpdateVirtualDesktopSize()
        {
            var displays = displayAdapter.GetDisplays();
            var minLeft = displays.Select(d => d.Left).Min();
            var minTop = displays.Select(d => d.Top).Min();
            var maxRight = displays.Select(d => d.Right).Max();
            var maxBottom = displays.Select(d => d.Bottom).Max();
            virtualDesktop = new Rectangle(minLeft, minTop, maxRight - minLeft, maxBottom - minTop);
            scaleFactorX = (double)virtualDesktop.Width / (virtualDesktop.Width - 1);
            scaleFactorY = (double)virtualDesktop.Height / (virtualDesktop.Height - 1);
        }

        private Point ToVirtualDesktopCoordinates(Point point)
        {
            int x = Math.Min((int)Math.Round(((point.X - virtualDesktop.X) * VirtualDesktopDimensions / virtualDesktop.Width) * scaleFactorX), VirtualDesktopDimensions);
            int y = Math.Min((int)Math.Round(((point.Y - virtualDesktop.Y) * VirtualDesktopDimensions / virtualDesktop.Height) * scaleFactorY), VirtualDesktopDimensions);
            return new Point(x, y);
        }

        private void OnDisplaysChanged(object? sender, DisplaysChangedEventArgs e)
        {
            UpdateVirtualDesktopSize();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    displayAdapter.DisplaysChanged -= OnDisplaysChanged;
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
