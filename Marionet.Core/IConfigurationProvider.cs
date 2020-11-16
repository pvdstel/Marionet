using System.Collections.Immutable;

namespace Marionet.Core
{
    public interface IConfigurationProvider
    {
        string GetSelfName();

        ImmutableList<string> GetDesktopOrder();

        int GetStickyCornerSize();

        bool GetBlockTransferWhenButtonPressed();
    }
}
