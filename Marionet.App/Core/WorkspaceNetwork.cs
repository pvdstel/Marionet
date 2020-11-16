using Marionet.App.Communication;
using Marionet.App.Configuration;
using Marionet.Core;
using Marionet.Core.Communication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App.Core
{
    public class WorkspaceNetwork : IWorkspaceNetwork, IDisposable
    {
        private readonly IHubContext<NetHub, INetClient> netHub;
        private readonly ClientIdentifierService clientIdentifierService;
        private readonly ConfigurationService configurationService;
        private readonly ILogger<WorkspaceNetwork> logger;
        private CancellationTokenSource? onDesktopsChangedDebounce;

        public WorkspaceNetwork(
            IHubContext<NetHub, INetClient> netHub,
            ClientIdentifierService clientIdentifierService,
            ConfigurationService configurationService,
            ILogger<WorkspaceNetwork> logger)
        {
            this.netHub = netHub ?? throw new ArgumentNullException(nameof(netHub));
            this.clientIdentifierService = clientIdentifierService ?? throw new ArgumentNullException(nameof(clientIdentifierService));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            configurationService.DesktopManagement.DesktopsChanged += OnDesktopsChanged;
        }

        public event EventHandler<ClientConnectionChangedEventArgs>? ClientConnected;
        public event EventHandler<ClientConnectionChangedEventArgs>? ClientDisconnected;
        public event EventHandler<DesktopsChangedEventArgs>? DesktopsChanged;
        public event EventHandler<ClientConnectionChangedEventArgs>? ControlAssumed;
        public event EventHandler<ClientConnectionChangedEventArgs>? ControlRelinquished;
        public event EventHandler<ClientConnectionChangedEventArgs>? ClientResignedFromControl;
        public event EventHandler<ClientDisplaysChangedEventArgs>? ClientDisplaysChanged;
        public event EventHandler<MouseMoveReceivedEventArgs>? ControlledMouseMoveReceived;
        public event EventHandler<MouseMoveReceivedEventArgs>? MouseMoveReceived;
        public event EventHandler<MouseButtonActionReceivedEventArgs>? PressMouseButtonReceived;
        public event EventHandler<MouseButtonActionReceivedEventArgs>? ReleaseMouseButtonReceived;
        public event EventHandler<MouseWheelReceivedEventArgs>? MouseWheelReceived;
        public event EventHandler<KeyboardButtonActionReceivedEventArgs>? PressKeyboardButtonReceived;
        public event EventHandler<KeyboardButtonActionReceivedEventArgs>? ReleaseKeyboardButtonReceived;

        public Task<IClientDesktop> GetAllClientDesktops()
        {
            return Task.FromResult<IClientDesktop>(netHub.Clients.All);
        }

        public async Task<IClientDesktop?> GetClientDesktop(string desktopName)
        {
            var connectionId = await clientIdentifierService.GetConnectionId(desktopName);
            if (connectionId == null)
            {
                return null;
            }

            return netHub.Clients.Client(connectionId);
        }
        public async Task<IClientDesktop> GetClientDesktops(IEnumerable<string> desktopNames)
        {
            var connectionIds = await Task.WhenAll(desktopNames.Select(d => clientIdentifierService.GetConnectionId(d)));
            List<string> nonNullConnectionIds = connectionIds.Where(d => d != null).ToList()!;
            return netHub.Clients.Clients(nonNullConnectionIds);
        }

        private void OnDesktopsChanged(object? sender, EventArgs e)
        {
            logger.LogDebug("Desktops updated -- triggering recompute in " + nameof(Workspace));
            DesktopsChanged?.Invoke(this, new DesktopsChangedEventArgs(configurationService.Configuration.Desktops.ToImmutableList()));
        }

        internal void ConnectClient(string desktopName) => ClientConnected?.Invoke(this, new ClientConnectionChangedEventArgs(desktopName));
        internal void DisconnectClient(string desktopName) => ClientDisconnected?.Invoke(this, new ClientConnectionChangedEventArgs(desktopName));
        internal void ChangeDisplays(string desktopName, ImmutableList<Rectangle> displays) => ClientDisplaysChanged?.Invoke(this, new ClientDisplaysChangedEventArgs(desktopName, displays));
        internal void AssumeControl(string desktopName) => ControlAssumed?.Invoke(this, new ClientConnectionChangedEventArgs(desktopName));
        internal void RelinquishControl(string desktopName) => ControlRelinquished?.Invoke(this, new ClientConnectionChangedEventArgs(desktopName));
        internal void ResignFromControl(string desktopName) => ClientResignedFromControl?.Invoke(this, new ClientConnectionChangedEventArgs(desktopName));
        internal void MoveMouse(string desktopName, Point position) => MouseMoveReceived?.Invoke(this, new MouseMoveReceivedEventArgs(desktopName, position));
        internal void MoveControlledMouse(string desktopName, Point position) => ControlledMouseMoveReceived?.Invoke(this, new MouseMoveReceivedEventArgs(desktopName, position));
        internal void PressMouseButton(string desktopName, Marionet.Core.Input.MouseButton mouseButton) => PressMouseButtonReceived?.Invoke(this, new MouseButtonActionReceivedEventArgs(desktopName, mouseButton));
        internal void ReleaseMouseButton(string desktopName, Marionet.Core.Input.MouseButton mouseButton) => ReleaseMouseButtonReceived?.Invoke(this, new MouseButtonActionReceivedEventArgs(desktopName, mouseButton));
        internal void Wheel(string desktopName, int delta, Marionet.Core.Input.MouseWheelDirection direction) => MouseWheelReceived?.Invoke(this, new MouseWheelReceivedEventArgs(desktopName, delta, direction));
        internal void PressKeyboardButton(string desktopName, int keyCode) => PressKeyboardButtonReceived?.Invoke(this, new KeyboardButtonActionReceivedEventArgs(desktopName, keyCode));
        internal void ReleaseKeyboardButton(string desktopName, int keyCode) => ReleaseKeyboardButtonReceived?.Invoke(this, new KeyboardButtonActionReceivedEventArgs(desktopName, keyCode));

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    configurationService.DesktopManagement.DesktopsChanged -= OnDesktopsChanged;

                    onDesktopsChangedDebounce?.Cancel();
                    onDesktopsChangedDebounce?.Dispose();
                    onDesktopsChangedDebounce = null;
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
