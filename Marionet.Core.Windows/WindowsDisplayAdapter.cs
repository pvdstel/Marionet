using Avalonia.Win32;
using Marionet.Core.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    internal class WindowsDisplayAdapter : IDisplayAdapter
    {
        internal const int DisplayChangeProcessingDelay = 1000;
        private readonly ChannelReader<Native.DisplayChannelMessage> displayChannelReader;
        private readonly ScreenImpl screenImpl = new ScreenImpl();

        public WindowsDisplayAdapter(ChannelReader<Native.DisplayChannelMessage> displayChannelReader)
        {
            this.displayChannelReader = displayChannelReader;
        }

        public event EventHandler<DisplaysChangedEventArgs>? DisplaysChanged;

        public ReadOnlyCollection<Rectangle> GetDisplays()
        {
            return screenImpl.AllScreens.Select(s => new Rectangle(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height)).ToList().AsReadOnly();
        }

        public Rectangle GetPrimaryDisplay()
        {
            var bounds = screenImpl.AllScreens.First(s => s.Primary).Bounds;
            return new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        public async void StartListener(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (await displayChannelReader.WaitToReadAsync(cancellationToken))
                    {
                        _ = await displayChannelReader.ReadAsync(cancellationToken);
                        await Task.Delay(DisplayChangeProcessingDelay, cancellationToken);
                        ProcessMessage();
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException) { }
        }

        private void ProcessMessage()
        {
            screenImpl.InvalidateScreensCache();
            DisplaysChanged?.Invoke(this, new DisplaysChangedEventArgs(GetDisplays(), GetPrimaryDisplay()));
        }
    }
}
