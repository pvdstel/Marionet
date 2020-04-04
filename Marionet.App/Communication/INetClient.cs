using Marionet.Core.Net;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    public interface INetClient : IClientDesktop
    {
        Task DesktopsUpdated(List<string> desktopNames);

        Task Debug();
    }
}
