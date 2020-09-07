using Marionet.Core.Communication;
using Marionet.Core.Input;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.Core
{
    public partial class Workspace
    {
        private (bool isSticky, Point? newPosition) GetStickyPosition(Point currentPosition, Point nextPosition, Rectangle currentDisplay)
        {
            int averageY = (currentPosition.Y + nextPosition.Y) / 2;
            int stickyCornerSize = configurationProvider.GetStickyCornerSize();

            if (averageY < currentDisplay.Top + stickyCornerSize || averageY > currentDisplay.Bottom - stickyCornerSize)
            {
                int x = Math.Max(currentDisplay.Left, Math.Min(currentDisplay.Right, currentPosition.X));
                int y = Math.Max(currentDisplay.Top, Math.Min(currentDisplay.Bottom, currentPosition.Y));
                return (true, new Point(x, y));
            }

            return (false, null);
        }

        private async void OnMouseMoved(object sender, MouseMoveEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            if (localState is LocalState.Uncontrolled uncontrolled)
            {
                await HandleUncontrolledMouse(uncontrolled, e);
            }
            else if (localState is LocalState.Controlling controlling)
            {
                await HandleControllingMouse(controlling, e);
            }
            else if (localState is LocalState.Controlled controlled)
            {
                await HandleControlledMouse(controlled, e);
            }
            else if (localState is LocalState.Relinquished)
            {
                await HandleRelinquishedMouse();
            }

            mutableStateLock.Release();
        }

        private async void OnMouseButtonPressed(object sender, MouseButtonActionEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            if (localState is LocalState.Controlling controlling)
            {
                var client = await workspaceNetwork.GetClientDesktop(controlling.ActiveDesktop.Name);
                if (client != null)
                {
                    await client.PressMouseButton(e.Button);
                }
            }

            mutableStateLock.Release();
        }

        private async void OnMouseButtonReleased(object sender, MouseButtonActionEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            if (localState is LocalState.Controlling controlling)
            {
                var client = await workspaceNetwork.GetClientDesktop(controlling.ActiveDesktop.Name);
                if (client != null)
                {
                    await client.ReleaseMouseButton(e.Button);
                }
            }
            else if (localState is LocalState.Relinquished)
            {
                await ReturnToPrimaryDisplay();
            }

            mutableStateLock.Release();
        }

        private async void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            if (localState is LocalState.Controlling controlling)
            {
                var client = await workspaceNetwork.GetClientDesktop(controlling.ActiveDesktop.Name);
                if (client != null)
                {
                    await client.Wheel(e.Delta, e.Direction);
                }
            }
            else if (localState is LocalState.Relinquished)
            {
                await ReturnToPrimaryDisplay();
            }

            mutableStateLock.Release();
        }

        private async void OnMouseMoveReceived(object sender, MouseMoveReceivedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();
            string desktopName = e.From.NormalizeDesktopName();
            if (localState is LocalState.Controlled controlled && controlled.By.Contains(desktopName))
            {
                var localPoint = TranslateGlobalToLocal(e.Position);
                await inputManager.MouseController.MoveMouse(localPoint);
            }
            mutableStateLock.Release();
        }

        private async void OnControlledMouseMoveReceived(object sender, MouseMoveReceivedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            string desktopName = e.From.NormalizeDesktopName();
            if (localState is LocalState.Controlling controlling && controlling.ActiveDesktop.Name == desktopName)
            {
                DebugMessage($"controlled mouse moved to {e.Position}");
                controlling.CursorPosition = e.Position;
            }

            mutableStateLock.Release();
        }

        private async void OnPressMouseButtonReceived(object sender, MouseButtonActionReceivedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();
            string desktopName = e.From.NormalizeDesktopName();
            if (localState is LocalState.Controlled controlled && controlled.By.Contains(desktopName))
            {
                DebugMessage($"{e.Button} from {e.From}");
                await inputManager.MouseController.PressMouseButton(e.Button);
            }
            mutableStateLock.Release();
        }

        private async void OnReleaseMouseButtonReceived(object sender, MouseButtonActionReceivedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();
            string desktopName = e.From.NormalizeDesktopName();
            if (localState is LocalState.Controlled controlled && controlled.By.Contains(desktopName))
            {
                DebugMessage($"{e.Button} from {e.From}");
                await inputManager.MouseController.ReleaseMouseButton(e.Button);
            }
            mutableStateLock.Release();
        }

        private async void OnMouseWheelReceived(object sender, MouseWheelReceivedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();
            string desktopName = e.From.NormalizeDesktopName();
            if (localState is LocalState.Controlled controlled && controlled.By.Contains(desktopName))
            {
                DebugMessage($"{e.Direction} {e.Delta} from {e.From}");
                await inputManager.MouseController.Wheel(e.Delta, e.Direction);
            }
            mutableStateLock.Release();
        }

        private async Task HandleUncontrolledMouse(LocalState.Uncontrolled uncontrolled, MouseMoveEventArgs e)
        {
            var nextGlobalPoint = TranslateLocalToGlobal(e.Position);
            if (!uncontrolled.ActiveDisplay.Contains(nextGlobalPoint))
            {
                var next = displayLayout.FindPoint(nextGlobalPoint);
                if (next.HasValue)
                {
                    var (nextDesktop, nextDisplay) = next.Value;
                    if (nextDesktop != selfDesktop)
                    {
                        var (isSticky, _) = GetStickyPosition(nextGlobalPoint, nextGlobalPoint, uncontrolled.ActiveDisplay);

                        if (!isSticky)
                        {
                            DebugMessage("blocking local input");
                            inputManager.BlockInput(true);
                            localCursorPosition = await inputManager.MouseListener.GetCursorPosition();
                            DebugMessage($"assuming control of {nextDesktop.Name} on display {nextDisplay} on position {nextGlobalPoint}");
                            localState = new LocalState.Controlling(nextDesktop, nextDisplay, nextGlobalPoint);
                            var nextClient = await workspaceNetwork.GetClientDesktop(nextDesktop.Name);
                            if (nextClient != null)
                            {
                                await nextClient.AssumeControl();
                                await nextClient.MoveMouse(nextGlobalPoint);
                            }
                        }
                    }
                    else
                    {
                        DebugMessage($"moved to display {nextDisplay}");
                        localState = new LocalState.Uncontrolled(nextDisplay, selfDesktop.PrimaryDisplay!.Value);
                    }
                }
            }
        }

        private async Task HandleControllingMouse(LocalState.Controlling controlling, MouseMoveEventArgs e)
        {
            var deltaX = e.Position.X - localCursorPosition.X;
            var deltaY = e.Position.Y - localCursorPosition.Y;
            if (!e.Blocked || Math.Abs(deltaX) > mouseDeltaDebounceValueX || Math.Abs(deltaY) > mouseDeltaDebounceValueY)
            {
                DebugMessage($"Mouse delta larger than debounce value ({deltaX} > {mouseDeltaDebounceValueX} || {deltaY} > {mouseDeltaDebounceValueY}).");
                return;
            }

            var nextGlobalPoint = controlling.CursorPosition.Offset(deltaX, deltaY);
            var client = await workspaceNetwork.GetClientDesktop(controlling.ActiveDesktop.Name);
            if (!controlling.ActiveDisplay.Contains(nextGlobalPoint))
            {
                var next = displayLayout.FindPoint(nextGlobalPoint);
                if (next.HasValue)
                {
                    var (isSticky, stickyPoint) = GetStickyPosition(controlling.CursorPosition, nextGlobalPoint, controlling.ActiveDisplay);
                    Desktop nextDesktop;
                    Rectangle nextDisplay;

                    DebugMessage($"sticky: {isSticky} @ {stickyPoint}");

                    if (isSticky)
                    {
                        // stickyPoint is guaranteed to have a value
                        nextGlobalPoint = stickyPoint!.Value;
                        nextDesktop = controlling.ActiveDesktop;
                        nextDisplay = controlling.ActiveDisplay;
                    }
                    else
                    {
                        (nextDesktop, nextDisplay) = next.Value;
                    }

                    if (nextDesktop != controlling.ActiveDesktop)
                    {
                        DebugMessage($"relinquishing {controlling.ActiveDesktop}");
                        if (client != null)
                        {
                            await client.RelinquishControl();
                        }
                        if (nextDesktop == selfDesktop)
                        {
                            DebugMessage($"moving to local display {nextDisplay}");
                            var localPoint = TranslateGlobalToLocal(nextGlobalPoint);
                            DebugMessage($"placing cursor at {localPoint} (global {nextGlobalPoint}");
                            await inputManager.MouseController.MoveMouse(localPoint);
                            await Task.Yield();
                            DebugMessage("unblocking local input");
                            inputManager.BlockInput(false);
                            localState = new LocalState.Uncontrolled(nextDisplay, selfDesktop.PrimaryDisplay!.Value);
                        }
                        else
                        {
                            localState = new LocalState.Controlling(nextDesktop, nextDisplay, nextGlobalPoint);
                            DebugMessage($"assuming control of {nextDesktop.Name}");
                            var nextClient = await workspaceNetwork.GetClientDesktop(nextDesktop.Name);
                            if (nextClient != null)
                            {
                                await nextClient.AssumeControl();
                                await nextClient.MoveMouse(nextGlobalPoint);
                            }
                        }
                    }
                    else
                    {
                        DebugMessage($"moving to display {nextDisplay} on same desktop {nextDesktop}");
                        localState = new LocalState.Controlling(nextDesktop, nextDisplay, nextGlobalPoint);
                        if (client != null)
                        {
                            await client.MoveMouse(nextGlobalPoint);
                        }
                    }
                }
                else
                {
                    var clampedNextGlobalPoint = controlling.ActiveDisplay.Clamp(nextGlobalPoint);
                    DebugMessage($"moving to {clampedNextGlobalPoint} on {controlling.ActiveDesktop}");
                    controlling.CursorPosition = clampedNextGlobalPoint;
                    if (client != null)
                    {
                        await client.MoveMouse(clampedNextGlobalPoint);
                    }
                }
            }
            else
            {
                DebugMessage($"moving to {nextGlobalPoint} on {controlling.ActiveDesktop}");
                controlling.CursorPosition = nextGlobalPoint;
                if (client != null)
                {
                    await client.MoveMouse(nextGlobalPoint);
                }
            }
        }

        private async Task HandleControlledMouse(LocalState.Controlled controlled, MouseMoveEventArgs e)
        {
            var nextGlobalPoint = TranslateLocalToGlobal(e.Position);
            if (!selfDesktop.Displays.Any(d => d.Contains(e.Position))) // selfDesktop.Displays uses local coordinates
            {
                var next = displayLayout.FindPoint(nextGlobalPoint);
                if (next.HasValue)
                {
                    var (nextDesktop, nextDisplay) = next.Value;
                    var controllers = await workspaceNetwork.GetClientDesktops(controlled.By);

                    DebugMessage("resigning from control");
                    await controllers.ResignFromControl();
                    DebugMessage("blocking local input");
                    inputManager.BlockInput(true);
                    localCursorPosition = await inputManager.MouseListener.GetCursorPosition();
                    DebugMessage($"assuming control of {nextDesktop.Name} on display {nextDisplay} on position {nextGlobalPoint}");
                    localState = new LocalState.Controlling(nextDesktop, nextDisplay, nextGlobalPoint);
                    var nextClient = await workspaceNetwork.GetClientDesktop(nextDesktop.Name);
                    if (nextClient != null)
                    {
                        await nextClient.AssumeControl();
                        await nextClient.MoveMouse(nextGlobalPoint);
                    }
                }
            }
            else
            {
                var controllers = await workspaceNetwork.GetClientDesktops(controlled.By);
                await controllers.ControlledMouseMove(nextGlobalPoint);
            }
        }

        private async Task HandleRelinquishedMouse()
        {
            await ReturnToPrimaryDisplay();
        }
    }
}
