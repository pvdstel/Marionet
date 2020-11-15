using System;

namespace Marionet.Core
{
    public struct Rectangle : IEquatable<Rectangle>
    {
        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; set; }
        
        public int Y { get; set; }

        public int Width { get; set; }
        
        public int Height { get; set; }

        public int Left => X;

        public int Top => Y;

        public int Right => X + Width;

        public int Bottom => Y + Height;

        public Rectangle Offset(int byX, int byY) => new Rectangle(X + byX, Y + byY, Width, Height);

        public bool Contains(Point point) => point.X >= this.X && point.Y >= this.Y && point.X < this.Right && point.Y < this.Bottom;

        public Point Clamp(Point point) => new Point(Math.Max(X, Math.Min(Right - 1, point.X)), Math.Max(Y, Math.Min(Bottom - 1, point.Y)));

        public override bool Equals(object? obj)
        {
            return obj is Rectangle rectangle && Equals(rectangle);
        }

        public bool Equals(Rectangle other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public override string ToString()
        {
            return $"Rectangle (X = {X}, Y = {Y}, Width = {Width}, Height = {Height})";
        }

        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !(left == right);
        }
    }
}
