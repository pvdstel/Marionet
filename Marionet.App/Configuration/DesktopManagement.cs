﻿using Marionet.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.App.Configuration
{
    internal class DesktopManagement : IDisposable
    {
        private readonly ConfigurationService configurationService;
        private DesktopManagementState lastState = DesktopManagementState.Default;
        private bool disposedValue;

        public DesktopManagement(ConfigurationService configurationService)
        {
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            configurationService.ConfigurationChanged += OnConfigurationChanged;
        }

        public event EventHandler? DesktopsChanged;

        public async Task AddFromClient(ImmutableList<string> clientDesktopNames)
        {
            if (clientDesktopNames == null) throw new ArgumentNullException(nameof(clientDesktopNames));

            bool added = false;
            var desktops = new List<string>(configurationService.Configuration.Desktops);

            foreach (var n in clientDesktopNames)
            {
                if (!desktops.Any(d => string.Compare(d, n, StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    desktops.Add(n.NormalizeDesktopName());
                    added = true;
                }
            }

            if (added)
            {
                await configurationService.Update(configurationService.Configuration with { Desktops = desktops.ToImmutableList() });
                DesktopsChanged?.Invoke(this, new EventArgs());
            }
        }

        public async Task AddFromServer(ImmutableList<string> serverDesktopNames, ImmutableDictionary<string, int> desktopYOffsets)
        {
            if (serverDesktopNames == null) throw new ArgumentNullException(nameof(serverDesktopNames));
            if (desktopYOffsets == null) throw new ArgumentNullException(nameof(serverDesktopNames));

            var receivedState = new DesktopManagementState(serverDesktopNames, desktopYOffsets);

            if (!receivedState.Equals(lastState))
            {
                await configurationService.Update(configurationService.Configuration with { Desktops = serverDesktopNames, DesktopYOffsets = desktopYOffsets });
                DesktopsChanged?.Invoke(this, new EventArgs());
            }
        }

        private void OnConfigurationChanged(object? sender, EventArgs e)
        {
            var potentialState = new DesktopManagementState(configurationService.Configuration.Desktops, configurationService.Configuration.DesktopYOffsets);

            if (!lastState.Equals(potentialState))
            {
                lastState = potentialState;
                DesktopsChanged?.Invoke(null, new EventArgs());
            }
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
