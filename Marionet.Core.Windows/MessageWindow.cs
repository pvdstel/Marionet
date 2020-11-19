using Avalonia.Win32;
using System;
using System.Threading.Channels;

namespace Marionet.Core.Windows
{
    internal class MessageWindow : WindowImpl
    {
        private readonly ChannelWriter<Native.DisplayChannelMessage> displayChannelWriter;

        public MessageWindow(ChannelWriter<Native.DisplayChannelMessage> displayChannelWriter)
        {
            this.displayChannelWriter = displayChannelWriter;
            this.SetTitle("Marionet Message Window");
            this.ShowTaskbarIcon(false);
            this.WindowState = Avalonia.Controls.WindowState.Minimized;

            this.Handle.Handle.ToInt64(); // ensure the handle exists
        }

        public override void Show() { }

        protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == Native.Constants.WM_DISPLAYCHANGE)
            {
                displayChannelWriter.TryWrite(new Native.DisplayChannelMessage { wParam = (uint)wParam, lParam = (uint)lParam });
            }

            return base.WndProc(hWnd, msg, wParam, lParam);
        }

    }
}
