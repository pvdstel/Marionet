using Marionet.Core;
using Marionet.Core.Input;
using Marionet.Core.Net;
using Marionet.Core.Windows;
using System;
using System.Runtime.InteropServices;

namespace Marionet.App.Configuration
{
    public static class PlatformSelector
    {
        public static IInputManager GetInputManager()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsInputManager();
            }
            throw new NotImplementedException("This operating system's input mechanism is not supported.");
        }

        public static INetwork GetNetwork()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsNetwork();
            }
            throw new NotImplementedException("This operating system's network is not supported.");
        }

        public static ISystemPersmissions GetSystemPersmissions()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsSystemPermissions();
            }
            throw new NotImplementedException("This operating system's network is not supported.");
        }
    }
}
