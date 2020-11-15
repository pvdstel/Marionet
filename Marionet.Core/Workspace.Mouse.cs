using Marionet.Core.Communication;
using Marionet.Core.Input;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.Core
{
    public partial class Workspace
    {
        /// <summary>
        /// If the sticky cursor option is enabled, clamps the cursor to the current display if a sticky corner is hit.
        /// </summary>
        /// <param name="currentPosition">The current global cursor position.</param>
        /// <param name="nextPosition">The next global cursor position.</param>
        /// <param name="currentDisplay">The current display.</param>
        /// <returns>An optional point. If it has a value, that is the next cursor position clamped to the current display.</returns>
        private Point? GetStickyPosition(Point currentPosition, Point nextPosition, Rectangle currentDisplay)
        {
            int averageY = (currentPosition.Y + nextPosition.Y) / 2;
            int stickyCornerSize = configurationProvider.GetStickyCornerSize();

            if (stickyCornerSize > 0 && (averageY < currentDisplay.Top + stickyCornerSize || averageY > currentDisplay.Bottom - stickyCornerSize))
            {
                int x = Math.Max(currentDisplay.Left, Math.Min(currentDisplay.Right, nextPosition.X));
                int y = Math.Max(currentDisplay.Top, Math.Min(currentDisplay.Bottom, nextPosition.Y));
                return new Point(x, y);
            }

            return null;
        }

        /// <summary>
        /// If the block on button down option is enabled, clamps the cursor to the current desktop if a button is pressed.
        /// </summary>
        /// <param name="nextPosition">The next global cursor position.</param>
        /// <param name="currentDisplay">The currently active display.</param>
        /// <returns>An optional point. If it has a value, that is the next cursor position clamped to the current display.</returns>
        private Point? GetKeyButtonBlockPosition(Point nextPosition, Rectangle currentDisplay)
        {
            bool checkEnabled = configurationProvider.GetBlockTransferWhenButtonPressed();

            if (checkEnabled && (inputManager.KeyboardListener.IsAnyButtonPressed || inputManager.MouseListener.IsAnyButtonPressed))
            {
                int x = Math.Max(currentDisplay.Left, Math.Min(currentDisplay.Right, nextPosition.X));
                int y = Math.Max(currentDisplay.Top, Math.Min(currentDisplay.Bottom, nextPosition.Y));
                return new Point(x, y);
            }

            return null;
        }

        private async void OnMouseMoved(object? sender, MouseMoveEventArgs e)
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

        private async void OnMouseButtonPressed(object? sender, MouseButtonActionEventArgs e)
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

        private async void OnMouseButtonReleased(object? sender, MouseButtonActionEventArgs e)
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

        private async void OnMouseWheel(object? sender, MouseWheelEventArgs e)
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

        private async void OnMouseMoveReceived(object? sender, MouseMoveReceivedEventArgs e)
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

        private async void OnControlledMouseMoveReceived(object? sender, MouseMoveReceivedEventArgs e)
        {
            await EnsureInitialized();
            await mutableStateLock.WaitAsync();

            string desktopName = e.From.NormalizeDesktopName();
            if (localState is LocalState.Controlling controlling && controlling.ActiveDesktop.Name == desktopName)
            {
                DebugMessage($"controlled mouse moved to {e.Position}");
                localState = controlling with { CursorPosition = e.Position };
            }

            mutableStateLock.Release();
        }

        private async void OnPressMouseButtonReceived(object? sender, MouseButtonActionReceivedEventArgs e)
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

        private async void OnReleaseMouseButtonReceived(object? sender, MouseButtonActionReceivedEventArgs e)
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

        private async void OnMouseWheelReceived(object? sender, MouseWheelReceivedEventArgs e)
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
                        var stickyPosition = GetStickyPosition(nextGlobalPoint, nextGlobalPoint, uncontrolled.ActiveDisplay);
                        var keyButtonBlockPosition = GetKeyButtonBlockPosition(nextGlobalPoint, uncontrolled.ActiveDisplay);

                        if (!stickyPosition.HasValue && !keyButtonBlockPosition.HasValue)
                        {
                            DebugMessage("blocking local input");
                            await inputManager.BlockInput(true);
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
                    var stickyPoint = GetStickyPosition(controlling.CursorPosition, nextGlobalPoint, controlling.ActiveDisplay);

                    Desktop nextDesktop;
                    Rectangle nextDisplay;

                    DebugMessage($"sticky: {(stickyPoint.HasValue ? stickyPoint.Value.ToString() : "null")}");

                    if (stickyPoint.HasValue)
                    {
                        nextGlobalPoint = stickyPoint.Value;
                        nextDesktop = controlling.ActiveDesktop;
                        nextDisplay = controlling.ActiveDisplay;
                    }
                    else
                    {
                        (nextDesktop, nextDisplay) = next.Value;
                    }

                    if (nextDesktop != controlling.ActiveDesktop)
                    {
                        var keyButtonBlockPoint = GetKeyButtonBlockPosition(nextGlobalPoint, controlling.ActiveDisplay);

                        if (keyButtonBlockPoint.HasValue)
                        {
                            DebugMessage($"moving to display {keyButtonBlockPoint} on same desktop {nextDesktop} [blocked: button down]");
                            localState = new LocalState.Controlling(controlling.ActiveDesktop, controlling.ActiveDisplay, keyButtonBlockPoint.Value);
                            if (client != null)
                            {
                                await client.MoveMouse(keyButtonBlockPoint!.Value);
                            }
                        }
                        else
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
                                await inputManager.BlockInput(false);
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
                    DebugMessage($"moving to {clampedNextGlobalPoint} on {controlling.ActiveDesktop} [clamped]");
                    localState = controlling with { CursorPosition = clampedNextGlobalPoint };
                    if (client != null)
                    {
                        await client.MoveMouse(clampedNextGlobalPoint);
                    }
                }
            }
            else
            {
                DebugMessage($"moving to {nextGlobalPoint} on {controlling.ActiveDesktop}");
                localState = controlling with { CursorPosition = nextGlobalPoint };
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
                    await inputManager.BlockInput(true);
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
