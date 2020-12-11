using Marionet.App.Configuration;
using Marionet.App.Core;
using Marionet.Core;
using Marionet.Core.Input;
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
        private readonly Supervisor supervisor;
        private readonly ConfigurationService configurationService;
        private readonly ConfigurationSynchronizationService configurationSynchronizationService;
        private readonly WorkspaceNetwork workspaceNetwork;
        private readonly IInputManager inputManager;
        private readonly ILogger<WorkspaceClientManager> logger;
        private readonly ILogger<NetClient> clientLogger;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly SemaphoreSlim mutationLock = new SemaphoreSlim(1, 1);

        public WorkspaceClientManager(
            Supervisor supervisor,
            ConfigurationService configurationService,
            ConfigurationSynchronizationService configurationSynchronizationService,
            WorkspaceNetwork workspaceNetwork,
            IInputManager inputManager,
            ILogger<WorkspaceClientManager> logger,
            ILogger<NetClient> clientLogger,
            IHostApplicationLifetime hostApplicationLifetime
            )
        {
            this.supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.configurationSynchronizationService = configurationSynchronizationService ?? throw new ArgumentNullException(nameof(configurationSynchronizationService));
            this.workspaceNetwork = workspaceNetwork ?? throw new ArgumentNullException(nameof(workspaceNetwork));
            this.inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clientLogger = clientLogger ?? throw new ArgumentNullException(nameof(clientLogger));
            this.hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));

            configurationService.DesktopManagement.DesktopsChanged += OnDesktopsChanged;
        }

        public void Start()
        {
            logger.LogDebug("Started");
            mutationLock.Wait();
            foreach (var desktop in configurationService.Configuration.Desktops)
            {
                AddMachineClient(desktop);
            }
            mutationLock.Release();
            logger.LogDebug("Start completed");
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
            foreach (var desktopName in configurationService.Configuration.Desktops)
            {
                if (!clients.ContainsKey(desktopName))
                {
                    AddMachineClient(desktopName);
                }
                var normalizedName = desktopName.NormalizeDesktopName();
                done.Add(normalizedName);
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
            if (string.Compare(desktopName, configurationService.Configuration.Self, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                logger.LogDebug($"Ignored desktop {desktopName} -- it is the current desktop");
                return;
            }

            var customHostSet = configurationService.Configuration.DesktopAddresses.TryGetValue(desktopName, out var hosts);
            if (!customHostSet || hosts == null)
            {
                hosts = System.Collections.Immutable.ImmutableList<string>.Empty.Add(desktopName);
            }

            logger.LogDebug($"Adding client for {desktopName} ({string.Join(", ", hosts)}) for {desktopName}");

            NetClient client = new NetClient(desktopName, hosts, workspaceNetwork, configurationService, configurationSynchronizationService, clientLogger, inputManager, supervisor);
            clients.Add(desktopName.NormalizeDesktopName(), client);
            _ = client.Connect(hostApplicationLifetime.ApplicationStopping);
        }

        private void OnDesktopsChanged(object? sender, EventArgs e)
        {
            logger.LogDebug("Desktops updated -- checking for clients to add and remove");
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
                    configurationService.DesktopManagement.DesktopsChanged -= OnDesktopsChanged;
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
