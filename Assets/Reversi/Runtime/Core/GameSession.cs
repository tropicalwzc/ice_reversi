using System;
using System.Collections.Generic;

namespace IceReversi.Core
{
    public sealed class GameSession
    {
        private readonly List<SessionState> history = new List<SessionState>();

        public GameSession()
            : this(BoardState.CreateStandardOpening(), PieceColor.Black)
        {
        }

        public GameSession(BoardState board, PieceColor activeColor)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            if (!activeColor.IsPlayer())
            {
                throw new ArgumentException("The active color must be Black or White.", nameof(activeColor));
            }

            ActiveColor = activeColor;
            LastPassedColor = PieceColor.Empty;
            Result = CalculateResult(board);
            NormalizeTurn();
        }

        public BoardState Board { get; private set; }
        public PieceColor ActiveColor { get; private set; }
        public PieceColor LastPassedColor { get; private set; }
        public GameResult Result { get; private set; }
        public int HistoryCount => history.Count;
        public bool CanUndo => history.Count > 0;
        public IReadOnlyList<BoardCoordinate> LegalMoves =>
            Result == GameResult.InProgress
                ? ReversiRules.GetLegalMoves(Board, ActiveColor)
                : Array.Empty<BoardCoordinate>();

        public GameSnapshot Snapshot()
        {
            return new GameSnapshot(Board, ActiveColor, LastPassedColor, Result, LegalMoves, history.Count);
        }

        public bool TryPlayMove(BoardCoordinate move, out MoveResult moveResult)
        {
            moveResult = null;
            if (Result != GameResult.InProgress)
            {
                return false;
            }

            var flips = ReversiRules.GetFlips(Board, move, ActiveColor);
            if (flips.Count == 0)
            {
                return false;
            }

            history.Add(CaptureState());
            var playedColor = ActiveColor;
            Board = Board.WithMove(move, playedColor, flips);
            LastPassedColor = PieceColor.Empty;

            var opponent = playedColor.Opponent();
            if (ReversiRules.HasAnyLegalMove(Board, opponent))
            {
                ActiveColor = opponent;
            }
            else if (ReversiRules.HasAnyLegalMove(Board, playedColor))
            {
                ActiveColor = playedColor;
                LastPassedColor = opponent;
            }
            else
            {
                Result = CalculateResult(Board);
            }

            moveResult = new MoveResult(move, playedColor, Board, flips, LastPassedColor, Result);
            return true;
        }

        public void Restart()
        {
            Board = BoardState.CreateStandardOpening();
            ActiveColor = PieceColor.Black;
            LastPassedColor = PieceColor.Empty;
            Result = GameResult.InProgress;
            history.Clear();
        }

        public bool UndoOneTurn()
        {
            if (history.Count == 0)
            {
                return false;
            }

            var index = history.Count - 1;
            RestoreState(history[index]);
            history.RemoveAt(index);
            return true;
        }

        public bool UndoHumanAiExchange(PieceColor humanColor)
        {
            if (!humanColor.IsPlayer() || history.Count == 0)
            {
                return false;
            }

            var undone = UndoOneTurn();
            if (undone && ActiveColor != humanColor && history.Count > 0)
            {
                UndoOneTurn();
            }

            return undone;
        }

        private void NormalizeTurn()
        {
            if (Result != GameResult.InProgress)
            {
                return;
            }

            if (ReversiRules.HasAnyLegalMove(Board, ActiveColor))
            {
                return;
            }

            var opponent = ActiveColor.Opponent();
            if (ReversiRules.HasAnyLegalMove(Board, opponent))
            {
                LastPassedColor = ActiveColor;
                ActiveColor = opponent;
                return;
            }

            Result = CalculateResult(Board);
        }

        private static GameResult CalculateResult(BoardState board)
        {
            if (ReversiRules.HasAnyLegalMove(board, PieceColor.Black) ||
                ReversiRules.HasAnyLegalMove(board, PieceColor.White))
            {
                return GameResult.InProgress;
            }

            var black = board.Count(PieceColor.Black);
            var white = board.Count(PieceColor.White);
            return black > white ? GameResult.BlackWins :
                white > black ? GameResult.WhiteWins : GameResult.Draw;
        }

        private SessionState CaptureState()
        {
            return new SessionState(Board, ActiveColor, LastPassedColor, Result);
        }

        private void RestoreState(SessionState state)
        {
            Board = state.Board;
            ActiveColor = state.ActiveColor;
            LastPassedColor = state.LastPassedColor;
            Result = state.Result;
        }

        private readonly struct SessionState
        {
            public SessionState(BoardState board, PieceColor activeColor, PieceColor lastPassedColor, GameResult result)
            {
                Board = board;
                ActiveColor = activeColor;
                LastPassedColor = lastPassedColor;
                Result = result;
            }

            public BoardState Board { get; }
            public PieceColor ActiveColor { get; }
            public PieceColor LastPassedColor { get; }
            public GameResult Result { get; }
        }
    }
}
