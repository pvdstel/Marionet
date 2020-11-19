﻿namespace Marionet.Core.Input
{
    public class MouseMoveEventArgs : InputEventArgs
    {
        public MouseMoveEventArgs(Point position, bool blocked) : base(blocked)
        {
            Position = position;
        }

        public Point Position { get; }
    }
}
