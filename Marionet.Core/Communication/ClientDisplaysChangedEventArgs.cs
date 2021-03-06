﻿using System;
using System.Collections.Immutable;

namespace Marionet.Core.Communication
{
    public class ClientDisplaysChangedEventArgs : EventArgs
    {
        public ClientDisplaysChangedEventArgs(string desktopName, ImmutableList<Rectangle> displays)
        {
            DesktopName = desktopName;
            Displays = displays;
        }

        public string DesktopName { get; }

        public ImmutableList<Rectangle> Displays { get; }
    }
}
