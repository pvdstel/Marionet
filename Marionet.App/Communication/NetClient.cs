using Marionet.App.Configuration;
using Marionet.App.Core;
using Marionet.App.SignalR;
using Marionet.Core;
using Marionet.Core.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    public class NetClient : SignalRClient<NetHubInterface>, INetClient
    {
        private readonly WorkspaceNetwork workspaceNetwork;
        private readonly ConfigurationSynchronizationService configurationSynchronizationService;
        private readonly ILogger<NetClient> logger;
        private TaskCompletionSource<object?> connectionTaskCompletionSource;

        private string? serverName;

        public NetClient(
            Uri uri,
            WorkspaceNetwork workspaceNetwork,
            ConfigurationService configurationService,
            ConfigurationSynchronizationService configurationSynchronizationService,
            ILogger<NetClient> logger,
            IInputManager inputManager,
            Supervisor supervisor
            ) : base(uri, configurationService, logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.workspaceNetwork = workspaceNetwork ?? throw new ArgumentNullException(nameof(workspaceNetwork));
            if (configurationService == null) throw new ArgumentNullException(nameof(configurationService));
            this.configurationSynchronizationService = configurationSynchronizationService ?? throw new ArgumentNullException(nameof(configurationSynchronizationService));

            connectionTaskCompletionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.Disconnected += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(serverName))
                {
                    supervisor.SetPeerStatus(serverName, Supervisor.PeerConnectionStatuses.IsServer, false);
                }
                connectionTaskCompletionSource.TrySetCanceled();
            };
            this.ConnectingStarted += (sender, e) =>
            {
                connectionTaskCompletionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            };
            this.Connected += async (sender, e) =>
            {
                var serverIdentity = await Hub.Identify(configurationService.Configuration.Self, configurationService.Configuration.Desktops);
                if (!serverIdentity.IsValid())
                {
                    throw new InvalidOperationException("The server provided invalid identification data.");
                }

                serverName = serverIdentity.DesktopName;
                await configurationService.DesktopManagement.AddFromServer(serverIdentity.Desktops!);

                await Hub.ChangeDisplays(inputManager.DisplayAdapter.GetDisplays().ToList());
                if (!string.IsNullOrEmpty(serverName))
                {
                    supervisor.SetPeerStatus(serverName, Supervisor.PeerConnectionStatuses.IsServer, true);
                }
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
                if (displays == null)
                {
                    logger.LogError($"Received a null list of displays from server {serverName}");
                    return;
                }

                workspaceNetwork.ChangeDisplays(serverName!, displays.ToImmutableList());
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
        public async Task UpdateSynchronizedConfiguration(SynchronizedConfig config)
        {
            await configurationSynchronizationService.Receive(config);
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
