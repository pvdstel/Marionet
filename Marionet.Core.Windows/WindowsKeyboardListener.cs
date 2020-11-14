using Marionet.Core.Input;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    internal class WindowsKeyboardListener : IKeyboardListener
    {
        private readonly ChannelReader<Native.KeyboardChannelMessage> keyboardChannelReader;

        public WindowsKeyboardListener(ChannelReader<Native.KeyboardChannelMessage> keyboardChannelReader)
        {
            this.keyboardChannelReader = keyboardChannelReader;
        }

        public event EventHandler<KeyboardButtonActionEventArgs>? KeyboardButtonPressed;
        public event EventHandler<KeyboardButtonActionEventArgs>? KeyboardButtonReleased;

        public async void StartListener(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (await keyboardChannelReader.WaitToReadAsync(cancellationToken))
                    {
                        Native.KeyboardChannelMessage message = await keyboardChannelReader.ReadAsync(cancellationToken);
                        ProcessMessage(message);
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException) { }
        }

        private void ProcessMessage(Native.KeyboardChannelMessage message)
        {
            if (message.lParam.dwExtraInfo.IsMarionetInstancePointer())
            {
                return;
            }

            switch (message.wParam)
            {
                case Native.LowLevelKeyboardProc_wParam.WM_KEYDOWN:
                    KeyboardButtonPressed?.Invoke(this, new KeyboardButtonActionEventArgs(message.lParam.vkCode, false));
                    break;
                case Native.LowLevelKeyboardProc_wParam.WM_KEYUP:
                    KeyboardButtonReleased?.Invoke(this, new KeyboardButtonActionEventArgs(message.lParam.vkCode, false));
                    break;
                case Native.LowLevelKeyboardProc_wParam.WM_SYSKEYDOWN:
                    KeyboardButtonPressed?.Invoke(this, new KeyboardButtonActionEventArgs(message.lParam.vkCode, true));
                    break;
                case Native.LowLevelKeyboardProc_wParam.WM_SYSKEYUP:
                    KeyboardButtonReleased?.Invoke(this, new KeyboardButtonActionEventArgs(message.lParam.vkCode, true));
                    break;
            }
        }
    }
}
