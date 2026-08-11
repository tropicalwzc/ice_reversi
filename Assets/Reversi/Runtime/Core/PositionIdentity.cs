using System;

namespace IceReversi.Core
{
    public readonly struct PositionIdentity : IEquatable<PositionIdentity>
    {
        public PositionIdentity(ulong blackOccupancy, ulong whiteOccupancy, PieceColor turn)
        {
            BlackOccupancy = blackOccupancy;
            WhiteOccupancy = whiteOccupancy;
            Turn = turn;
        }

        public ulong BlackOccupancy { get; }
        public ulong WhiteOccupancy { get; }
        public PieceColor Turn { get; }

        public bool Equals(PositionIdentity other)
        {
            return BlackOccupancy == other.BlackOccupancy &&
                WhiteOccupancy == other.WhiteOccupancy && Turn == other.Turn;
        }

        public override bool Equals(object obj) => obj is PositionIdentity other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var mixed = BlackOccupancy ^ RotateLeft(WhiteOccupancy, 23) ^ ((ulong)Turn * 0x9E3779B97F4A7C15UL);
                return (int)(mixed ^ (mixed >> 32));
            }
        }

        public static bool operator ==(PositionIdentity left, PositionIdentity right) => left.Equals(right);
        public static bool operator !=(PositionIdentity left, PositionIdentity right) => !left.Equals(right);

        private static ulong RotateLeft(ulong value, int count)
        {
            return (value << count) | (value >> (64 - count));
        }
    }
}
