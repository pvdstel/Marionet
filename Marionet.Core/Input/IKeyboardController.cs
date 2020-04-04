using System.Threading.Tasks;

namespace Marionet.Core.Input
{
    public interface IKeyboardController
    {
        Task PressKeyboardButton(int keyCode);

        Task ReleaseKeyboardButton(int keyCode);
    }
}
