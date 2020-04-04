using Marionet.Core.Communication;
using Marionet.Core.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core
{
    public class WorkspaceSettings
    {
        public IWorkspaceNetwork? WorkspaceNetwork { get; set; }

        public IInputManager? InputManager { get; set; }

        public IConfigurationProvider? ConfigurationProvider { get; set; }
    }
}
