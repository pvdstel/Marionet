using Marionet.Core.Input;
using Marionet.Core.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Marionet.App.Configuration
{
    internal static class InputManagerSelector
    {
        public static IInputManager GetInputManager()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsInputManager();
            }
            throw new NotImplementedException("This operating system's input mechanism is not supported.");
        }
    }
}
