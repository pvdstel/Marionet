using Marionet.App.Configuration;
using Marionet.Core;
using Marionet.Core.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.App.Core
{
    public class WorkspaceSettingsService : WorkspaceSettings
    {
        public WorkspaceSettingsService(WorkspaceNetwork workspaceNetwork, IInputManager inputManager)
        {
            ConfigurationProvider = new ConfigurationProvider();
            InputManager = inputManager;
            WorkspaceNetwork = workspaceNetwork;
        }
    }
}
