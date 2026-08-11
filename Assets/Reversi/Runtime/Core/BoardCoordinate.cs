using System;

namespace IceReversi.Core
{
    public readonly struct BoardCoordinate : IEquatable<BoardCoordinate>, IComparable<BoardCoordinate>
    {
        public BoardCoordinate(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }
        public bool IsValid => Row >= 0 && Row < BoardState.Size && Column >= 0 && Column < BoardState.Size;

        public bool Equals(BoardCoordinate other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Column;
            }
        }

        public int CompareTo(BoardCoordinate other)
        {
            var rowComparison = Row.CompareTo(other.Row);
            return rowComparison != 0 ? rowComparison : Column.CompareTo(other.Column);
        }

        public override string ToString()
        {
            return $"({Row},{Column})";
        }

        public static bool operator ==(BoardCoordinate left, BoardCoordinate right) => left.Equals(right);
        public static bool operator !=(BoardCoordinate left, BoardCoordinate right) => !left.Equals(right);
    }
}
