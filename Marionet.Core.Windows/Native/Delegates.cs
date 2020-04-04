using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Marionet.Core.Windows.Native
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
}
