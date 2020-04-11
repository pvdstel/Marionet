using Marionet.Core.Communication;
using Marionet.Core.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.Core
{
    public partial class Workspace : IDisposable
    {
        private readonly IWorkspaceNetwork workspaceNetwork;
        private readonly IInputManager inputManager;
        private readonly IConfigurationProvider configurationProvider;
        private readonly string selfName;
        private readonly TaskCompletionSource<object?> initialized = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly SemaphoreSlim mutableStateLock = new SemaphoreSlim(1, 1);
        private Desktop selfDesktop = default!;
        private List<Desktop> desktops = default!;
        private DisplayLayout displayLayout = default!;
        private LocalState.State localState = default!;
        private Point localCursorPosition;
        private int mouseDeltaDebounceValueX;
        private int mouseDeltaDebounceValueY;

        public Workspace(WorkspaceSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this.workspaceNetwork = settings.WorkspaceNetwork ?? throw new NullReferenceException(nameof(settings) + "." + nameof(settings.WorkspaceNetwork) + " cannot be null.");
            this.inputManager = settings.InputManager ?? throw new NullReferenceException(nameof(settings) + "." + nameof(settings.InputManager) + " cannot be null.");
            this.configurationProvider = settings.ConfigurationProvider ?? throw new NullReferenceException(nameof(settings) + "." + nameof(settings.ConfigurationProvider) + " cannot be null.");
            this.selfName = configurationProvider.GetSelfName();

            workspaceNetwork.ClientConnected += OnClientConnected;
            workspaceNetwork.ClientDisconnected += OnClientDisconnected;
            workspaceNetwork.ClientDisplaysChanged += OnClientDisplaysChanged;
            workspaceNetwork.DesktopsChanged += OnDesktopsChanged;

            workspaceNetwork.ControlAssumed += OnControlAssumed;
            workspaceNetwork.ControlRelinquished += OnControlRelinquished;
            workspaceNetwork.ClientResignedFromControl += OnClientResignedFromControl;

            workspaceNetwork.MouseMoveReceived += OnMouseMoveReceived;
            workspaceNetwork.ControlledMouseMoveReceived += OnControlledMouseMoveReceived;
            workspaceNetwork.PressMouseButtonReceived += OnPressMouseButtonReceived;
            workspaceNetwork.ReleaseMouseButtonReceived += OnReleaseMouseButtonReceived;
            workspaceNetwork.MouseWheelReceived += OnMouseWheelReceived;
            workspaceNetwork.PressKeyboardButtonReceived += OnPressKeyboardButtonReceived;
            workspaceNetwork.ReleaseKeyboardButtonReceived += OnReleaseKeyboardButtonReceived;

            inputManager.SystemEvent += OnSystemEvent;
            inputManager.DisplayAdapter.DisplaysChanged += OnDisplaysChanged;
            inputManager.MouseListener.MouseMoved += OnMouseMoved;
            inputManager.MouseListener.MouseButtonPressed += OnMouseButtonPressed;
            inputManager.MouseListener.MouseButtonReleased += OnMouseButtonReleased;
            inputManager.MouseListener.MouseWheel += OnMouseWheel;
            inputManager.KeyboardListener.KeyboardButtonPressed += OnKeyboardButtonPressed;
            inputManager.KeyboardListener.KeyboardButtonReleased += OnKeyboardButtonReleased;
        }

        public async Task Initialize()
        {
            await inputManager.StartAsync();
            localCursorPosition = await inputManager.MouseListener.GetCursorPosition();

            await mutableStateLock.WaitAsync();
            selfDesktop = new Desktop() { Name = selfName, Displays = inputManager.DisplayAdapter.GetDisplays(), PrimaryDisplay = inputManager.DisplayAdapter.GetPrimaryDisplay() };
            mouseDeltaDebounceValueX = selfDesktop.PrimaryDisplay.Value.Width / 3;
            mouseDeltaDebounceValueY = selfDesktop.PrimaryDisplay.Value.Height / 3;
            desktops = new List<Desktop>() { selfDesktop };
            displayLayout = new DisplayLayout(desktops);

            var (_, display) = displayLayout.FindPoint(TranslateLocalToGlobal(localCursorPosition))!.Value;
            localState = new LocalState.Uncontrolled(display, selfDesktop.PrimaryDisplay.Value);
            mutableStateLock.Release();

            initialized.TrySetResult(null);
        }

        private async void OnControlAssumed(object sender, ClientConnectionChangedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            string desktopName = e.DesktopName.NormalizeDesktopName();

            if (localState is LocalState.Controlling controlling)
            {
                var client = await workspaceNetwork.GetClientDesktop(controlling.ActiveDesktop.Name);
                if (client != null)
                {
                    DebugMessage($"controlling {controlling.ActiveDesktop.Name}, relinquishing");
                    await client.RelinquishControl();
                }
            }

            HashSet<string> by;
            if (localState is LocalState.Controlled controlled)
            {
                by = new HashSet<string>(controlled.By);
            }
            else
            {
                by = new HashSet<string>();
            }
            DebugMessage($"adding {desktopName} to controlled by");
            by.Add(desktopName);
            localState = new LocalState.Controlled(by);
            DebugMessage("unblocking local input");
            inputManager.BlockInput(false);

            mutableStateLock.Release();
        }

        private async void OnControlRelinquished(object sender, ClientConnectionChangedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            string desktopName = e.DesktopName.NormalizeDesktopName();

            if (localState is LocalState.Controlled controlled)
            {
                HashSet<string> by = new HashSet<string>(controlled.By);
                by.Remove(desktopName);
                if (by.Count == 0)
                {
                    DebugMessage("no controllers left, becoming relinquished");
                    localState = new LocalState.Relinquished();
                    DebugMessage("blocking local input");
                    inputManager.BlockInput(true);
                }
                else
                {
                    DebugMessage("controllers remaining, not relinquished");
                    localState = new LocalState.Controlled(by);
                }
            }

            mutableStateLock.Release();
        }

        private async void OnClientResignedFromControl(object sender, ClientConnectionChangedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            string desktopName = e.DesktopName.NormalizeDesktopName();

            if (localState is LocalState.Controlling controlling && controlling.ActiveDesktop.Name == desktopName)
            {
                DebugMessage($"client {desktopName} has resigned from being controlled");
                await ReturnToPrimaryDisplay();
            }

            mutableStateLock.Release();
        }

        private async void OnSystemEvent(object sender, EventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            DebugMessage($"releasing all resources");
            await ReturnToPrimaryDisplay();

            mutableStateLock.Release();
        }

        private Point TranslateGlobalToLocal(Point globalPoint)
        {
            var localOrigin = displayLayout.DesktopOrigins[selfDesktop];
            return globalPoint.Offset(-localOrigin.X, -localOrigin.Y);
        }

        private Point TranslateLocalToGlobal(Point localPoint)
        {
            var localOrigin = displayLayout.DesktopOrigins[selfDesktop];
            return localPoint.Offset(localOrigin.X, localOrigin.Y);
        }

        private Task EnsureInitialized() => initialized.Task;

        [Conditional("DEBUG")]
        private static void DebugMessage(string message, [CallerMemberName] string source = "")
        {
            Debug.WriteLine($"{source}: {message}");
        }

        [Conditional("DEBUG")]
        private void DebugPrintDisplays([CallerMemberName] string source = "")
        {
            Debug.WriteLine(source);
            displayLayout.DisplayRectangles.ToList().ForEach(r => Debug.WriteLine($"\t{r}"));
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mutableStateLock.Dispose();

                    workspaceNetwork.ClientConnected -= OnClientConnected;
                    workspaceNetwork.ClientDisconnected -= OnClientDisconnected;
                    workspaceNetwork.ClientDisplaysChanged -= OnClientDisplaysChanged;
                    workspaceNetwork.DesktopsChanged -= OnDesktopsChanged;

                    workspaceNetwork.ControlAssumed -= OnControlAssumed;
                    workspaceNetwork.ControlRelinquished -= OnControlRelinquished;
                    workspaceNetwork.ClientResignedFromControl -= OnClientResignedFromControl;

                    workspaceNetwork.MouseMoveReceived -= OnMouseMoveReceived;
                    workspaceNetwork.ControlledMouseMoveReceived -= OnControlledMouseMoveReceived;
                    workspaceNetwork.PressMouseButtonReceived -= OnPressMouseButtonReceived;
                    workspaceNetwork.ReleaseMouseButtonReceived -= OnReleaseMouseButtonReceived;
                    workspaceNetwork.MouseWheelReceived -= OnMouseWheelReceived;
                    workspaceNetwork.PressKeyboardButtonReceived -= OnPressKeyboardButtonReceived;
                    workspaceNetwork.ReleaseKeyboardButtonReceived -= OnReleaseKeyboardButtonReceived;

                    inputManager.SystemEvent -= OnSystemEvent;
                    inputManager.DisplayAdapter.DisplaysChanged -= OnDisplaysChanged;
                    inputManager.MouseListener.MouseMoved -= OnMouseMoved;
                    inputManager.MouseListener.MouseButtonPressed -= OnMouseButtonPressed;
                    inputManager.MouseListener.MouseButtonReleased -= OnMouseButtonReleased;
                    inputManager.MouseListener.MouseWheel -= OnMouseWheel;
                    inputManager.KeyboardListener.KeyboardButtonPressed -= OnKeyboardButtonPressed;
                    inputManager.KeyboardListener.KeyboardButtonReleased -= OnKeyboardButtonReleased;
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
