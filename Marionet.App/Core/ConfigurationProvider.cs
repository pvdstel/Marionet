﻿using Marionet.Core;
using System.Collections.Generic;

namespace Marionet.App.Core
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public List<string> GetDesktopOrder() => Configuration.Config.Instance.Desktops;

        public string GetSelfName() => Configuration.Config.Instance.Self;

        public int GetStickyCornerSize() => Configuration.Config.Instance.StickyCornerSize;
    }
}
