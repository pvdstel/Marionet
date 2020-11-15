namespace Marionet.Core.LocalState
{
    internal record Controlling(Desktop ActiveDesktop, Rectangle ActiveDisplay, Point CursorPosition) : State;
}
