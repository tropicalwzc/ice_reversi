using System;
using System.Collections.Generic;
using System.Text;

namespace IceReversi.Core
{
    public sealed class BoardState : IEquatable<BoardState>
    {
        public const int Size = 8;
        private readonly PieceColor[] cells;
        private readonly ulong blackOccupancy;
        private readonly ulong whiteOccupancy;

        public BoardState()
            : this(new PieceColor[Size * Size], false)
        {
        }

        private BoardState(PieceColor[] source, bool clone)
        {
            cells = clone ? (PieceColor[])source.Clone() : source;
            for (var index = 0; index < cells.Length; index++)
            {
                var bit = 1UL << index;
                if (cells[index] == PieceColor.Black)
                {
                    blackOccupancy |= bit;
                }
                else if (cells[index] == PieceColor.White)
                {
                    whiteOccupancy |= bit;
                }
            }
        }

        public ulong BlackOccupancy => blackOccupancy;
        public ulong WhiteOccupancy => whiteOccupancy;
        public int EmptyCount => Size * Size - Count(PieceColor.Black) - Count(PieceColor.White);

        public PositionIdentity Identity(PieceColor turn)
        {
            if (!turn.IsPlayer())
            {
                throw new ArgumentException("Position turn must be Black or White.", nameof(turn));
            }

            return new PositionIdentity(blackOccupancy, whiteOccupancy, turn);
        }

        public PieceColor this[int row, int column]
        {
            get
            {
                ValidateCoordinate(row, column);
                return cells[(row * Size) + column];
            }
        }

        public PieceColor this[BoardCoordinate coordinate] => this[coordinate.Row, coordinate.Column];

        public static BoardState CreateStandardOpening()
        {
            var cells = new PieceColor[Size * Size];
            cells[(3 * Size) + 3] = PieceColor.White;
            cells[(3 * Size) + 4] = PieceColor.Black;
            cells[(4 * Size) + 3] = PieceColor.Black;
            cells[(4 * Size) + 4] = PieceColor.White;
            return new BoardState(cells, false);
        }

        public static BoardState FromRows(params string[] rows)
        {
            if (rows == null || rows.Length != Size)
            {
                throw new ArgumentException($"A board requires exactly {Size} rows.", nameof(rows));
            }

            var cells = new PieceColor[Size * Size];
            for (var row = 0; row < Size; row++)
            {
                if (rows[row] == null || rows[row].Length != Size)
                {
                    throw new ArgumentException($"Row {row} must contain exactly {Size} cells.", nameof(rows));
                }

                for (var column = 0; column < Size; column++)
                {
                    cells[(row * Size) + column] = ParseCell(rows[row][column]);
                }
            }

            return new BoardState(cells, false);
        }

        public int Count(PieceColor color)
        {
            var count = 0;
            for (var index = 0; index < cells.Length; index++)
            {
                if (cells[index] == color)
                {
                    count++;
                }
            }

            return count;
        }

        public IEnumerable<BoardCoordinate> OccupiedCoordinates()
        {
            for (var row = 0; row < Size; row++)
            {
                for (var column = 0; column < Size; column++)
                {
                    if (this[row, column] != PieceColor.Empty)
                    {
                        yield return new BoardCoordinate(row, column);
                    }
                }
            }
        }

        internal BoardState WithMove(BoardCoordinate move, PieceColor color, IReadOnlyList<BoardCoordinate> flips)
        {
            var next = (PieceColor[])cells.Clone();
            next[(move.Row * Size) + move.Column] = color;
            for (var index = 0; index < flips.Count; index++)
            {
                var flip = flips[index];
                next[(flip.Row * Size) + flip.Column] = color;
            }

            return new BoardState(next, false);
        }

        public bool Equals(BoardState other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            for (var index = 0; index < cells.Length; index++)
            {
                if (cells[index] != other.cells[index])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                for (var index = 0; index < cells.Length; index++)
                {
                    hash = (hash * 31) + (int)cells[index];
                }

                return hash;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder((Size + 1) * Size);
            for (var row = 0; row < Size; row++)
            {
                for (var column = 0; column < Size; column++)
                {
                    var cell = this[row, column];
                    builder.Append(cell == PieceColor.Black ? 'B' : cell == PieceColor.White ? 'W' : '.');
                }

                if (row < Size - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static PieceColor ParseCell(char value)
        {
            switch (value)
            {
                case 'B':
                case 'b':
                case 'X':
                case 'x':
                    return PieceColor.Black;
                case 'W':
                case 'w':
                case 'O':
                case 'o':
                    return PieceColor.White;
                case '.':
                case '-':
                case '_':
                    return PieceColor.Empty;
                default:
                    throw new ArgumentException($"Unsupported board cell '{value}'.");
            }
        }

        private static void ValidateCoordinate(int row, int column)
        {
            if (row < 0 || row >= Size || column < 0 || column >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(row), $"Coordinate ({row},{column}) is outside the board.");
            }
        }
    }
}
