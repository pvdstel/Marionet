using Marionet.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.App.Core
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public List<string> GetDesktopOrder() => Configuration.Config.Instance.Desktops;

        public string GetSelfName() => Configuration.Config.Instance.Self;
    }
}
