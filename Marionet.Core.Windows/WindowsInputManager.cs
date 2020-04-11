using Marionet.Core.Input;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    public class WindowsInputManager : IInputManager, IInputBlocking
    {
        private ApplicationManager applicationManager;
        private readonly Channel<Native.MouseChannelMessage> mouseInputChannel;
        private readonly Channel<Native.KeyboardChannelMessage> keyboardInputChannel;
        private readonly Channel<Native.DisplayChannelMessage> displayInputChannel;
        private CancellationTokenSource? cancellationTokenSource;

        private readonly WindowsMouseListener mouseListener;
        private readonly WindowsKeyboardListener keyboardListener;
        private readonly WindowsDisplayAdapter displayAdapter;
        private readonly WindowsMouseController mouseController;
        private readonly WindowsKeyboardController keyboardController;
        private readonly InputSender inputSender;

        public WindowsInputManager()
        {
            mouseInputChannel = Channel.CreateUnbounded<Native.MouseChannelMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
            keyboardInputChannel = Channel.CreateUnbounded<Native.KeyboardChannelMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
            displayInputChannel = Channel.CreateUnbounded<Native.DisplayChannelMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });

            applicationManager = new ApplicationManager(mouseInputChannel.Writer, keyboardInputChannel.Writer, displayInputChannel.Writer, this, OnSystemEvent);
            mouseListener = new WindowsMouseListener(mouseInputChannel.Reader, this);
            keyboardListener = new WindowsKeyboardListener(keyboardInputChannel.Reader);
            displayAdapter = new WindowsDisplayAdapter(displayInputChannel.Reader);

            inputSender = new InputSender();
            mouseController = new WindowsMouseController(inputSender, displayAdapter);
            keyboardController = new WindowsKeyboardController(inputSender);
        }

        public event EventHandler? SystemEvent;

        public IKeyboardListener KeyboardListener => keyboardListener;

        public IMouseListener MouseListener => mouseListener;

        public IDisplayAdapter DisplayAdapter => displayAdapter;

        public IMouseController MouseController => mouseController;

        public IKeyboardController KeyboardController => keyboardController;

        public bool IsInputBlocked { get; private set; }

        public async Task StartAsync()
        {
            await StopAsync();
            await applicationManager.StartApplication();
            cancellationTokenSource = new CancellationTokenSource();
            mouseListener.StartListener(cancellationTokenSource.Token);
            keyboardListener.StartListener(cancellationTokenSource.Token);
            displayAdapter.StartListener(cancellationTokenSource.Token);
        }

        public Task StopAsync()
        {
            cancellationTokenSource?.Cancel();
            applicationManager.StopApplication();
            return Task.CompletedTask;
        }

        public void BlockInput(bool blocked)
        {
            IsInputBlocked = blocked;
            if (blocked)
            {
                applicationManager.ShowBlockingWindow();
            }
            else
            {
                applicationManager.HideBlockingWindow();
            }
        }

        private async void OnSystemEvent()
        {
            await Task.Yield();
            SystemEvent?.Invoke(this, new EventArgs());
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopAsync();
                    applicationManager.Dispose();
                    mouseController.Dispose();
                    keyboardController.Dispose();
                    inputSender.Dispose();
                    if (this.cancellationTokenSource != null)
                    {
                        this.cancellationTokenSource.Dispose();
                    }
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
