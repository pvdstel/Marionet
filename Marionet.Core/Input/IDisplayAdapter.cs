using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Marionet.Core.Input
{
    public interface IDisplayAdapter
    {
        ImmutableList<Rectangle> GetDisplays();

        Rectangle GetPrimaryDisplay();

        event EventHandler<DisplaysChangedEventArgs> DisplaysChanged;
    }
}
