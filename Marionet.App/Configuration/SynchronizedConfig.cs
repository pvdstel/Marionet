using System;
using System.Collections.Immutable;

namespace Marionet.App.Configuration
{
    public record SynchronizedConfig
    {
        public ImmutableList<string> Desktops { get; init; } = ImmutableList<string>.Empty.Add(Environment.MachineName);

        public ImmutableDictionary<string, int> DesktopYOffsets { get; init; } = ImmutableDictionary<string, int>.Empty;
    }
}
