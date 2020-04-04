using System.ComponentModel;
using System.Threading.Channels;
using System.Windows.Forms;

namespace Marionet.Core.Windows
{
    internal class MessageWindow : Form
    {
        private ChannelWriter<Native.DisplayChannelMessage> displayChannelWriter;

        public MessageWindow(ChannelWriter<Native.DisplayChannelMessage> displayChannelWriter)
        {
            this.displayChannelWriter = displayChannelWriter;
            this.Text = "Marionet Message Window";
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;

            this.CreateHandle();
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.Constants.WM_DISPLAYCHANGE)
            {
                displayChannelWriter.TryWrite(new Native.DisplayChannelMessage { wParam = (uint)m.WParam, lParam = (uint)m.LParam });
            }

            base.WndProc(ref m);
        }
    }
}
