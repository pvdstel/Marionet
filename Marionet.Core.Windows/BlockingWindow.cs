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

            var center = Screen.PrimaryScreen.Bounds.Width / 2;

            Top = 0;
            Left = center - 1;
            Width = 2;
            Height = 2;

            BackColor = Color.White;
            Opacity = 0.01;
            TopMost = true;
            Cursor.Hide();

            Native.Methods.SetCursorPos(center, 1);
        }

        private async void DisplaysChanged()
        {
            await Task.Delay(WindowsDisplayAdapter.DisplayChangeProcessingDelay);
            var center = Screen.PrimaryScreen.Bounds.Width / 2;
            Top = 0;
            Left = center - 1;
            Native.Methods.SetCursorPos(center, 1);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.Constants.WM_DISPLAYCHANGE)
            {
                DisplaysChanged();
            }

            base.WndProc(ref m);
        }
    }
}
