using Marionet.Core;
using Marionet.Core.Communication;
using Marionet.Core.Input;

namespace Marionet.App.Core
{
    public class WorkspaceSettingsService : WorkspaceSettings
    {
        public WorkspaceSettingsService(IConfigurationProvider configurationProvider, IWorkspaceNetwork workspaceNetwork, IInputManager inputManager)
        {
            ConfigurationProvider = configurationProvider;
            InputManager = inputManager;
            WorkspaceNetwork = workspaceNetwork;
        }
    }
}
