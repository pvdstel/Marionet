﻿using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    internal class BlockingWindow : Window, IDisposable
    {
        private readonly BlockingWindowImpl blockingWindowImpl;
        private bool allowClose = false;

        private class BlockingWindowImpl : WindowImpl
        {
            public BlockingWindowImpl() : base()
            {
                long exStyle = Native.Methods.GetWindowLong(Handle.Handle, Native.Constants.GWL_EXSTYLE);
                exStyle |= 0x00000080L; // WS_EX_TOOLWINDOW: does not show up in task switchers;
                exStyle |= 0x08000000L; // WS_EX_NOACTIVATE
                Native.Methods.SetWindowLong(Handle.Handle, Native.Constants.GWL_EXSTYLE, exStyle);
            }

            protected override System.IntPtr WndProc(System.IntPtr hWnd, uint msg, System.IntPtr wParam, System.IntPtr lParam)
            {
                if (msg == Native.Constants.WM_DISPLAYCHANGE)
                {
                    DisplaysChanged?.Invoke(this, new EventArgs());
                }
                return base.WndProc(hWnd, msg, wParam, lParam);
            }

            public event EventHandler? DisplaysChanged;
        }

        public BlockingWindow() : this(new BlockingWindowImpl()) { }

        private BlockingWindow(BlockingWindowImpl blockingWindowImpl) : base(blockingWindowImpl)
        {
            this.blockingWindowImpl = blockingWindowImpl;

            SystemDecorations = SystemDecorations.None;
            ShowInTaskbar = false;
            Title = "Marionet";

            Background = Avalonia.Media.Brushes.Transparent;
            TransparencyLevelHint = WindowTransparencyLevel.Transparent;
            Topmost = true;

            blockingWindowImpl.DisplaysChanged += (sender, e) => DisplaysChanged();

            // Set the cursor in PointerEnter, because the cursor is not hidden otherwise
            void onPointerEnter(object? sender, EventArgs e)
            {
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None);
            };
            PointerEnter += onPointerEnter;

            InitializeIfNeeded();
            RefreshWindowRect();
        }

        public IPlatformHandle Handle { get => blockingWindowImpl.Handle; }

        public void Close(bool allowClose = false)
        {
            this.allowClose = allowClose;
            base.Close();
        }

        protected void RefreshWindowRect(bool centerCursor = false)
        {
            var centerX = Screens.Primary.Bounds.Width / 2;
            var centerY = Screens.Primary.Bounds.Height / 2;
            Position = new Avalonia.PixelPoint(centerX - 1, centerY - 1);

            Width = 2;
            Height = 2;

            if (centerCursor)
            {
                Native.Methods.SetCursorPos(centerX, centerY);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = !allowClose;
            base.OnClosing(e);
        }

        public new void Show()
        {
            base.Show();
            RefreshWindowRect(true);
            Debug.WriteLine(nameof(BlockingWindow) + ": claiming focus");
            Native.Methods.SetForegroundWindow(Handle.Handle);
        }

        public new void Hide()
        {
            base.Hide();
        }

        private async void DisplaysChanged()
        {
            await Task.Delay(WindowsDisplayAdapter.DisplayChangeProcessingDelay);
            RefreshWindowRect(true);
        }

        #region IDisposable support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                blockingWindowImpl.Dispose();
                disposedValue = true;
            }
        }

        ~BlockingWindow()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
