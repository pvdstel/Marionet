using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marionet.Core.Communication
{
    public interface IWorkspaceNetwork
    {
        Task<IClientDesktop> GetAllClientDesktops();

        Task<IClientDesktop?> GetClientDesktop(string desktopName);

        Task<IClientDesktop> GetClientDesktops(IEnumerable<string> desktopNames);

        event EventHandler<ClientConnectionChangedEventArgs> ClientConnected;

        event EventHandler<ClientConnectionChangedEventArgs> ClientDisconnected;

        event EventHandler<ClientDisplaysChangedEventArgs> ClientDisplaysChanged;

        event EventHandler<DesktopsChangedEventArgs> DesktopsChanged;

        event EventHandler<ClientConnectionChangedEventArgs> ControlAssumed;

        event EventHandler<ClientConnectionChangedEventArgs> ControlRelinquished;

        event EventHandler<ClientConnectionChangedEventArgs> ClientResignedFromControl;

        event EventHandler<MouseMoveReceivedEventArgs> MouseMoveReceived;

        event EventHandler<MouseMoveReceivedEventArgs> ControlledMouseMoveReceived;

        event EventHandler<MouseButtonActionReceivedEventArgs> PressMouseButtonReceived;

        event EventHandler<MouseButtonActionReceivedEventArgs> ReleaseMouseButtonReceived;

        event EventHandler<MouseWheelReceivedEventArgs> MouseWheelReceived;

        event EventHandler<KeyboardButtonActionReceivedEventArgs> PressKeyboardButtonReceived;

        event EventHandler<KeyboardButtonActionReceivedEventArgs> ReleaseKeyboardButtonReceived;
    }
}
