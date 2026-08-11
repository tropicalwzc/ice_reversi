using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using IceReversi.Core;
using NUnit.Framework;

namespace IceReversi.Tests
{
    public sealed class ReversiAiTests
    {
        [Test]
        public void OpeningSearch_ReturnsLegalMove()
        {
            var board = BoardState.CreateStandardOpening();
            var legal = ReversiRules.GetLegalMoves(board, PieceColor.Black);
            var result = new ReversiAi().FindBestMove(
                board,
                PieceColor.Black,
                new AiSearchOptions { MaximumDepth = 3, MaximumNodes = 10000, MaximumThinkTimeMilliseconds = 1000 });

            Assert.That(result.HasMove, Is.True);
            Assert.That(legal.Contains(result.Move.Value), Is.True);
        }

        [Test]
        public void NoMovePosition_ReturnsNoMove()
        {
            var board = BoardState.FromRows(
                "BBBBBBBB", "BBBBBBBB", "BBBBBBBB", "BBBBBBBB",
                "BBBBBBBB", "BBBBBBBB", "BBBBBBBB", "BBBBBBBB");

            var result = new ReversiAi().FindBestMove(board, PieceColor.White);

            Assert.That(result.HasMove, Is.False);
        }

        [Test]
        public void CancelledSearch_ThrowsWithoutReturningAStaleMove()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                new ReversiAi().FindBestMove(
                    BoardState.CreateStandardOpening(),
                    PieceColor.Black,
                    AiSearchOptions.Default,
                    cancellation.Token));
        }

        [Test]
        public void InvalidatedRequest_IsNotCurrent()
        {
            using var service = new AiRequestService();
            var request = service.Start(
                BoardState.CreateStandardOpening(),
                PieceColor.Black,
                new AiSearchOptions { MaximumDepth = 8, MaximumNodes = 1000000, MaximumThinkTimeMilliseconds = 5000 });

            service.Invalidate();

            Assert.That(service.IsCurrent(request.Generation), Is.False);
        }

        [Test]
        public void SearchHonorsConfiguredNodeBound()
        {
            const int maximumNodes = 50;
            var result = new ReversiAi().FindBestMove(
                BoardState.CreateStandardOpening(),
                PieceColor.Black,
                new AiSearchOptions
                {
                    MaximumDepth = 8,
                    MaximumNodes = maximumNodes,
                    MaximumThinkTimeMilliseconds = 5000
                });

            Assert.That(result.ExpandedNodes, Is.LessThanOrEqualTo(maximumNodes + 1));
            Assert.That(result.HasMove, Is.True);
            Assert.That(result.ReachedLimit, Is.True);
        }

        [TestCase(0, "early")]
        [TestCase(24, "middle")]
        [TestCase(48, "late")]
        public void RepresentativeSearches_AreLegalAndBounded(int plies, string stage)
        {
            var session = CreatePosition(plies);
            var legal = session.LegalMoves.ToArray();
            var options = AiSearchOptions.Default;
            var stopwatch = Stopwatch.StartNew();

            var result = new ReversiAi().FindBestMove(session.Board, session.ActiveColor, options);

            stopwatch.Stop();
            TestContext.Out.WriteLine(
                $"AI_PROFILE stage={stage} plies={plies} elapsedMs={stopwatch.ElapsedMilliseconds} " +
                $"nodes={result.ExpandedNodes} reachedLimit={result.ReachedLimit}");
            Assert.That(result.HasMove, Is.True);
            Assert.That(legal.Contains(result.Move.Value), Is.True);
            Assert.That(result.ExpandedNodes, Is.LessThanOrEqualTo(options.MaximumNodes));
            Assert.That(
                stopwatch.ElapsedMilliseconds,
                Is.LessThanOrEqualTo(options.MaximumThinkTimeMilliseconds + 250));
        }

        [Test]
        public void BackgroundRequest_ReturnsControlPromptlyAndCompletesLegally()
        {
            var board = BoardState.CreateStandardOpening();
            var legal = ReversiRules.GetLegalMoves(board, PieceColor.Black);
            using var service = new AiRequestService();
            var stopwatch = Stopwatch.StartNew();

            var request = service.Start(board, PieceColor.Black, AiSearchOptions.Default);

            stopwatch.Stop();
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(100));
            var result = request.Task.GetAwaiter().GetResult();
            Assert.That(legal.Contains(result.Move.Value), Is.True);
            Assert.That(service.IsCurrent(request.Generation), Is.True);
        }

        private static GameSession CreatePosition(int plies)
        {
            var session = new GameSession();
            for (var ply = 0; ply < plies; ply++)
            {
                Assert.That(session.Result, Is.EqualTo(GameResult.InProgress));
                var moves = session.LegalMoves;
                Assert.That(moves, Is.Not.Empty);
                Assert.That(session.TryPlayMove(moves[moves.Count / 2], out _), Is.True);
            }

            Assert.That(session.Result, Is.EqualTo(GameResult.InProgress));
            return session;
        }
    }
}
