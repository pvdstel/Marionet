using Marionet.App.Communication;
using Marionet.Core;
using Marionet.Core.Communication;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App.Core
{
    public class WorkspaceNetwork : IWorkspaceNetwork, IDisposable
    {
        private readonly IHubContext<NetHub, INetClient> netHub;
        private readonly ClientIdentifierService clientIdentifierService;
        private static CancellationTokenSource? onDesktopsChangedDebounce;
        private const int DesktopsChangedDebounceTimeout = 2500;

        public WorkspaceNetwork(
            IHubContext<NetHub, INetClient> netHub,
            ClientIdentifierService clientIdentifierService)
        {
            this.netHub = netHub;
            this.clientIdentifierService = clientIdentifierService;

            Configuration.Desktop.DesktopsChanged += OnDesktopsChanged;
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
            return netHub.Clients.Clients(nonNullConnectionIds.AsReadOnly());
        }

        internal void ConnectClient(string desktopName) => ClientConnected?.Invoke(this, new ClientConnectionChangedEventArgs(desktopName));
        internal void DisconnectClient(string desktopName) => ClientDisconnected?.Invoke(this, new ClientConnectionChangedEventArgs(desktopName));
        internal void ChangeDisplays(string desktopName, List<Rectangle> displays) => ClientDisplaysChanged?.Invoke(this, new ClientDisplaysChangedEventArgs(desktopName, displays));
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

        private async void OnDesktopsChanged(object? sender, EventArgs e)
        {
            if (onDesktopsChangedDebounce != null)
            {
                onDesktopsChangedDebounce.Cancel();
                onDesktopsChangedDebounce.Dispose();
            }

            onDesktopsChangedDebounce = new CancellationTokenSource();
            try
            {
                await Task.Delay(DesktopsChangedDebounceTimeout, onDesktopsChangedDebounce.Token);
                DesktopsChanged?.Invoke(this, new DesktopsChangedEventArgs(Configuration.Config.Instance.Desktops));
            }
            catch (TaskCanceledException) { }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Configuration.Desktop.DesktopsChanged -= OnDesktopsChanged;
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
