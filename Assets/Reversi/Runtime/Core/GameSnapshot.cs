using System.Collections.Generic;

namespace IceReversi.Core
{
    public sealed class GameSnapshot
    {
        public GameSnapshot(
            BoardState board,
            PieceColor activeColor,
            PieceColor lastPassedColor,
            GameResult result,
            IReadOnlyList<BoardCoordinate> legalMoves,
            int historyCount)
        {
            Board = board;
            ActiveColor = activeColor;
            LastPassedColor = lastPassedColor;
            Result = result;
            LegalMoves = legalMoves;
            HistoryCount = historyCount;
        }

        public BoardState Board { get; }
        public PieceColor ActiveColor { get; }
        public PieceColor LastPassedColor { get; }
        public GameResult Result { get; }
        public IReadOnlyList<BoardCoordinate> LegalMoves { get; }
        public int HistoryCount { get; }
        public int BlackScore => Board.Count(PieceColor.Black);
        public int WhiteScore => Board.Count(PieceColor.White);
        public bool IsGameOver => Result != GameResult.InProgress;
    }
}
