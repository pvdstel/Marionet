using System.Collections.Generic;

namespace Marionet.Core
{
    public interface IConfigurationProvider
    {
        string GetSelfName();

        List<string> GetDesktopOrder();

        int GetStickyCornerSize();

        bool GetBlockTransferWhenButtonPressed();
    }
}
