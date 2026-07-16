using System;

namespace UJam.Runtime.Grid
{
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public bool Equals(GridCell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(GridCell left, GridCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridCell left, GridCell right)
        {
            return !left.Equals(right);
        }
    }
}
