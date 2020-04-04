using System;
using System.Collections.Generic;

namespace Marionet.Core.Input
{
    public interface IDisplayAdapter
    {
        List<Rectangle> GetDisplays();

        Rectangle GetPrimaryDisplay();

        event EventHandler<DisplaysChangedEventArgs> DisplaysChanged;
    }
}
