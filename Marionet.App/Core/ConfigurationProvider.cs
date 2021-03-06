﻿using Marionet.App.Configuration;
using Marionet.Core;
using System.Collections.Immutable;

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

        public ImmutableList<string> GetDesktopOrder() => configurationService.Configuration.Desktops;

        public ImmutableDictionary<string, int> GetDesktopYOffsets() => configurationService.Configuration.DesktopYOffsets;

        public string GetSelfName() => configurationService.Configuration.Self;

        public int GetStickyCornerSize() => configurationService.Configuration.StickyCornerSize;

        public (int, int) GetTransferDistance() => (configurationService.Configuration.MinTransferDistance, configurationService.Configuration.MaxTransferDistance);
    }
}
