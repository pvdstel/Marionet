using System;

namespace Marionet.Core
{
    public struct Point : IEquatable<Point>
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public Point Offset(int byX, int byY) => new Point(X + byX, Y + byY);

        public override bool Equals(object obj)
        {
            return obj is Point point && Equals(point);
        }

        public bool Equals(Point other)
        {
            return X == other.X &&
                   Y == other.Y;
        }

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString()
        {
            return $"Point (X: {X}, Y: {Y})";
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }
    }
}
