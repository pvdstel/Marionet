using Marionet.Core.Input;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Marionet.Core.Windows
{
    internal class ApplicationManager : IDisposable
    {
        private (Action exit, Action block, Action unblock)? appActions;
        private Thread? applicationThread;

        private readonly ChannelWriter<Native.MouseChannelMessage> mouseChannelWriter;
        private readonly ChannelWriter<Native.KeyboardChannelMessage> keyboardChannelWriter;
        private readonly ChannelWriter<Native.DisplayChannelMessage> displayChannelWriter;
        private readonly IInputBlocking inputBlocking;
        private readonly Action onSystemEvent;

        public ApplicationManager(
            ChannelWriter<Native.MouseChannelMessage> mouseChannelWriter,
            ChannelWriter<Native.KeyboardChannelMessage> keyboardChannelWriter,
            ChannelWriter<Native.DisplayChannelMessage> displayChannelWriter,
            IInputBlocking inputBlocking,
            Action onSystemEvent)
        {
            this.mouseChannelWriter = mouseChannelWriter;
            this.keyboardChannelWriter = keyboardChannelWriter;
            this.displayChannelWriter = displayChannelWriter;
            this.inputBlocking = inputBlocking;
            this.onSystemEvent = onSystemEvent;
        }

        public Task StartApplication()
        {
            DisposeApplication();

            TaskCompletionSource<object?> startCompleted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            applicationThread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(true);
                Application.SetHighDpiMode(HighDpiMode.PerMonitor);

                MessageWindow messageWindow = new MessageWindow(displayChannelWriter);
                BlockingWindow? blockingWindow = default;

                IntPtr mouseHook = Native.Methods.SetWindowsHookEx(Native.IdHook.WH_MOUSE_LL, LowLevelMouseProc, IntPtr.Zero, 0);
                IntPtr keyboardHook = Native.Methods.SetWindowsHookEx(Native.IdHook.WH_KEYBOARD_LL, LowLevelKeyboardProc, IntPtr.Zero, 0);
                IntPtr switchHook = Native.Methods.SetWinEventHook(Native.Constants.EVENT_SYSTEM_DESKTOPSWITCH, Native.Constants.EVENT_SYSTEM_DESKTOPSWITCH, IntPtr.Zero, SwitchDesktopHandler, 0, 0, 0);

                void exit()
                {
                    appActions = null;
                    Native.Methods.UnhookWindowsHookEx(mouseHook);
                    Native.Methods.UnhookWindowsHookEx(keyboardHook);
                    Native.Methods.UnhookWinEvent(switchHook);
                    Application.Exit();
                }

                void block()
                {
                    if (blockingWindow == default)
                    {
                        blockingWindow = new BlockingWindow();
                        blockingWindow.Show();
                    }
                }

                void unblock()
                {
                    if (blockingWindow != default)
                    {
                        blockingWindow.Close();
                        blockingWindow = default;
                    }
                }

                Action invokeAction(Action action)
                {
                    return () => messageWindow.Invoke(action);
                }

                appActions = (exit,
                    invokeAction(block),
                    invokeAction(unblock)
                );

                startCompleted.TrySetResult(null);

                Application.Run(messageWindow);
            });
            applicationThread.SetApartmentState(ApartmentState.STA);
            applicationThread.Start();

            return startCompleted.Task;
        }

        public void StopApplication()
        {
            DisposeApplication();
        }

        private void DisposeApplication()
        {
            appActions?.exit.Invoke();
            applicationThread = null;
        }

        public void ShowBlockingWindow() => appActions?.block();

        public void HideBlockingWindow() => appActions?.unblock();

        private IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var st = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
                mouseChannelWriter.TryWrite(new Native.MouseChannelMessage { wParam = (Native.LowLevelMouseProc_wParam)(uint)wParam, lParam = st });

                if (inputBlocking.IsInputBlocked && !st.dwExtraInfo.IsMarionetInstancePointer())
                {
                    return new IntPtr(1);
                }
            }
            return Native.Methods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var st = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
                keyboardChannelWriter.TryWrite(new Native.KeyboardChannelMessage { wParam = (Native.LowLevelKeyboardProc_wParam)(uint)wParam, lParam = st });

                if (inputBlocking.IsInputBlocked && !st.dwExtraInfo.IsMarionetInstancePointer())
                {
                    return new IntPtr(1);
                }
            }
            return Native.Methods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private void SwitchDesktopHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            onSystemEvent();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeApplication();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
