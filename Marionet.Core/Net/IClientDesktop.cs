using Marionet.Core.Input;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marionet.Core.Net
{
    public interface IClientDesktop : IMouseController, IKeyboardController
    {
        /// <summary>
        /// Sent from a server to a client it wishes to control.
        /// </summary>
        Task AssumeControl();

        /// <summary>
        /// Sent from a server to a client it wishes to no longer control.
        /// </summary>
        Task RelinquishControl();

        /// <summary>
        /// Sent from a client to a server that it no longer wishes to be controlled by.
        /// </summary>
        Task ResignFromControl();

        /// <summary>
        /// Sent from a server to several clients to notify that its displays have changed.
        /// </summary>
        /// <param name="displays">The new displays.</param>
        Task DisplaysChanged(List<Rectangle> displays);

        /// <summary>
        /// Sent from a client to a server that controls it to notify that its mouse position has changed.
        /// </summary>
        /// <param name="position">The new global mouse position.</param>
        Task ControlledMouseMove(Point position);
    }
}
