using Marionet.Core.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    internal class WindowsDisplayAdapter : IDisplayAdapter
    {
        internal const int DisplayChangeProcessingDelay = 1000;
        private ChannelReader<Native.DisplayChannelMessage> displayChannelReader;

        public WindowsDisplayAdapter(ChannelReader<Native.DisplayChannelMessage> displayChannelReader)
        {
            this.displayChannelReader = displayChannelReader;
        }

        public event EventHandler<DisplaysChangedEventArgs>? DisplaysChanged;

        public List<Rectangle> GetDisplays()
        {
            return System.Windows.Forms.Screen.AllScreens.Select(s => new Rectangle(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height)).ToList();
        }

        public Rectangle GetPrimaryDisplay()
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
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
                        Native.DisplayChannelMessage message = await displayChannelReader.ReadAsync(cancellationToken);
                        await Task.Delay(DisplayChangeProcessingDelay, cancellationToken);
                        ProcessMessage(message);
                    }
                });
            }
            catch (OperationCanceledException) { }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReviewUnusedParameters", "CA1801:Remove unused parameter", Justification = "The message is not necessary.")]
        private void ProcessMessage(Native.DisplayChannelMessage message)
        {
            DisplaysChanged?.Invoke(this, new DisplaysChangedEventArgs(GetDisplays(), GetPrimaryDisplay()));
        }
    }
}
