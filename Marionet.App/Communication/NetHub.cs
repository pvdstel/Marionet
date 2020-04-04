using Marionet.App.Core;
using Marionet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    [Authorize]
    public class NetHub : Hub<INetClient>
    {
        private readonly ILogger<NetHub> logger;
        private readonly ClientIdentifierService clientIdentifierService;
        private readonly WorkspaceNetwork workspaceNetwork;

        public NetHub(ILogger<NetHub> logger, ClientIdentifierService clientIdentifierService, WorkspaceNetwork workspaceNetwork)
        {
            this.logger = logger;
            this.clientIdentifierService = clientIdentifierService;
            this.workspaceNetwork = workspaceNetwork;
        }

        public override Task OnConnectedAsync()
        {
            logger.LogInformation($"Desktop {Context.ConnectionId} has connected");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string? desktopName = await clientIdentifierService.GetDesktopName(Context.ConnectionId);
            if (desktopName != null)
            {
                await clientIdentifierService.Remove(Context.ConnectionId);
                workspaceNetwork.DisconnectClient(desktopName);
            }
            logger.LogInformation($"Connection {Context.ConnectionId} disconnected");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<IdentifyResult> Identify(string desktopName, List<string> knownNames)
        {
            if (!(await clientIdentifierService.KnowsConnection(Context.ConnectionId)))
            {
                await Configuration.Desktop.AddFromClient(knownNames ?? throw new ArgumentNullException(nameof(knownNames)));
                await clientIdentifierService.Add(Context.ConnectionId, desktopName);
                logger.LogDebug($"Desktop {desktopName} registered on connection {Context.ConnectionId}");
                workspaceNetwork.ConnectClient(desktopName);
            }

            return new IdentifyResult()
            {
                DesktopName = Configuration.Config.Instance.Self,
                Desktops = Configuration.Config.Instance.Desktops,
            };
        }

        public async Task ChangeDisplays(List<Rectangle> displays)
        {
            string? desktopName = await clientIdentifierService.GetDesktopName(Context.ConnectionId);
            if (desktopName != null)
            {
                workspaceNetwork.ChangeDisplays(desktopName, displays);
            }
        }
    }
}
