using System;
using System.Linq;
using System.Threading;
using IceReversi.Core;
using NUnit.Framework;

namespace IceReversi.Tests
{
    public sealed class ReversiAiDifficultyTests
    {
        [Test]
        public void Profiles_AreOrderedAndMapToBoundedOptions()
        {
            var profiles = new[]
            {
                AiDifficultyProfile.Easy,
                AiDifficultyProfile.Normal,
                AiDifficultyProfile.Hard,
                AiDifficultyProfile.Expert
            };

            CollectionAssert.AreEqual(
                new[] { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Expert },
                profiles.Select(profile => profile.Difficulty));
            for (var index = 1; index < profiles.Length; index++)
            {
                Assert.That(profiles[index].MaximumDepth, Is.GreaterThanOrEqualTo(profiles[index - 1].MaximumDepth));
                Assert.That(profiles[index].MaximumNodes, Is.GreaterThanOrEqualTo(profiles[index - 1].MaximumNodes));
                Assert.That(profiles[index].MaximumThinkTimeMilliseconds,
                    Is.GreaterThanOrEqualTo(profiles[index - 1].MaximumThinkTimeMilliseconds));
                Assert.That(profiles[index].TranspositionCapacity,
                    Is.GreaterThanOrEqualTo(profiles[index - 1].TranspositionCapacity));
            }

            Assert.That(AiDifficultyProfile.Easy.RootChoicePolicy,
                Is.EqualTo(AiRootChoicePolicy.ControlledTopThree));
            Assert.That(AiDifficultyProfile.Normal.RootChoicePolicy, Is.EqualTo(AiRootChoicePolicy.BestScore));
            Assert.That(AiDifficultyProfile.Hard.ExactEndgameThreshold, Is.EqualTo(10));
            Assert.That(AiDifficultyProfile.Expert.ExactEndgameThreshold, Is.EqualTo(12));
        }

        [Test]
        public void DifficultyCycle_WrapsInDocumentedOrder()
        {
            Assert.That(AiDifficultyProfile.Next(AiDifficulty.Easy), Is.EqualTo(AiDifficulty.Normal));
            Assert.That(AiDifficultyProfile.Next(AiDifficulty.Normal), Is.EqualTo(AiDifficulty.Hard));
            Assert.That(AiDifficultyProfile.Next(AiDifficulty.Hard), Is.EqualTo(AiDifficulty.Expert));
            Assert.That(AiDifficultyProfile.Next(AiDifficulty.Expert), Is.EqualTo(AiDifficulty.Easy));
        }

        [Test]
        public void DifficultyPreference_DefaultsToNormalAndPersistsValidValues()
        {
            var store = new MemoryDifficultyStore { Value = "not-a-profile" };
            var preferences = new AiDifficultyPreferences(store);
            Assert.That(preferences.Load(), Is.EqualTo(AiDifficulty.Normal));

            preferences.Save(AiDifficulty.Expert);
            Assert.That(store.Value, Is.EqualTo("expert"));
            Assert.That(preferences.Load(), Is.EqualTo(AiDifficulty.Expert));

            store.Value = "99";
            Assert.That(preferences.Load(), Is.EqualTo(AiDifficulty.Normal));
        }

        [Test]
        public void PositionIdentity_ContainsBothOccupanciesAndTurn()
        {
            var board = BoardState.CreateStandardOpening();
            var blackTurn = board.Identity(PieceColor.Black);
            var whiteTurn = board.Identity(PieceColor.White);

            Assert.That(blackTurn.BlackOccupancy, Is.EqualTo(board.BlackOccupancy));
            Assert.That(blackTurn.WhiteOccupancy, Is.EqualTo(board.WhiteOccupancy));
            Assert.That(blackTurn, Is.Not.EqualTo(whiteTurn));
            Assert.That(board.Identity(PieceColor.Black), Is.EqualTo(blackTurn));
        }

        [Test]
        public void TacticalFixtures_AreStableAndCoverRequiredShapes()
        {
            Assert.That(ReversiRules.GetLegalMoves(AiBoardFixtures.CornerCapture, PieceColor.Black),
                Does.Contain(new BoardCoordinate(0, 0)));
            Assert.That(AiBoardFixtures.UnsafeCornerAdjacency[0, 0], Is.EqualTo(PieceColor.Empty));
            Assert.That(AiBoardFixtures.UnsafeCornerAdjacency[0, 1], Is.EqualTo(PieceColor.Black));

            var passSession = new GameSession(AiBoardFixtures.ForcedPass, PieceColor.Black);
            Assert.That(passSession.TryPlayMove(new BoardCoordinate(7, 2), out var pass), Is.True);
            Assert.That(pass.PassedColor, Is.EqualTo(PieceColor.White));
            Assert.That(ReversiRules.GetLegalMoves(AiBoardFixtures.MobilityTradeOff, PieceColor.White).Count,
                Is.GreaterThan(1));
            Assert.That(AiBoardFixtures.ExactEndgame.EmptyCount, Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void InterruptedIteration_ReturnsDeterministicLegalFallback()
        {
            var board = BoardState.CreateStandardOpening();
            var options = new AiSearchOptions
            {
                MaximumDepth = 10,
                MaximumNodes = 1,
                MaximumThinkTimeMilliseconds = 1000,
                TranspositionCapacity = 8
            };

            var first = new ReversiAi().FindBestMove(board, PieceColor.Black, options);
            var second = new ReversiAi().FindBestMove(board, PieceColor.Black, options);

            Assert.That(first.CompletedDepth, Is.Zero);
            Assert.That(first.ReachedLimit, Is.True);
            Assert.That(first.Move, Is.EqualTo(second.Move));
            Assert.That(ReversiRules.GetLegalMoves(board, PieceColor.Black), Does.Contain(first.Move.Value));
        }

        [Test]
        public void IterativeSearch_ReportsCompletedDepthRootScoresAndBoundedCache()
        {
            var options = new AiSearchOptions
            {
                MaximumDepth = 5,
                MaximumNodes = 100000,
                MaximumThinkTimeMilliseconds = 3000,
                TranspositionCapacity = 64
            };
            var result = new ReversiAi().FindBestMove(
                AiBoardFixtures.MobilityTradeOff,
                PieceColor.White,
                options);

            Assert.That(result.CompletedDepth, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.RootScores.Count, Is.EqualTo(
                ReversiRules.GetLegalMoves(AiBoardFixtures.MobilityTradeOff, PieceColor.White).Count));
            Assert.That(result.CacheEntries, Is.LessThanOrEqualTo(options.TranspositionCapacity));
            Assert.That(result.CacheHits, Is.GreaterThan(0));
        }

        [Test]
        public void TacticalCornerCapture_IsSelected()
        {
            var legal = ReversiRules.GetLegalMoves(AiBoardFixtures.CornerCapture, PieceColor.Black);
            var result = new ReversiAi().FindBestMove(
                AiBoardFixtures.CornerCapture,
                PieceColor.Black,
                AiDifficultyProfile.Normal.CreateSearchOptions(7));

            Assert.That(legal, Does.Contain(new BoardCoordinate(0, 0)));
            Assert.That(result.Move, Is.EqualTo(new BoardCoordinate(0, 0)));
        }

        [Test]
        public void ExactEndgame_WhenCompletedReturnsLegalSolvedMove()
        {
            var legal = ReversiRules.GetLegalMoves(AiBoardFixtures.ExactEndgame, PieceColor.White);
            Assert.That(legal, Is.Not.Empty);
            var result = new ReversiAi().FindBestMove(
                AiBoardFixtures.ExactEndgame,
                PieceColor.White,
                new AiSearchOptions
                {
                    MaximumDepth = 2,
                    MaximumNodes = 250000,
                    MaximumThinkTimeMilliseconds = 3000,
                    TranspositionCapacity = 4096,
                    ExactEndgameThreshold = 12
                });

            Assert.That(result.SolvedToEnd, Is.True);
            Assert.That(legal, Does.Contain(result.Move.Value));
        }

        [TestCase(AiDifficulty.Easy)]
        [TestCase(AiDifficulty.Normal)]
        [TestCase(AiDifficulty.Hard)]
        [TestCase(AiDifficulty.Expert)]
        public void EveryProfile_ReturnsOnlyLegalMoves(AiDifficulty difficulty)
        {
            var board = BoardState.CreateStandardOpening();
            var result = new ReversiAi().FindBestMove(
                board,
                PieceColor.Black,
                AiDifficultyProfile.For(difficulty).CreateSearchOptions(1234));

            Assert.That(ReversiRules.GetLegalMoves(board, PieceColor.Black), Does.Contain(result.Move.Value));
        }

        [Test]
        public void CancellationStillStopsOptimizedSearch()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => new ReversiAi().FindBestMove(
                AiBoardFixtures.MobilityTradeOff,
                PieceColor.White,
                AiDifficultyProfile.Expert.CreateSearchOptions(),
                cancellation.Token));
        }

        private sealed class MemoryDifficultyStore : IAiDifficultyPreferenceStore
        {
            public string Value { get; set; }
            public string Read() => Value;
            public void Write(string value) => Value = value;
        }
    }
}
