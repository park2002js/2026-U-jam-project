using System;

namespace UJam.Runtime.Grid
{
    public readonly struct GridDistance : IEquatable<GridDistance>
    {
        public GridDistance(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(GridDistance other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is GridDistance other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(GridDistance left, GridDistance right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridDistance left, GridDistance right)
        {
            return !left.Equals(right);
        }
    }
}
