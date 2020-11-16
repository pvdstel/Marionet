using Marionet.Core.Communication;
using Marionet.Core.Input;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.Core
{
    public partial class Workspace
    {
        private async void OnClientConnected(object? sender, ClientConnectionChangedEventArgs e)
        {
            await EnsureInitialized();

            string desktopName = e.DesktopName.NormalizeDesktopName();
            if (desktopName == selfName)
            {
                return;
            }

            await mutableStateLock.WaitAsync();

            LocalState.Controlling? controlling = localState as LocalState.Controlling;
            LocalState.Uncontrolled? uncontrolled = localState as LocalState.Uncontrolled;
            string? activeDisplayId = null;
            if (controlling != null)
            {
                activeDisplayId = displayLayout.DisplayIds[controlling.ActiveDisplay];
            }

            DebugMessage($"adding client {desktopName}");
            desktops.Add(new Desktop(desktopName, ImmutableList<Rectangle>.Empty, null));
            displayLayout = CreateDisplayLayout(configurationProvider.GetDesktopOrder());
            DebugPrintDisplays();

            if (controlling != null && activeDisplayId != null)
            {
                var nextDisplay = displayLayout.DisplayById[activeDisplayId];
                var displayOriginDeltaX = nextDisplay.X - controlling.ActiveDisplay.X;
                var displayOriginDeltaY = nextDisplay.Y - controlling.ActiveDisplay.Y;
                localState = new LocalState.Controlling(controlling.ActiveDesktop, nextDisplay, controlling.CursorPosition.Offset(displayOriginDeltaX, displayOriginDeltaY));
            }
            else if (uncontrolled != null)
            {
                localCursorPosition = await inputManager.MouseListener.GetCursorPosition();
                var (_, display) = displayLayout.FindPoint(TranslateLocalToGlobal(localCursorPosition))!.Value;
                localState = new LocalState.Uncontrolled(display, display);
                DebugMessage($"detected a global desktop change (connect), but state is uncontrolled; updating display to {display}");
            }

            mutableStateLock.Release();
        }

        private async void OnClientDisconnected(object? sender, ClientConnectionChangedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            string desktopName = e.DesktopName.NormalizeDesktopName();
            LocalState.Controlling? controlling = localState as LocalState.Controlling;
            LocalState.Uncontrolled? uncontrolled = localState as LocalState.Uncontrolled;
            string? activeDisplayId = null;
            if (controlling != null && controlling.ActiveDesktop.Name != desktopName)
            {
                activeDisplayId = displayLayout.DisplayIds[controlling.ActiveDisplay];
            }

            DebugMessage($"removing client {desktopName}");
            desktops = desktops.Where(d => d.Name != desktopName).ToList();
            displayLayout = new DisplayLayout(desktops);
            DebugPrintDisplays();

            if (controlling != null)
            {
                if (activeDisplayId != null)
                {
                    var nextDisplay = displayLayout.DisplayById[activeDisplayId];
                    DebugMessage($"moving from display {controlling.ActiveDisplay} to {nextDisplay}");
                    var displayOriginDeltaX = nextDisplay.X - controlling.ActiveDisplay.X;
                    var displayOriginDeltaY = nextDisplay.Y - controlling.ActiveDisplay.Y;
                    localState = new LocalState.Controlling(controlling.ActiveDesktop, nextDisplay, controlling.CursorPosition.Offset(displayOriginDeltaX, displayOriginDeltaY));
                }
                else
                {
                    DebugMessage($"active desktop disconnected. Returning to local primary display");
                    await ReturnToPrimaryDisplay();
                }
            }
            else if (uncontrolled != null)
            {
                localCursorPosition = await inputManager.MouseListener.GetCursorPosition();
                var (_, display) = displayLayout.FindPoint(TranslateLocalToGlobal(localCursorPosition))!.Value;
                localState = new LocalState.Uncontrolled(display, display);
                DebugMessage($"detected a global desktop change (disconnect), but state is uncontrolled; updating display to {display}");
            }

            mutableStateLock.Release();
        }

        private async void OnDesktopsChanged(object? sender, DesktopsChangedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();
            DebugMessage("desktops changed, recomputing");

            LocalState.Controlling? controlling = localState as LocalState.Controlling;
            LocalState.Uncontrolled? uncontrolled = localState as LocalState.Uncontrolled;
            string? activeDisplayId = null;
            if (controlling != null)
            {
                activeDisplayId = displayLayout.DisplayIds[controlling.ActiveDisplay];
            }

            DebugMessage("recreating display layout");
            displayLayout = CreateDisplayLayout(e.Desktops);
            DebugPrintDisplays();

            if (controlling != null && activeDisplayId != null)
            {
                var nextDisplay = displayLayout.DisplayById[activeDisplayId];
                DebugMessage($"moving from display {controlling.ActiveDisplay} to {nextDisplay}");
                var displayOriginDeltaX = nextDisplay.X - controlling.ActiveDisplay.X;
                var displayOriginDeltaY = nextDisplay.Y - controlling.ActiveDisplay.Y;
                localState = new LocalState.Controlling(controlling.ActiveDesktop, nextDisplay, controlling.CursorPosition.Offset(displayOriginDeltaX, displayOriginDeltaY));
            }
            else if (uncontrolled != null)
            {
                localCursorPosition = await inputManager.MouseListener.GetCursorPosition();
                var (_, display) = displayLayout.FindPoint(TranslateLocalToGlobal(localCursorPosition))!.Value;
                localState = new LocalState.Uncontrolled(display, display);
                DebugMessage($"detected a global desktop change (desktop change), but state is uncontrolled; updating display to {display}");
            }

            mutableStateLock.Release();
        }


        private async void OnClientDisplaysChanged(object? sender, ClientDisplaysChangedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            var desktopName = e.DesktopName.NormalizeDesktopName();
            var desktopIndex = desktops.FindIndex(d => d.Name == desktopName);
            if (desktopIndex >= 0)
            {
                LocalState.Controlling? controlling = localState as LocalState.Controlling;
                LocalState.Uncontrolled? uncontrolled = localState as LocalState.Uncontrolled;
                string? activeDisplayId = null;
                if (controlling != null)
                {
                    activeDisplayId = displayLayout.DisplayIds[controlling.ActiveDisplay];
                }

                DebugMessage($"displays for {desktopName} changed");
                var oldDesktop = desktops[desktopIndex];
                desktops[desktopIndex] = oldDesktop with { Displays = e.Displays };
                displayLayout = new DisplayLayout(desktops);
                DebugPrintDisplays();

                if (controlling != null && activeDisplayId != null)
                {
                    if (displayLayout.DisplayById.TryGetValue(activeDisplayId, out Rectangle nextDisplay))
                    {
                        DebugMessage($"moving from display {controlling.ActiveDisplay} to {nextDisplay}");
                        var displayOriginDeltaX = nextDisplay.X - controlling.ActiveDisplay.X;
                        var displayOriginDeltaY = nextDisplay.Y - controlling.ActiveDisplay.Y;
                        localState = new LocalState.Controlling(controlling.ActiveDesktop, nextDisplay, controlling.CursorPosition.Offset(displayOriginDeltaX, displayOriginDeltaY));
                    }
                    else
                    {
                        DebugMessage($"display {activeDisplayId} not found. Returning to local primary display");
                        await ReturnToPrimaryDisplay();
                    }
                }
                else if (uncontrolled != null)
                {
                    localCursorPosition = await inputManager.MouseListener.GetCursorPosition();
                    var (_, display) = displayLayout.FindPoint(TranslateLocalToGlobal(localCursorPosition))!.Value;
                    localState = new LocalState.Uncontrolled(display, display);
                    DebugMessage($"detected a global desktop change (client display change), but state is uncontrolled; updating display to {display}");
                }
            }

            mutableStateLock.Release();
        }

        private async void OnDisplaysChanged(object? sender, DisplaysChangedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            LocalState.Controlling? controlling = localState as LocalState.Controlling;
            string? activeDisplayId = null;
            LocalState.Uncontrolled? uncontrolled = localState as LocalState.Uncontrolled;
            if (controlling != null)
            {
                activeDisplayId = displayLayout.DisplayIds[controlling.ActiveDisplay];
            }

            DebugMessage("own displays changed");
            selfDesktop = selfDesktop with { Displays = e.Displays, PrimaryDisplay = e.PrimaryDisplay };
            mouseDeltaDebounceValueX = e.PrimaryDisplay.Width / 2;
            mouseDeltaDebounceValueY = e.PrimaryDisplay.Height / 2;
            displayLayout = new DisplayLayout(desktops);
            var allClients = await workspaceNetwork.GetAllClientDesktops();
            await allClients.DisplaysChanged(new List<Rectangle>(e.Displays));
            DebugPrintDisplays();

            if (controlling != null && activeDisplayId != null)
            {
                var nextDisplay = displayLayout.DisplayById[activeDisplayId];
                DebugMessage($"moving from display {controlling.ActiveDisplay} to {nextDisplay}");
                var displayOriginDeltaX = nextDisplay.X - controlling.ActiveDisplay.X;
                var displayOriginDeltaY = nextDisplay.Y - controlling.ActiveDisplay.Y;
                DebugMessage(controlling.CursorPosition.Offset(displayOriginDeltaX, displayOriginDeltaY).ToString());
                localState = new LocalState.Controlling(controlling.ActiveDesktop, nextDisplay, controlling.CursorPosition.Offset(displayOriginDeltaX, displayOriginDeltaY));
            }
            else if (uncontrolled != null)
            {
                localCursorPosition = await inputManager.MouseListener.GetCursorPosition();
                var (_, display) = displayLayout.FindPoint(TranslateLocalToGlobal(localCursorPosition))!.Value;
                localState = new LocalState.Uncontrolled(display, display);
                DebugMessage($"detected a global desktop change (local display change), but state is uncontrolled; updating display to {display}");
            }

            mutableStateLock.Release();
        }

        private async Task ReturnToPrimaryDisplay(string? releaseDesktopName = null)
        {
            if (releaseDesktopName != null)
            {
                var client = await workspaceNetwork.GetClientDesktop(releaseDesktopName);
                if (client != null)
                {
                    await client.RelinquishControl();
                }
            }

            var primaryDisplay = selfDesktop.PrimaryDisplay!.Value;
            DebugMessage("unblocking local input");
            await inputManager.BlockInput(false);
            DebugMessage($"moving to local display {primaryDisplay}");
            var localPoint = new Point(primaryDisplay.Width / 2, primaryDisplay.Height / 2);
            await inputManager.MouseController.MoveMouse(localPoint);
            localState = new LocalState.Uncontrolled(primaryDisplay, selfDesktop.PrimaryDisplay!.Value);
        }

        private DisplayLayout CreateDisplayLayout(List<string> desktopNames)
        {
            var desktopOrder = desktopNames.Select(d => d.NormalizeDesktopName()).ToList();
            var groupedDesktops = desktops.GroupBy(d => desktopOrder.Contains(d.Name)).ToDictionary(k => k.Key, k => k.ToList());
            desktops = groupedDesktops.ContainsKey(true) ? groupedDesktops[true].OrderBy(d => desktopOrder.IndexOf(d.Name)).ToList() : new List<Desktop>();
            if (groupedDesktops.ContainsKey(false))
            {
                desktops.AddRange(groupedDesktops[false]);
            }
            return new DisplayLayout(desktops);
        }
    }
}
