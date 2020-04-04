using Marionet.Core.Communication;
using Marionet.Core.Input;
using System;

namespace Marionet.Core
{
    public partial class Workspace
    {
        private async void OnKeyboardButtonPressed(object sender, KeyboardButtonActionEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            if (localState is LocalState.Controlling controlling)
            {
                var client = await workspaceNetwork.GetClientDesktop(controlling.ActiveDesktop.Name);
                if (client != null)
                {
                    await client.PressKeyboardButton(e.KeyCode);
                }
            }

            mutableStateLock.Release();
        }

        private async void OnKeyboardButtonReleased(object sender, KeyboardButtonActionEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            if (localState is LocalState.Controlling controlling)
            {
                var client = await workspaceNetwork.GetClientDesktop(controlling.ActiveDesktop.Name);
                if (client != null)
                {
                    await client.ReleaseKeyboardButton(e.KeyCode);
                }
            }
            else if (localState is LocalState.Relinquished)
            {
                await ReturnToPrimaryDisplay();
            }

            mutableStateLock.Release();
        }

        private async void OnPressKeyboardButtonReceived(object sender, KeyboardButtonActionReceivedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();
            string desktopName = e.From.NormalizeDesktopName();
            if (localState is LocalState.Controlled controlled && controlled.By.Contains(desktopName))
            {
                DebugMessage($"{e.KeyCode} from {e.From}");
                await inputManager.KeyboardController.PressKeyboardButton(e.KeyCode);
            }
            mutableStateLock.Release();
        }

        private async void OnReleaseKeyboardButtonReceived(object sender, KeyboardButtonActionReceivedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();
            string desktopName = e.From.NormalizeDesktopName();
            if (localState is LocalState.Controlled controlled && controlled.By.Contains(desktopName))
            {
                DebugMessage($"{e.KeyCode} from {e.From}");
                await inputManager.KeyboardController.ReleaseKeyboardButton(e.KeyCode);
            }
            mutableStateLock.Release();
        }
    }
}
