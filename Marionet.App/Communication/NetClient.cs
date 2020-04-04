using Marionet.App.Core;
using Marionet.App.SignalR;
using Marionet.Core;
using Marionet.Core.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    public class NetClient : SignalRClient<NetHubInterface>, INetClient
    {
        private readonly WorkspaceNetwork workspaceNetwork;
        private TaskCompletionSource<object?> connectionTaskCompletionSource;
        private ILogger<NetClient> logger;

        private string? serverName;

        public NetClient(Uri uri, ILogger<NetClient> logger, IInputManager inputManager, WorkspaceNetwork workspaceNetwork) : base(uri, logger)
        {
            this.logger = logger;
            this.workspaceNetwork = workspaceNetwork;
            connectionTaskCompletionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.Disconnected += (sender, e) =>
            {
                connectionTaskCompletionSource.TrySetCanceled();
            };
            this.ConnectingStarted += (sender, e) =>
            {
                connectionTaskCompletionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            };
            this.Connected += async (sender, e) =>
            {
                var serverIdentity = await Hub.Identify(Configuration.Config.Instance.Self, Configuration.Config.Instance.Desktops);
                if (!serverIdentity.IsValid())
                {
                    throw new InvalidOperationException("The server provided invalid identification data.");
                }

                serverName = serverIdentity.DesktopName;
                await Configuration.Desktop.AddFromServer(serverIdentity.Desktops!);

                await Hub.ChangeDisplays(inputManager.DisplayAdapter.GetDisplays().ToList());
                connectionTaskCompletionSource.TrySetResult(null);
            };
        }

        [HubCallable]
        public async Task MoveMouse(Point position)
        {
            if (await WaitForName())
            {
                workspaceNetwork.MoveMouse(serverName!, position);
            }
        }

        [HubCallable]
        public async Task PressMouseButton(MouseButton button)
        {
            if (await WaitForName())
            {
                workspaceNetwork.PressMouseButton(serverName!, button);
            }
        }

        [HubCallable]
        public async Task ReleaseMouseButton(MouseButton button)
        {
            if (await WaitForName())
            {
                workspaceNetwork.ReleaseMouseButton(serverName!, button);
            }
        }

        [HubCallable]
        public async Task Wheel(int delta, MouseWheelDirection direction)
        {
            if (await WaitForName())
            {
                workspaceNetwork.Wheel(serverName!, delta, direction);
            }
        }

        [HubCallable]
        public async Task PressKeyboardButton(int keyCode)
        {
            if (await WaitForName())
            {
                workspaceNetwork.PressKeyboardButton(serverName!, keyCode);
            }
        }

        [HubCallable]
        public async Task ReleaseKeyboardButton(int keyCode)
        {
            if (await WaitForName())
            {
                workspaceNetwork.ReleaseKeyboardButton(serverName!, keyCode);
            }
        }

        [HubCallable]
        public async Task AssumeControl()
        {
            if (await WaitForName())
            {
                workspaceNetwork.AssumeControl(serverName!);
            }
        }

        [HubCallable]
        public async Task RelinquishControl()
        {
            if (await WaitForName())
            {
                workspaceNetwork.RelinquishControl(serverName!);
            }
        }

        [HubCallable]
        public async Task ResignFromControl()
        {
            if (await WaitForName())
            {
                workspaceNetwork.ResignFromControl(serverName!);
            }
        }

        [HubCallable]
        public async Task DisplaysChanged(List<Rectangle> displays)
        {
            if (await WaitForName())
            {
                workspaceNetwork.ChangeDisplays(serverName!, displays);
            }
        }

        [HubCallable]
        public async Task ControlledMouseMove(Point position)
        {
            if (await WaitForName())
            {
                workspaceNetwork.MoveControlledMouse(serverName!, position);
            }
        }

        [HubCallable]
        public async Task DesktopsUpdated(List<string> desktopNames)
        {
            await Configuration.Desktop.AddFromServer(desktopNames);
        }

        [HubCallable]
        public Task Debug()
        {
            StringBuilder debugData = new StringBuilder();
            debugData.AppendLine("Debug request received");
            if (connectionTaskCompletionSource.Task.IsCompleted)
            {
                debugData.AppendLine("\tThe server identification has completed.");
                debugData.AppendLine($"\tThe server name is {serverName}.");
            }
            if (connectionTaskCompletionSource.Task.IsCanceled)
            {
                debugData.AppendLine("\tThe server identification was canceled.");
            }
            logger.LogDebug(debugData.ToString());
            return Task.CompletedTask;
        }

        private async Task<bool> WaitForName()
        {
            try
            {
                await this.connectionTaskCompletionSource.Task;
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
