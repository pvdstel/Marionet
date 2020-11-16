using Avalonia.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Marionet.UI.Native.Windows
{
    /**
    * Adapted from https://github.com/dotnet/winforms/blob/33e683aaf6a4ecaa87d31d3ee98de00f6db1ea2f/src/System.Windows.Forms/src/System/Windows/Forms/NotifyIcon.cs,
    * which is licensed under the MIT license.
    */

    public class BasicNotifyIcon : IDisposable
    {
        private const int WM_USER = 0x0400;
        private const int WM_TRAYMOUSEMESSAGE = (int)WM_USER + 1024;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;

        private const int NIF_MESSAGE = 0x1;
        private const int NIF_ICON = 0x2;
        private const int NIF_TIP = 0x4;

        private static int nextId = 0;

        private Icon icon;
        private string text = "";
        private bool visible;
        private NotifyIconWindow window;
        private bool added = false;
        private readonly int id = 0;
        private readonly object syncObj = new object();
        private bool disposedValue;

        public BasicNotifyIcon(Icon icon)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }

            this.icon = icon ?? throw new ArgumentNullException(nameof(icon));
            window = new NotifyIconWindow(this);
            id = ++nextId;
        }

        public Icon Icon
        {
            get => icon;
            set
            {
                if (icon != value)
                {
                    icon = value;
                    UpdateIcon();
                }
            }
        }

        public string Text
        {
            get => text;
            set
            {
                if (value == null) value = "";
                if (!value.Equals(text))
                {
                    if (value != null && value.Length > 63)
                    {
                        throw new ArgumentOutOfRangeException(nameof(Text));
                    }
                    text = value!;
                    if (added)
                    {
                        UpdateIcon(true);
                    }
                }
            }
        }

        public bool Visible
        {
            get => visible;
            set
            {
                if (visible != value)
                {
                    UpdateIcon(value);
                    visible = value;
                }
            }
        }

        public event EventHandler? Clicked;

        public static bool IsSupported() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [DllImport("shell32.dll")]
        static extern bool Shell_NotifyIcon(int dwMessage, [In] ref NotifyIconData pnid);

        private void UpdateIcon(bool showIcon = true)
        {
            lock (syncObj)
            {
                window.LockReference(showIcon);

                NotifyIconData data = NotifyIconData.Make();
                data.uCallbackMessage = WM_TRAYMOUSEMESSAGE;
                data.uFlags = NIF_MESSAGE;
                if (showIcon)
                {
                    if (window.Handle.Handle == IntPtr.Zero)
                    {
                        return;
                    }
                }
                data.hWnd = window.Handle.Handle;
                data.uID = id;
                data.hIcon = IntPtr.Zero;
                data.szTip = null;
                if (icon != null)
                {
                    data.uFlags |= NIF_ICON;
                    data.hIcon = icon.Handle;
                }
                data.uFlags |= NIF_TIP;
                data.szTip = text;

                if (showIcon && icon != null)
                {
                    if (!added)
                    {
                        Shell_NotifyIcon(NIM_ADD, ref data);
                        added = true;
                    }
                    else
                    {
                        Shell_NotifyIcon(NIM_MODIFY, ref data);
                    }
                }
                else if (added)
                {
                    Shell_NotifyIcon(NIM_DELETE, ref data);
                    added = false;
                }
            }
        }

        private void WndProc(uint msg, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_TRAYMOUSEMESSAGE:
                    switch (lParam.ToInt32())
                    {
                        case WM_LBUTTONDOWN:
                            break;
                        case WM_LBUTTONUP:
                            Clicked?.Invoke(this, new EventArgs());
                            break;
                    }
                    break;
            }
        }

        private class NotifyIconWindow : WindowImpl, IDisposable
        {
            internal BasicNotifyIcon reference;
            private GCHandle rootRef;

            public NotifyIconWindow(BasicNotifyIcon reference)
            {
                this.reference = reference ?? throw new ArgumentNullException(nameof(reference));
            }

            public void LockReference(bool locked)
            {
                if (locked)
                {
                    if (!rootRef.IsAllocated)
                    {
                        rootRef = GCHandle.Alloc(reference, GCHandleType.Normal);
                    }
                }
                else
                {
                    if (rootRef.IsAllocated)
                    {
                        rootRef.Free();
                    }
                }
            }

            protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
            {
                if (reference != null)
                {
                    reference.WndProc(msg, lParam);
                }
                return base.WndProc(hWnd, msg, wParam, lParam);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    icon = null!;
                    Text = string.Empty;
                    UpdateIcon(false);
                    window.Dispose();
                    window = null!;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
