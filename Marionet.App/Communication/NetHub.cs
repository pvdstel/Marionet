using Marionet.App.Configuration;
using Marionet.App.Core;
using Marionet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    [Authorize]
    public class NetHub : Hub<INetClient>
    {
        private readonly Supervisor supervisor;
        private readonly ConfigurationService configurationService;
        private readonly ClientIdentifierService clientIdentifierService;
        private readonly WorkspaceNetwork workspaceNetwork;
        private readonly ILogger<NetHub> logger;

        public NetHub(
            Supervisor supervisor,
            ConfigurationService configurationService,
            ClientIdentifierService clientIdentifierService,
            WorkspaceNetwork workspaceNetwork,
            ILogger<NetHub> logger
            )
        {
            this.supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.clientIdentifierService = clientIdentifierService ?? throw new ArgumentNullException(nameof(clientIdentifierService));
            this.workspaceNetwork = workspaceNetwork ?? throw new ArgumentNullException(nameof(workspaceNetwork));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task OnConnectedAsync()
        {
            logger.LogInformation($"Desktop {Context.ConnectionId} has connected");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string? desktopName = await clientIdentifierService.GetDesktopName(Context.ConnectionId);
            if (desktopName != null)
            {
                await clientIdentifierService.Remove(Context.ConnectionId);
                workspaceNetwork.DisconnectClient(desktopName);
                supervisor.SetPeerStatus(desktopName, Supervisor.PeerConnectionStatuses.IsClient, false);
            }
            logger.LogInformation($"Connection {Context.ConnectionId} disconnected");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<IdentifyResult> Identify(string desktopName, List<string> knownNames)
        {
            if (!(await clientIdentifierService.KnowsConnection(Context.ConnectionId)))
            {
                await configurationService.DesktopManagement.AddFromClient(knownNames ?? throw new ArgumentNullException(nameof(knownNames)));
                desktopName = desktopName.NormalizeDesktopName();
                await clientIdentifierService.Add(Context.ConnectionId, desktopName);
                logger.LogDebug($"Desktop {desktopName} registered on connection {Context.ConnectionId}");
                workspaceNetwork.ConnectClient(desktopName);
                supervisor.SetPeerStatus(desktopName, Supervisor.PeerConnectionStatuses.IsClient, true);
            }

            return new IdentifyResult()
            {
                DesktopName = configurationService.Configuration.Self,
                Desktops = configurationService.Configuration.Desktops,
            };
        }

        public async Task ChangeDisplays(List<Rectangle> displays)
        {
            string? desktopName = await clientIdentifierService.GetDesktopName(Context.ConnectionId);
            if (displays == null)
            {
                logger.LogError($"Received a null list of displays from client {desktopName}");
                return;
            }

            if (desktopName != null)
            {
                workspaceNetwork.ChangeDisplays(desktopName, displays.AsReadOnly());
            }
        }
    }
}
