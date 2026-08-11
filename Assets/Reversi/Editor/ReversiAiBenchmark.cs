using System;
using System.Collections.Generic;
using System.Diagnostics;
using IceReversi.Core;
using UnityEditor;
using UnityEngine;

namespace IceReversi.Editor
{
    public static class ReversiAiBenchmark
    {
        public static void RunFromCommandLine()
        {
            try
            {
                RunPositionSuite();
                RunFullGame(AiDifficulty.Easy, AiDifficulty.Normal);
                RunFullGame(AiDifficulty.Hard, AiDifficulty.Expert);
                UnityEngine.Debug.Log("ICE_REVERSI_AI_BENCHMARK_SUCCESS");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RunPositionSuite()
        {
            var positions = new[]
            {
                CreatePosition(0),
                CreatePosition(24),
                CreatePosition(48)
            };
            var names = new[] { "early", "middle", "late" };
            foreach (AiDifficulty difficulty in Enum.GetValues(typeof(AiDifficulty)))
            {
                var profile = AiDifficultyProfile.For(difficulty);
                for (var index = 0; index < positions.Length; index++)
                {
                    var session = positions[index];
                    var stopwatch = Stopwatch.StartNew();
                    var result = new ReversiAi().FindBestMove(
                        session.Board,
                        session.ActiveColor,
                        profile.CreateSearchOptions(9000 + index));
                    stopwatch.Stop();
                    if (!result.HasMove || !Contains(session.LegalMoves, result.Move.Value))
                    {
                        throw new InvalidOperationException($"{difficulty} returned an illegal {names[index]} move.");
                    }

                    UnityEngine.Debug.Log(
                        $"AI_BENCHMARK kind=position difficulty={difficulty} stage={names[index]} " +
                        $"move={result.Move.Value} depth={result.CompletedDepth} nodes={result.ExpandedNodes} " +
                        $"elapsedMs={stopwatch.ElapsedMilliseconds} cacheHits={result.CacheHits} " +
                        $"cacheEntries={result.CacheEntries} limited={result.ReachedLimit} exact={result.SolvedToEnd}");
                }
            }
        }

        private static void RunFullGame(AiDifficulty blackDifficulty, AiDifficulty whiteDifficulty)
        {
            var session = new GameSession();
            var aggregates = new Dictionary<PieceColor, Aggregate>
            {
                [PieceColor.Black] = new Aggregate(),
                [PieceColor.White] = new Aggregate()
            };
            var stopwatch = Stopwatch.StartNew();
            var ply = 0;
            while (session.Result == GameResult.InProgress && ply < 64)
            {
                var color = session.ActiveColor;
                var difficulty = color == PieceColor.Black ? blackDifficulty : whiteDifficulty;
                var moveStopwatch = Stopwatch.StartNew();
                var result = new ReversiAi().FindBestMove(
                    session.Board,
                    color,
                    AiDifficultyProfile.For(difficulty).CreateSearchOptions(12000 + ply));
                moveStopwatch.Stop();
                if (!result.HasMove || !session.TryPlayMove(result.Move.Value, out _))
                {
                    throw new InvalidOperationException($"{difficulty} returned an invalid move at ply {ply}.");
                }

                aggregates[color].Add(result, moveStopwatch.ElapsedMilliseconds);
                ply++;
            }

            stopwatch.Stop();
            var black = aggregates[PieceColor.Black];
            var white = aggregates[PieceColor.White];
            UnityEngine.Debug.Log(
                $"AI_BENCHMARK kind=game black={blackDifficulty} white={whiteDifficulty} result={session.Result} " +
                $"score={session.Board.Count(PieceColor.Black)}-{session.Board.Count(PieceColor.White)} " +
                $"plies={ply} elapsedMs={stopwatch.ElapsedMilliseconds} " +
                $"blackNodes={black.Nodes} blackCacheHits={black.CacheHits} blackMaxDepth={black.MaximumDepth} blackThinkMs={black.ElapsedMilliseconds} " +
                $"whiteNodes={white.Nodes} whiteCacheHits={white.CacheHits} whiteMaxDepth={white.MaximumDepth} whiteThinkMs={white.ElapsedMilliseconds}");
        }

        private static GameSession CreatePosition(int plies)
        {
            var session = new GameSession();
            for (var ply = 0; ply < plies && session.Result == GameResult.InProgress; ply++)
            {
                var moves = session.LegalMoves;
                if (!session.TryPlayMove(moves[moves.Count / 2], out _))
                {
                    throw new InvalidOperationException($"Could not create deterministic position at ply {ply}.");
                }
            }
            return session;
        }

        private static bool Contains(IReadOnlyList<BoardCoordinate> moves, BoardCoordinate target)
        {
            for (var index = 0; index < moves.Count; index++)
            {
                if (moves[index] == target) return true;
            }
            return false;
        }

        private sealed class Aggregate
        {
            public long Nodes { get; private set; }
            public long CacheHits { get; private set; }
            public int MaximumDepth { get; private set; }
            public long ElapsedMilliseconds { get; private set; }

            public void Add(AiSearchResult result, long elapsedMilliseconds)
            {
                Nodes += result.ExpandedNodes;
                CacheHits += result.CacheHits;
                MaximumDepth = Math.Max(MaximumDepth, result.CompletedDepth);
                ElapsedMilliseconds += elapsedMilliseconds;
            }
        }
    }
}
