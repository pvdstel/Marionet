using Marionet.App.Core;
using Marionet.Core;
using Marionet.Core.Input;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Marionet.App.Communication
{
    public class WorkspaceClientManager : IDisposable
    {
        private readonly Dictionary<string, NetClient> clients = new Dictionary<string, NetClient>();
        private readonly ILogger<WorkspaceClientManager> logger;
        private readonly ILogger<NetClient> clientLogger;
        private readonly IInputManager inputManager;
        private readonly WorkspaceNetwork workspaceNetwork;
        private readonly IHubContext<NetHub, INetClient> netHub;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly Supervisor supervisor;
        private readonly SemaphoreSlim mutationLock = new SemaphoreSlim(1, 1);

        public WorkspaceClientManager(
            ILogger<WorkspaceClientManager> logger,
            ILogger<NetClient> clientLogger,
            IInputManager inputManager,
            WorkspaceNetwork workspaceNetwork,
            IHubContext<NetHub, INetClient> netHub,
            IHostApplicationLifetime hostApplicationLifetime,
            Supervisor supervisor)
        {
            this.logger = logger;
            this.clientLogger = clientLogger;
            this.inputManager = inputManager;
            this.workspaceNetwork = workspaceNetwork;
            this.netHub = netHub;
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.supervisor = supervisor;
        }

        public void Start()
        {
            mutationLock.Wait();
            foreach (var desktop in Configuration.Config.Instance.Desktops)
            {
                AddMachineClient(desktop);
            }
            mutationLock.Release();
            Configuration.Desktop.DesktopsChanged += OnDesktopsChanged;
        }

        public NetClient GetNetClient(string desktopName)
        {
            mutationLock.Wait();
            if (desktopName == null)
            {
                throw new ArgumentNullException(nameof(desktopName));
            }

            var client = clients[desktopName.NormalizeDesktopName()];
            mutationLock.Release();
            return client;
        }

        private void UpdateMachineClients()
        {
            mutationLock.Wait();

            HashSet<string> done = new HashSet<string>();
            foreach (var desktopName in Configuration.Config.Instance.Desktops)
            {
                if (!clients.ContainsKey(desktopName))
                {
                    AddMachineClient(desktopName);
                }
                done.Add(desktopName.NormalizeDesktopName());
            }

            foreach (var connectedName in clients.Keys)
            {
                if (!done.Contains(connectedName))
                {
                    logger.LogDebug($"Removing client for {connectedName}");
                    _ = clients[connectedName].Disconnect();
                    clients.Remove(connectedName);
                }
            }

            mutationLock.Release();
        }

        private void AddMachineClient(string desktopName)
        {
            if (string.Compare(desktopName, Configuration.Config.Instance.Self, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return;
            }

            var customHostSet = Configuration.Config.Instance.DesktopAddresses.TryGetValue(desktopName, out string? host);
            if (!customHostSet || host == null)
            {
                host = desktopName;
            }

            var uri = Utility.GetNetHubUri(host, Configuration.Config.ServerPort);
            logger.LogDebug($"Adding client for {uri}");
            NetClient client = new NetClient(uri, clientLogger, inputManager, workspaceNetwork, supervisor);
            clients.Add(desktopName.NormalizeDesktopName(), client);
            _ = client.Connect(hostApplicationLifetime.ApplicationStopping);
        }

        private async void OnDesktopsChanged(object? sender, EventArgs e)
        {
            await netHub.Clients.All.DesktopsUpdated(Configuration.Config.Instance.Desktops);
            UpdateMachineClients();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mutationLock.Dispose();
                    Configuration.Desktop.DesktopsChanged -= OnDesktopsChanged;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
