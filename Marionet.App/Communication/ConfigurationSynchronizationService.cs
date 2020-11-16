using Marionet.App.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    public class ConfigurationSynchronizationService : IDisposable
    {
        private readonly ConfigurationService configurationService;
        private readonly IHubContext<NetHub, INetClient> netHub;
        private readonly ILogger<ConfigurationSynchronizationService> logger;
        private bool suppressNextSend = false;
        private bool disposedValue;

        public ConfigurationSynchronizationService(
            ConfigurationService configurationService,
            IHubContext<NetHub, INetClient> netHub,
            ILogger<ConfigurationSynchronizationService> logger)
        {
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.netHub = netHub ?? throw new ArgumentNullException(nameof(netHub));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            configurationService.ConfigurationChanged += OnConfigurationChanged;
        }

        public async Task Receive(SynchronizedConfig synchronizedConfig)
        {
            logger.LogDebug("Received synchronized configuration object, suppressing next change");
            suppressNextSend = true;
            await configurationService.Update(configurationService.Configuration.ApplySynchronizedConfig(synchronizedConfig));
        }

        private async void OnConfigurationChanged(object? sender, EventArgs e)
        {
            if (suppressNextSend)
            {
                logger.LogDebug("Configuration changed but change was suppressed");
                suppressNextSend = false;
                return;
            }

            var syncConfig = configurationService.Configuration.ToSynchronizedConfig();
            logger.LogInformation("Sending synchronized configuration");
            await netHub.Clients.All.UpdateSynchronizedConfiguration(syncConfig);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    configurationService.ConfigurationChanged -= OnConfigurationChanged;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
