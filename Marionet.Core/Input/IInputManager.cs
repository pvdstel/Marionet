using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Marionet.Core.Input
{
    public interface IInputManager : IDisposable
    {
        IKeyboardListener KeyboardListener { get; }

        IMouseListener MouseListener { get; }

        IDisplayAdapter DisplayAdapter { get; }

        IMouseController MouseController { get; }

        IKeyboardController KeyboardController { get; }

        event EventHandler SystemEvent;

        Task StartAsync();

        Task StopAsync();

        void BlockInput(bool blocked);
    }
}
