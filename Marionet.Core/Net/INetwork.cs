using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marionet.Core.Net
{
    public interface INetwork
    {
        public Task<List<WirelessNetworkInterface>> GetWirelessNetworkInterfaces();
    }
}
