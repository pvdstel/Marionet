using Marionet.App.Configuration;
using Marionet.Core.Communication;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    public interface INetClient : IClientDesktop
    {
        Task UpdateSynchronizedConfiguration(SynchronizedConfig config);

        Task Debug();
    }
}
