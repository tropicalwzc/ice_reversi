using System;
using System.Collections.Generic;

namespace IceReversi.Core
{
    public static class ReversiRules
    {
        private static readonly (int row, int column)[] Directions =
        {
            (-1, -1), (-1, 0), (-1, 1),
            (0, -1),             (0, 1),
            (1, -1),  (1, 0),   (1, 1)
        };

        public static IReadOnlyList<BoardCoordinate> GetLegalMoves(BoardState board, PieceColor color)
        {
            ValidatePlayer(color);
            var moves = new List<BoardCoordinate>();
            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    var coordinate = new BoardCoordinate(row, column);
                    if (board[coordinate] == PieceColor.Empty && GetFlips(board, coordinate, color).Count > 0)
                    {
                        moves.Add(coordinate);
                    }
                }
            }

            return moves;
        }

        public static IReadOnlyList<BoardCoordinate> GetFlips(BoardState board, BoardCoordinate move, PieceColor color)
        {
            ValidatePlayer(color);
            if (!move.IsValid || board[move] != PieceColor.Empty)
            {
                return Array.Empty<BoardCoordinate>();
            }

            var allFlips = new List<BoardCoordinate>();
            var opponent = color.Opponent();
            for (var directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                var direction = Directions[directionIndex];
                var row = move.Row + direction.row;
                var column = move.Column + direction.column;
                var line = new List<BoardCoordinate>();

                while (IsInside(row, column) && board[row, column] == opponent)
                {
                    line.Add(new BoardCoordinate(row, column));
                    row += direction.row;
                    column += direction.column;
                }

                if (line.Count > 0 && IsInside(row, column) && board[row, column] == color)
                {
                    allFlips.AddRange(line);
                }
            }

            return allFlips;
        }

        public static bool IsLegalMove(BoardState board, BoardCoordinate move, PieceColor color)
        {
            return GetFlips(board, move, color).Count > 0;
        }

        public static BoardState ApplyMove(
            BoardState board,
            BoardCoordinate move,
            PieceColor color,
            out IReadOnlyList<BoardCoordinate> flips)
        {
            flips = GetFlips(board, move, color);
            if (flips.Count == 0)
            {
                throw new InvalidOperationException($"{move} is not a legal {color} move.");
            }

            return board.WithMove(move, color, flips);
        }

        public static bool HasAnyLegalMove(BoardState board, PieceColor color)
        {
            ValidatePlayer(color);
            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    if (IsLegalMove(board, new BoardCoordinate(row, column), color))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsInside(int row, int column)
        {
            return row >= 0 && row < BoardState.Size && column >= 0 && column < BoardState.Size;
        }

        private static void ValidatePlayer(PieceColor color)
        {
            if (!color.IsPlayer())
            {
                throw new ArgumentException("A move color must be Black or White.", nameof(color));
            }
        }
    }
}
