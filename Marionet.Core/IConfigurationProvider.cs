using System.Collections.Immutable;

namespace Marionet.Core
{
    public interface IConfigurationProvider
    {
        string GetSelfName();

        ImmutableList<string> GetDesktopOrder();

        ImmutableDictionary<string, int> GetDesktopYOffsets();

        int GetStickyCornerSize();

        bool GetBlockTransferWhenButtonPressed();
    }
}
