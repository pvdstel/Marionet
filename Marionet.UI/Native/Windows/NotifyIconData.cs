using System;
using System.Runtime.InteropServices;

namespace Marionet.UI.Native.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "It's a native struct")]
    public struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string? szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string? szInfoTitle;
        public int dwInfoFlags;

        public static NotifyIconData Make()
        {
            NotifyIconData notifyIconData = new NotifyIconData();
            notifyIconData.cbSize = Marshal.SizeOf(notifyIconData);
            notifyIconData.dwState = 0;
            notifyIconData.dwStateMask = 0;
            return notifyIconData;
        }
    }
}
