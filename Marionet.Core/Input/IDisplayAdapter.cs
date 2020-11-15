using System;
using System.Collections.ObjectModel;

namespace Marionet.Core.Input
{
    public interface IDisplayAdapter
    {
        ReadOnlyCollection<Rectangle> GetDisplays();

        Rectangle GetPrimaryDisplay();

        event EventHandler<DisplaysChangedEventArgs> DisplaysChanged;
    }
}
