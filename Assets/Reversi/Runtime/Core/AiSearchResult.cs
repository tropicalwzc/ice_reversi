using System;
using System.Collections.Generic;

namespace IceReversi.Core
{
    public readonly struct AiRootScore
    {
        public AiRootScore(BoardCoordinate move, int score)
        {
            Move = move;
            Score = score;
        }

        public BoardCoordinate Move { get; }
        public int Score { get; }
    }

    public sealed class AiSearchResult
    {
        public AiSearchResult(
            BoardCoordinate? move,
            int score,
            int expandedNodes,
            bool reachedLimit,
            int completedDepth = 0,
            int cacheHits = 0,
            int cacheEntries = 0,
            IReadOnlyList<AiRootScore> rootScores = null,
            bool solvedToEnd = false)
        {
            Move = move;
            Score = score;
            ExpandedNodes = expandedNodes;
            ReachedLimit = reachedLimit;
            CompletedDepth = completedDepth;
            CacheHits = cacheHits;
            CacheEntries = cacheEntries;
            RootScores = rootScores ?? Array.Empty<AiRootScore>();
            SolvedToEnd = solvedToEnd;
        }

        public BoardCoordinate? Move { get; }
        public int Score { get; }
        public int ExpandedNodes { get; }
        public bool ReachedLimit { get; }
        public int CompletedDepth { get; }
        public int CacheHits { get; }
        public int CacheEntries { get; }
        public IReadOnlyList<AiRootScore> RootScores { get; }
        public bool SolvedToEnd { get; }
        public bool HasMove => Move.HasValue;
    }
}
