using Marionet.App.Configuration;
using Marionet.Core;
using System.Collections.Generic;

namespace Marionet.App.Core
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private readonly ConfigurationService configurationService;

        public ConfigurationProvider(ConfigurationService configurationService)
        {
            this.configurationService = configurationService;
        }

        public bool GetBlockTransferWhenButtonPressed() => configurationService.Configuration.BlockTransferWhenButtonPressed;

        public List<string> GetDesktopOrder() => configurationService.Configuration.Desktops;

        public string GetSelfName() => configurationService.Configuration.Self;

        public int GetStickyCornerSize() => configurationService.Configuration.StickyCornerSize;
    }
}
