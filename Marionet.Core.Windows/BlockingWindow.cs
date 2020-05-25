using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Marionet.Core.Windows
{
    internal class BlockingWindow : Form
    {
        public BlockingWindow()
        {
            this.CreateHandle();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;

            var centerX = Screen.PrimaryScreen.Bounds.Width / 2;
            var centerY = Screen.PrimaryScreen.Bounds.Height / 2;

            Top = centerY - 1;
            Left = centerX - 1;
            Width = 2;
            Height = 2;

            BackColor = Color.White;
            Opacity = 0;
            TopMost = true;
            Cursor.Hide();

            base.Show();
        }

        public new void Show()
        {
            Opacity = 0.01;
            var centerX = Screen.PrimaryScreen.Bounds.Width / 2;
            var centerY = Screen.PrimaryScreen.Bounds.Height / 2;
            Native.Methods.SetCursorPos(centerX, centerY);
        }

        public new void Hide()
        {
            Opacity = 0;
        }

        private async void DisplaysChanged()
        {
            await Task.Delay(WindowsDisplayAdapter.DisplayChangeProcessingDelay);
            var centerX = Screen.PrimaryScreen.Bounds.Width / 2;
            var centerY = Screen.PrimaryScreen.Bounds.Height / 2;
            Top = centerY - 1;
            Left = centerX - 1;
            Native.Methods.SetCursorPos(centerX, centerY);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.Constants.WM_DISPLAYCHANGE)
            {
                DisplaysChanged();
            }

            base.WndProc(ref m);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // turn on WS_EX_TOOLWINDOW style bit
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
    }
}
