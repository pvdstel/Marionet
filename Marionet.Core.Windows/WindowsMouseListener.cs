using Marionet.Core.Input;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    internal class WindowsMouseListener : IMouseListener
    {
        private readonly ChannelReader<Native.MouseChannelMessage> mouseChannelReader;
        private readonly IInputBlocking inputBlocking;

        public WindowsMouseListener(ChannelReader<Native.MouseChannelMessage> mouseChannelReader, IInputBlocking inputBlocking)
        {
            this.mouseChannelReader = mouseChannelReader;
            this.inputBlocking = inputBlocking;
        }

        public event EventHandler<MouseButtonActionEventArgs>? MouseButtonPressed;
        public event EventHandler<MouseButtonActionEventArgs>? MouseButtonReleased;
        public event EventHandler<MouseMoveEventArgs>? MouseMoved;
        public event EventHandler<MouseWheelEventArgs>? MouseWheel;

        public ValueTask<Point> GetCursorPosition()
        {
            Native.Methods.GetCursorPos(out Native.POINT cursorPos);
            return new ValueTask<Point>(new Point(cursorPos.x, cursorPos.y));
        }

        public async void StartListener(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (await mouseChannelReader.WaitToReadAsync(cancellationToken))
                    {
                        Native.MouseChannelMessage message = await mouseChannelReader.ReadAsync(cancellationToken);
                        ProcessMessage(message);
                    }
                });
            }
            catch (OperationCanceledException) { }
        }

        private void ProcessMessage(Native.MouseChannelMessage message)
        {
            if (message.lParam.dwExtraInfo.IsMarionetInstancePointer())
            {
                return;
            }

            switch (message.wParam)
            {
                case Native.LowLevelMouseProc_wParam.WM_LBUTTONDOWN:
                    MouseButtonPressed?.Invoke(this, new MouseButtonActionEventArgs(MouseButton.Left));
                    break;
                case Native.LowLevelMouseProc_wParam.WM_LBUTTONUP:
                    MouseButtonReleased?.Invoke(this, new MouseButtonActionEventArgs(MouseButton.Left));
                    break;
                case Native.LowLevelMouseProc_wParam.WM_MBUTTONDOWN:
                    MouseButtonPressed?.Invoke(this, new MouseButtonActionEventArgs(MouseButton.Middle));
                    break;
                case Native.LowLevelMouseProc_wParam.WM_MBUTTONUP:
                    MouseButtonReleased?.Invoke(this, new MouseButtonActionEventArgs(MouseButton.Middle));
                    break;
                case Native.LowLevelMouseProc_wParam.WM_RBUTTONDOWN:
                    MouseButtonPressed?.Invoke(this, new MouseButtonActionEventArgs(MouseButton.Right));
                    break;
                case Native.LowLevelMouseProc_wParam.WM_RBUTTONUP:
                    MouseButtonReleased?.Invoke(this, new MouseButtonActionEventArgs(MouseButton.Right));
                    break;
                case Native.LowLevelMouseProc_wParam.WM_MOUSEMOVE:
                    MouseMoved?.Invoke(this, new MouseMoveEventArgs(new Point(message.lParam.pt.x, message.lParam.pt.y), inputBlocking.IsInputBlocked));
                    break;
                case Native.LowLevelMouseProc_wParam.WM_MOUSEWHEEL:
                case Native.LowLevelMouseProc_wParam.WM_MOUSEHWHEEL:
                    int delta = message.lParam.mouseData >> 16; // delta is in the high word
                    MouseWheelDirection direction = message.wParam switch
                    {
                        Native.LowLevelMouseProc_wParam.WM_MOUSEWHEEL => MouseWheelDirection.Vertical,
                        Native.LowLevelMouseProc_wParam.WM_MOUSEHWHEEL => MouseWheelDirection.Horizontal,
                        _ => throw new NotImplementedException("This error should never occur."),
                    };
                    MouseWheel?.Invoke(this, new MouseWheelEventArgs(delta, direction));
                    break;
                case Native.LowLevelMouseProc_wParam.WM_XBUTTONDOWN:
                case Native.LowLevelMouseProc_wParam.WM_XBUTTONUP:
                    int xButton = message.lParam.mouseData >> 16; // button is in the high word
                    MouseButton button;
                    if (xButton == Native.Constants.XBUTTON1)
                    {
                        button = MouseButton.XButton1;
                    }
                    else if (xButton == Native.Constants.XBUTTON2)
                    {
                        button = MouseButton.XButton2;
                    }
                    else
                    {
                        break;
                    }

                    EventHandler<MouseButtonActionEventArgs>? eventHandler = message.wParam switch
                    {
                        Native.LowLevelMouseProc_wParam.WM_XBUTTONDOWN => MouseButtonPressed,
                        Native.LowLevelMouseProc_wParam.WM_XBUTTONUP => MouseButtonReleased,
                        _ => throw new NotImplementedException("This error should never occur."),
                    };

                    eventHandler?.Invoke(this, new MouseButtonActionEventArgs(button));

                    break;
            }
        }
    }
}
