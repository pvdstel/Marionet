using System.Collections.Immutable;

namespace Marionet.Core.Communication
{
    public class DesktopsChangedEventArgs
    {
        public DesktopsChangedEventArgs(ImmutableList<string> desktops)
        {
            Desktops = desktops ?? throw new System.ArgumentNullException(nameof(desktops));
        }

        public ImmutableList<string> Desktops { get; }
    }
}
