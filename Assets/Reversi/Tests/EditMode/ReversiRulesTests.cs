using System.Collections.Generic;
using System.Linq;
using IceReversi.Core;
using NUnit.Framework;

namespace IceReversi.Tests
{
    public sealed class ReversiRulesTests
    {
        [Test]
        public void StandardOpening_HasExpectedPiecesAndBlackMoves()
        {
            var board = BoardState.CreateStandardOpening();

            Assert.That(board.Count(PieceColor.Black), Is.EqualTo(2));
            Assert.That(board.Count(PieceColor.White), Is.EqualTo(2));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    new BoardCoordinate(2, 3),
                    new BoardCoordinate(3, 2),
                    new BoardCoordinate(4, 5),
                    new BoardCoordinate(5, 4)
                },
                ReversiRules.GetLegalMoves(board, PieceColor.Black));
        }

        [Test]
        public void Move_CapturesAllEightDirections()
        {
            var board = BoardState.FromRows(
                "........",
                ".B.B.B..",
                "..WWW...",
                ".BW.WB..",
                "..WWW...",
                ".B.B.B..",
                "........",
                "........");

            var flips = ReversiRules.GetFlips(board, new BoardCoordinate(3, 3), PieceColor.Black);

            Assert.That(flips.Count, Is.EqualTo(8));
            var next = ReversiRules.ApplyMove(board, new BoardCoordinate(3, 3), PieceColor.Black, out _);
            Assert.That(next.Count(PieceColor.White), Is.EqualTo(0));
        }

        [TestCase(-1, -1)]
        [TestCase(-1, 0)]
        [TestCase(-1, 1)]
        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(1, -1)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        public void Move_CapturesEachDirectionIndependently(int rowStep, int columnStep)
        {
            var rows = Enumerable.Repeat("........", BoardState.Size)
                .Select(row => row.ToCharArray())
                .ToArray();
            rows[3 + rowStep][3 + columnStep] = 'W';
            rows[3 + (rowStep * 2)][3 + (columnStep * 2)] = 'B';
            var board = BoardState.FromRows(rows.Select(row => new string(row)).ToArray());

            var flips = ReversiRules.GetFlips(board, new BoardCoordinate(3, 3), PieceColor.Black);

            CollectionAssert.AreEqual(
                new[] { new BoardCoordinate(3 + rowStep, 3 + columnStep) },
                flips);
        }

        [Test]
        public void InvalidMove_DoesNotChangeSession()
        {
            var session = new GameSession();
            var before = session.Snapshot();

            var accepted = session.TryPlayMove(new BoardCoordinate(0, 0), out var result);

            Assert.That(accepted, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(session.Board, Is.EqualTo(before.Board));
            Assert.That(session.ActiveColor, Is.EqualTo(before.ActiveColor));
            Assert.That(session.HistoryCount, Is.Zero);
        }

        [Test]
        public void OccupiedMove_DoesNotChangeSession()
        {
            var session = new GameSession();
            var before = session.Snapshot();

            Assert.That(session.TryPlayMove(new BoardCoordinate(3, 3), out var result), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(session.Board, Is.EqualTo(before.Board));
            Assert.That(session.HistoryCount, Is.Zero);
        }

        [Test]
        public void Turn_PassesOpponentWhoHasNoMove()
        {
            var session = new GameSession(BoardState.FromRows(
                "BBBBBBBB",
                "BBBBBBBB",
                "BBBBBBBB",
                "BBBBBBBB",
                "BBBBBBBB",
                "BBBBBBBB",
                "BBBBBBBB",
                "BW.BW.BB"), PieceColor.Black);

            Assert.That(session.TryPlayMove(new BoardCoordinate(7, 2), out var result), Is.True);
            Assert.That(result.PassedColor, Is.EqualTo(PieceColor.White));
            Assert.That(session.ActiveColor, Is.EqualTo(PieceColor.Black));
            Assert.That(session.Result, Is.EqualTo(GameResult.InProgress));
        }

        [Test]
        public void FullBoard_DeterminesWinnerAndDraw()
        {
            var drawBoard = BoardState.FromRows(
                "BWBWBWBW",
                "WBWBWBWB",
                "BWBWBWBW",
                "WBWBWBWB",
                "BWBWBWBW",
                "WBWBWBWB",
                "BWBWBWBW",
                "WBWBWBWB");

            var session = new GameSession(drawBoard, PieceColor.Black);

            Assert.That(session.Result, Is.EqualTo(GameResult.Draw));
            Assert.That(session.LegalMoves, Is.Empty);
        }

        [Test]
        public void TerminalBoard_DeterminesWinningColor()
        {
            var session = new GameSession(BoardState.FromRows(
                "BBBBBBBB", "BBBBBBBB", "BBBBBBBB", "BBBBBBBB",
                "BBBBBBBB", "BBBBBBBB", "BBBBBBBB", "BBBBBBBW"), PieceColor.White);

            Assert.That(session.Result, Is.EqualTo(GameResult.BlackWins));
            Assert.That(session.LegalMoves, Is.Empty);
        }

        [Test]
        public void UndoOneTurn_RestoresHistoryAndDerivedState()
        {
            var session = new GameSession();
            var opening = session.Snapshot();
            Assert.That(session.TryPlayMove(new BoardCoordinate(2, 3), out _), Is.True);

            Assert.That(session.UndoOneTurn(), Is.True);

            Assert.That(session.Board, Is.EqualTo(opening.Board));
            Assert.That(session.ActiveColor, Is.EqualTo(opening.ActiveColor));
            Assert.That(session.LegalMoves, Is.EquivalentTo(opening.LegalMoves));
            Assert.That(session.HistoryCount, Is.Zero);
        }

        [Test]
        public void UndoHumanAiExchange_RestoresOpeningState()
        {
            var session = new GameSession();
            var opening = session.Board;
            Assert.That(session.TryPlayMove(new BoardCoordinate(2, 3), out _), Is.True);
            var whiteMove = session.LegalMoves.First();
            Assert.That(session.TryPlayMove(whiteMove, out _), Is.True);

            Assert.That(session.UndoHumanAiExchange(PieceColor.Black), Is.True);

            Assert.That(session.Board, Is.EqualTo(opening));
            Assert.That(session.ActiveColor, Is.EqualTo(PieceColor.Black));
            Assert.That(session.HistoryCount, Is.Zero);
        }

        [Test]
        public void Restart_ClearsHistoryAndRestoresOpening()
        {
            var session = new GameSession();
            Assert.That(session.TryPlayMove(new BoardCoordinate(2, 3), out _), Is.True);

            session.Restart();

            Assert.That(session.Board, Is.EqualTo(BoardState.CreateStandardOpening()));
            Assert.That(session.ActiveColor, Is.EqualTo(PieceColor.Black));
            Assert.That(session.HistoryCount, Is.Zero);
            Assert.That(session.Result, Is.EqualTo(GameResult.InProgress));
        }

        [Test]
        public void SidePreference_UsesFallbackAndPersistsValidSide()
        {
            var store = new MemorySideStore { Value = "invalid" };
            var preferences = new HumanSidePreferences(store);

            Assert.That(preferences.Load(PieceColor.White), Is.EqualTo(PieceColor.White));
            preferences.Save(PieceColor.Black);

            Assert.That(store.Value, Is.EqualTo("black"));
            Assert.That(preferences.Load(PieceColor.White), Is.EqualTo(PieceColor.Black));
        }

        private sealed class MemorySideStore : ISidePreferenceStore
        {
            public string Value { get; set; }
            public string Read() => Value;
            public void Write(string value) => Value = value;
        }
    }
}
