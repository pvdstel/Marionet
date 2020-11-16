using System;
using System.Collections.Generic;

namespace Marionet.App.Configuration
{
    public record SynchronizedConfig
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "It's an init-only set.")]
        public List<string> Desktops { get; init; } = new List<string>() { Environment.MachineName };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "It's an init-only set.")]
        public Dictionary<string, int> DesktopYOffsets { get; init; } = new Dictionary<string, int>();
    }
}
