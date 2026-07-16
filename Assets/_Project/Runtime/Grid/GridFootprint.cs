using System;

namespace UJam.Runtime.Grid
{
    public readonly struct GridFootprint : IEquatable<GridFootprint>
    {
        public GridFootprint(int width, int height, int rotationQuarterTurns)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (rotationQuarterTurns < 0 || rotationQuarterTurns > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(rotationQuarterTurns));
            }

            Width = width;
            Height = height;
            RotationQuarterTurns = rotationQuarterTurns;
        }

        public int Width { get; }

        public int Height { get; }

        public int RotationQuarterTurns { get; }

        public bool Equals(GridFootprint other)
        {
            return Width == other.Width
                && Height == other.Height
                && RotationQuarterTurns == other.RotationQuarterTurns;
        }

        public override bool Equals(object obj)
        {
            return obj is GridFootprint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Width;
                hash = (hash * 397) ^ Height;
                return (hash * 397) ^ RotationQuarterTurns;
            }
        }

        public static bool operator ==(GridFootprint left, GridFootprint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridFootprint left, GridFootprint right)
        {
            return !left.Equals(right);
        }
    }
}
