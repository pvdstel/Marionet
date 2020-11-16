using Marionet.Core.Communication;
using Marionet.Core.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.Core
{
    public partial class Workspace : IDisposable
    {
        private const int InputManagerStartTimeout = 10000;

        private readonly IWorkspaceNetwork workspaceNetwork;
        private readonly IInputManager inputManager;
        private readonly IConfigurationProvider configurationProvider;
        private readonly string selfName;
        private readonly TaskCompletionSource<object?> initialized = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly SemaphoreSlim mutableStateLock = new SemaphoreSlim(1, 1);
        private Desktop selfDesktop = default!;
        private HashSet<Desktop> connectedDesktops = default!;
        private DisplayLayout displayLayout = default!;
        private LocalState.State localState = default!;
        private Point localCursorPosition;
        private Point mouseDeltaDebounceValue;

        public Workspace(WorkspaceSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            workspaceNetwork = settings.WorkspaceNetwork ?? throw new NullReferenceException(nameof(settings) + "." + nameof(settings.WorkspaceNetwork) + " cannot be null.");
            inputManager = settings.InputManager ?? throw new NullReferenceException(nameof(settings) + "." + nameof(settings.InputManager) + " cannot be null.");
            configurationProvider = settings.ConfigurationProvider ?? throw new NullReferenceException(nameof(settings) + "." + nameof(settings.ConfigurationProvider) + " cannot be null.");
            selfName = configurationProvider.GetSelfName();

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

            inputManager.DisplayAdapter.DisplaysChanged += OnDisplaysChanged;
            inputManager.MouseListener.MouseMoved += OnMouseMoved;
            inputManager.MouseListener.MouseButtonPressed += OnMouseButtonPressed;
            inputManager.MouseListener.MouseButtonReleased += OnMouseButtonReleased;
            inputManager.MouseListener.MouseWheel += OnMouseWheel;
            inputManager.KeyboardListener.KeyboardButtonPressed += OnKeyboardButtonPressed;
            inputManager.KeyboardListener.KeyboardButtonReleased += OnKeyboardButtonReleased;
            inputManager.SystemEvent += OnSystemEvent;
        }

        public async Task Initialize()
        {
            var inputManagerStartTask = inputManager.StartAsync();
            if (await Task.WhenAny(inputManagerStartTask, Task.Delay(InputManagerStartTimeout)) != inputManagerStartTask)
            {
                throw new TimeoutException($"The input manager instance was unable to start within the {InputManagerStartTimeout} ms timeout window.");
            }

            localCursorPosition = await inputManager.MouseListener.GetCursorPosition();

            await mutableStateLock.WaitAsync();
            var primaryDisplay = inputManager.DisplayAdapter.GetPrimaryDisplay();
            selfDesktop = new Desktop(selfName, inputManager.DisplayAdapter.GetDisplays(), primaryDisplay);
            mouseDeltaDebounceValue = new Point(primaryDisplay.Width / 3, primaryDisplay.Height / 3);
            connectedDesktops = new HashSet<Desktop>() { selfDesktop };
            displayLayout = new DisplayLayout(connectedDesktops, configurationProvider.GetDesktopYOffsets());

            var (_, display) = displayLayout.FindPoint(TranslateLocalToGlobal(localCursorPosition))!.Value;
            localState = new LocalState.Uncontrolled(display, primaryDisplay);
            mutableStateLock.Release();

            initialized.TrySetResult(null);
        }

        private async void OnSystemEvent(object? sender, EventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            if (localState is LocalState.Controlling)
            {
                await ReturnToPrimaryDisplay();
            }
            else if (localState is LocalState.Controlled controlled)
            {
                var nextGlobalPoint = TranslateLocalToGlobal(await inputManager.MouseListener.GetCursorPosition());
                var controllers = await workspaceNetwork.GetClientDesktops(controlled.By);
                await controllers.ControlledMouseMove(nextGlobalPoint);
            }

            mutableStateLock.Release();
        }

        private async void OnControlAssumed(object? sender, ClientConnectionChangedEventArgs e)
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

            DebugMessage($"adding {desktopName} to controlled by");
            ImmutableHashSet<string> nextBy = localState is LocalState.Controlled controlled 
                ? controlled.By.Add(desktopName)
                : ImmutableHashSet<string>.Empty.Add(desktopName);
            localState = new LocalState.Controlled(nextBy);
            DebugMessage("unblocking local input");
            await inputManager.BlockInput(false);

            mutableStateLock.Release();
        }

        private async void OnControlRelinquished(object? sender, ClientConnectionChangedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            string desktopName = e.DesktopName.NormalizeDesktopName();

            if (localState is LocalState.Controlled controlled)
            {
                ImmutableHashSet<string> by = controlled.By.Remove(desktopName);
                if (by.Count == 0)
                {
                    DebugMessage("no controllers left, becoming relinquished");
                    localState = new LocalState.Relinquished();
                    DebugMessage("blocking local input");
                    await inputManager.BlockInput(true);
                }
                else
                {
                    DebugMessage("controllers remaining, not relinquished");
                    localState = new LocalState.Controlled(by);
                }
            }

            mutableStateLock.Release();
        }

        private async void OnClientResignedFromControl(object? sender, ClientConnectionChangedEventArgs e)
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

                    inputManager.DisplayAdapter.DisplaysChanged -= OnDisplaysChanged;
                    inputManager.MouseListener.MouseMoved -= OnMouseMoved;
                    inputManager.MouseListener.MouseButtonPressed -= OnMouseButtonPressed;
                    inputManager.MouseListener.MouseButtonReleased -= OnMouseButtonReleased;
                    inputManager.MouseListener.MouseWheel -= OnMouseWheel;
                    inputManager.KeyboardListener.KeyboardButtonPressed -= OnKeyboardButtonPressed;
                    inputManager.KeyboardListener.KeyboardButtonReleased -= OnKeyboardButtonReleased;
                    inputManager.SystemEvent -= OnSystemEvent;

                    // Must happen after events are unsubscribed; they might still trigger otherwise.
                    mutableStateLock.Dispose();
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
