using System.Threading.Tasks;

namespace Marionet.Core
{
    public interface ISystemPersmissions
    {
        public Task<bool> IsAdmin();

        public Task<bool> HasUiAccess();
    }
}
