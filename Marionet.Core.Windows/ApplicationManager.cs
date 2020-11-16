using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Marionet.Core.Input;
using Marionet.Core.Windows.Native;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    internal class ApplicationManager : IDisposable
    {
        private (Func<Task> block, Func<Task> unblock)? appActions;

        private CancellationTokenSource? cancellation;

        private readonly ChannelWriter<Native.MouseChannelMessage> mouseChannelWriter;
        private readonly ChannelWriter<Native.KeyboardChannelMessage> keyboardChannelWriter;
        private readonly ChannelWriter<Native.DisplayChannelMessage> displayChannelWriter;
        private readonly IInputBlocking inputBlocking;
        private readonly Action onSystemEvent;

        public ApplicationManager(
            WindowsInputManager windowsInputManager,
            Action onSystemEvent)
        {
            inputBlocking = windowsInputManager;
            mouseChannelWriter = windowsInputManager.mouseInputChannel.Writer;
            keyboardChannelWriter = windowsInputManager.keyboardInputChannel.Writer;
            displayChannelWriter = windowsInputManager.displayInputChannel.Writer;
            this.onSystemEvent = onSystemEvent;
        }

        public Task StartApplication()
        {
            StopApplication();

            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            var cancellationToken = cancellation.Token;

            TaskCompletionSource inputStartCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource applicationStartCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Thread inputThread = new Thread(() =>
            {
                var mouseProc = new HookProc(LowLevelMouseProc);
                var keyboardProc = new HookProc(LowLevelKeyboardProc);
                var switchProc = new WinEventDelegate(SwitchDesktopHandler);

                var mouseProcHandle = GCHandle.Alloc(mouseProc);
                var keyboardProcHandle = GCHandle.Alloc(keyboardProc);
                var switchProcHandle = GCHandle.Alloc(switchProc);

                IntPtr mouseHook = Native.Methods.SetWindowsHookEx(Native.IdHook.WH_MOUSE_LL, mouseProc, IntPtr.Zero, 0);
                IntPtr keyboardHook = Native.Methods.SetWindowsHookEx(Native.IdHook.WH_KEYBOARD_LL, keyboardProc, IntPtr.Zero, 0);
                IntPtr switchHook = Native.Methods.SetWinEventHook(Native.Constants.EVENT_SYSTEM_DESKTOPSWITCH, Native.Constants.EVENT_SYSTEM_DESKTOPSWITCH, IntPtr.Zero, switchProc, 0, 0, 0);

                uint currentThreadId = Native.Methods.GetCurrentThreadId();
                cancellationToken.Register(() =>
                {
                    Native.Methods.PostThreadMessage(currentThreadId, Native.Constants.WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
                });

                inputStartCompleted.TrySetResult();

                while (!cancellationToken.IsCancellationRequested)
                {
                    _ = Native.Methods.GetMessage(out Native.MSG msg, IntPtr.Zero, 0, 0);
                    if (msg.message == Native.Constants.WM_QUIT)
                    {
                        break;
                    }
                    Native.Methods.TranslateMessage(ref msg);
                    Native.Methods.DispatchMessage(ref msg);
                }

                Native.Methods.UnhookWindowsHookEx(mouseHook);
                Native.Methods.UnhookWindowsHookEx(keyboardHook);
                Native.Methods.UnhookWinEvent(switchHook);

                mouseProcHandle.Free();
                keyboardProcHandle.Free();
                switchProcHandle.Free();
            });
            inputThread.Start();

            Thread applicationThread = new Thread(() =>
            {
                bool needsNewApplication = Application.Current == null;
                Debug.WriteLine($"Marionet.Core.Windows.ApplicationManager: Avalonia application not initialized = {needsNewApplication}");
                Debug.WriteLineIf(needsNewApplication, "Creating new application...");
                if (needsNewApplication)
                {
                    AppBuilder.Configure<HostApp>().UsePlatformDetect().SetupWithoutStarting();
                }

                MessageWindow? messageWindow = null;
                BlockingWindow? blockingWindow = null;
                Dispatcher.UIThread.InvokeAsync(() =>
                {

                    messageWindow = new MessageWindow(displayChannelWriter);
                    blockingWindow = new BlockingWindow();
                    Debug.WriteLine($"MessageWindow @{messageWindow.Handle.Handle}");
                    Debug.WriteLine($"BlockingWindow @{blockingWindow.Handle.Handle}");
                });

                cancellationToken.Register(() =>
                {
                    appActions = null;
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        messageWindow?.Dispose();
                        blockingWindow?.Close(true);
                        blockingWindow?.Dispose();
                    });
                });

                void block()
                {
                    blockingWindow?.Show();
                }

                void unblock()
                {
                    blockingWindow?.Hide();
                }

                appActions = (
                    () => Dispatcher.UIThread.InvokeAsync(block),
                    () => Dispatcher.UIThread.InvokeAsync(unblock)
                );

                applicationStartCompleted.TrySetResult();

                if (needsNewApplication)
                {
                    Application.Current.Run(cancellationToken);
                }
            });
            applicationThread.Start();

            return Task.WhenAll(inputStartCompleted.Task, applicationStartCompleted.Task);
        }

        public void StopApplication()
        {
            cancellation?.Cancel();
        }

        public Task ShowBlockingWindow() => appActions?.block() ?? Task.CompletedTask;

        public Task HideBlockingWindow() => appActions?.unblock() ?? Task.CompletedTask;

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
                    cancellation?.Dispose();
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
