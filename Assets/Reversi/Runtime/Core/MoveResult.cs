using System.Collections.Generic;

namespace IceReversi.Core
{
    public sealed class MoveResult
    {
        public MoveResult(
            BoardCoordinate move,
            PieceColor color,
            BoardState board,
            IReadOnlyList<BoardCoordinate> flips,
            PieceColor passedColor,
            GameResult gameResult)
        {
            Move = move;
            Color = color;
            Board = board;
            Flips = flips;
            PassedColor = passedColor;
            GameResult = gameResult;
        }

        public BoardCoordinate Move { get; }
        public PieceColor Color { get; }
        public BoardState Board { get; }
        public IReadOnlyList<BoardCoordinate> Flips { get; }
        public PieceColor PassedColor { get; }
        public bool HasPass => PassedColor.IsPlayer();
        public GameResult GameResult { get; }
        public bool IsGameOver => GameResult != GameResult.InProgress;
    }
}
